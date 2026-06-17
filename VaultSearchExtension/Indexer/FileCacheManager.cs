using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Indexer.Files;
using CmdPal.VaultSearchExtension.Indexer.Search;
using CmdPal.VaultSearchExtension.Indexer.Vaults;
using EzPinyin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CmdPal.VaultSearchExtension.Indexer;

internal sealed partial class FileCacheManager: IDisposable {

    // Cache configuration
    private const int MaxContentPreviewLength = 10240; // 10KB
    private const int MaxTokenLength = 50;
    private const int MinTokenLength = 2;

    // Vault indexes
    private readonly ConcurrentDictionary<string, VaultFile> _vaultDictionaries = new();
    // Inverted index: token -> list of PostingEntry
    private readonly ConcurrentDictionary<string, List<PostingEntry>> _invertedIndex = new(StringComparer.OrdinalIgnoreCase);
    // Forward file index: absolute path -> FileEntry
    private readonly ConcurrentDictionary<string, FileEntry> _fileIndex = new();

    // File watcher manager
    private readonly FileWatcherManager _watcherManager;
    // Concurrency control
    private readonly ReaderWriterLockSlim _indexLock = new();
    // Rebuild lock: ensure queries are blocked during index rebuild to avoid reading incomplete index data
    private readonly ReaderWriterLockSlim _rebuildLock = new();
    // Index state
    private static IndexStateEnum _indexState = IndexStateEnum.Pending;

    private static readonly Lazy<FileCacheManager> _instance = new(() => new FileCacheManager());
    public static FileCacheManager Instance => _instance.Value;
    private FileCacheManager() {
        _watcherManager = new FileWatcherManager(this);
    }

    [GeneratedRegex(@"[\u4e00-\u9fff]+|[a-zA-Z0-9]+")]
    private static partial Regex TokensRegex();

    public int VaultCount => _vaultDictionaries.Count;
    public int FileCount => _fileIndex.Count;
    public long InvertedCount => _invertedIndex.Values.Sum(o => o.Count);

    public IndexStateEnum IndexState {
        get => _indexState;
        set {
            if(_indexState == value) return;
            IndexStateEnum oldValue = _indexState;
            _indexState = value;
            OnIndexStateChanged(oldValue, value);
        }
    }

    public event EventHandler<StateChangedEventArgs<IndexStateEnum>>? IndexStateChanged;

    public void OnIndexStateChanged(IndexStateEnum oldValue, IndexStateEnum newValue) {
        IndexStateChanged?.Invoke(this, new StateChangedEventArgs<IndexStateEnum>(oldValue, newValue));
    }

    public void RebuildIndex(ReadOnlySet<VaultEntry> vaultSet, ISettingOptions settingOptions) {

        if(vaultSet is null || vaultSet.Count == 0) {
            Clear(); return;
        }

        if(_indexState.Equals(IndexStateEnum.Running)) {
            LogHelper.Debug($"Building index, cancel this request...");
            return;
        }

        _rebuildLock.EnterWriteLock();
        try {
            // Change state to running
            _indexState = IndexStateEnum.Running;
            // 2. Stop the old watchers
            _watcherManager?.StopWatching();
            // 3. Clear all old data
            _invertedIndex.Clear();
            _fileIndex.Clear();
            _vaultDictionaries.Clear();
            // 4. Rebuild index
            BuildIndex(vaultSet, settingOptions);
            // 5. Start new watchers
            _watcherManager?.StartWatching(vaultSet);
            // 6. Change state to completed

        } finally {
            _indexState = IndexStateEnum.Completed;
            _rebuildLock.ExitWriteLock();
        }
    }

    private void BuildIndex(ReadOnlySet<VaultEntry> vaultSet, ISettingOptions settingOptions) {

        LogHelper.Info($"Start building the index, a total of {vaultSet.Count} vaults...");

        var startTime = DateTime.Now;
        _indexState = IndexStateEnum.Running;

        // Phase 1: parallel scan of all vault files
        Parallel.ForEach(vaultSet, vault => {
            var cacheVault = new VaultFile {
                VaultName = vault.VaultName,
                VaultRootPath = vault.VaultRootPath,
                VaultFiles = []
            };

            try {
                // Recursively scan files
                var filePaths = FileReaderFactory.FilesRecursive(vault.VaultRootPath);
                LogHelper.Debug($"[{vault.VaultName}] found {filePaths.Count} files");

                // Process files in parallel
                var processedCount = 0;
                Parallel.ForEach(filePaths, filePath => {
                    try {
                        var entry = FileReaderFactory.GetReader(filePath).ReadEntry(filePath, vault.VaultName);
                        _fileIndex.TryAdd(filePath, entry);
                        cacheVault.VaultFiles.Add(filePath);

                        // Build inverted index
                        BuildInvertedIndex(entry);

                        var count = Interlocked.Increment(ref processedCount);
                        if(count % 1000 == 0)
                            LogHelper.Debug($"[{vault.VaultName}] has processed {count}/{filePaths.Count} files");
                    } catch(Exception ex) {
                        LogHelper.Error($"Failed to process the file [{filePath}]", ex);
                    }
                });
                _vaultDictionaries.TryAdd(vault.VaultName, cacheVault);
            } catch(Exception ex) {
                LogHelper.Error($"Failed to scan warehouse [{vault.VaultName}]", ex);
            }
        });

        var elapsed = DateTime.Now - startTime;
        LogHelper.Info($"Index construction completed, time taken: {elapsed.TotalSeconds:F1} seconds");
        LogHelper.Info($"Total number of files: {_fileIndex.Count}, Number of index terms: {_invertedIndex.Count}");
    }

    /// <summary>
    /// Build inverted index for a single file. Index priority: FileName > Title > Tags > Content
    /// </summary>
    private void BuildInvertedIndex(FileEntry entry) {
        // 1. Index file name (highest weight)
        AddTokensToIndex(entry.FileName, entry.FilePath, MatchType.FileName);

        // 2. Index title
        if(!string.IsNullOrWhiteSpace(entry.Title))
            AddTokensToIndex(entry.Title, entry.FilePath, MatchType.Title);

        // 3. Index tags
        if(entry.Tags != null)
            foreach(var tag in entry.Tags)
                AddTokensToIndex(tag, entry.FilePath, MatchType.Tags);

        // 4. Index content
        if(!string.IsNullOrWhiteSpace(entry.Content))
            AddTokensToIndex(entry.Content, entry.FilePath, MatchType.Content);
    }

    private void AddTokensToIndex(string text, string filePath, MatchType matchType) {
        var tokens = Tokenize(text);

        foreach(var token in tokens) {
            var posting = new PostingEntry {
                FilePath = filePath,
                MatchType = matchType,
                MatchPosition = MatchPosition.Substring // default substring match
            };

            _invertedIndex.AddOrUpdate(token, [posting], (key, existingList) => {
                lock(existingList) {
                    // 避免重复添加相同文件的同类型匹配
                    if(!existingList.Any(p => p.FilePath == filePath && p.MatchType == matchType)) {
                        existingList.Add(posting);
                    }
                }
                return existingList;
            }
            );
        }
    }

    /// <summary>
    /// Tokenizer: supports splitting Chinese by characters and English by spaces/punctuation.
    /// Also adds initials and full pinyin for Chinese tokens to improve pinyin search.
    /// </summary>
    private static HashSet<string> Tokenize(string text) {
        if(string.IsNullOrWhiteSpace(text)) return [];

        // Split: Chinese by characters; English by spaces and punctuation
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract English words and Chinese character groups
        var matches = TokensRegex().Matches(text);
        foreach(Match match in matches) {
            var token = match.Value.ToLower(CultureInfo.CurrentCulture);

            if(token.Length >= MinTokenLength && token.Length <= MaxTokenLength) {
                tokens.Add(token);

                // For Chinese tokens, additionally add initial pinyin
                if(token.Any(c => c >= 0x4e00 && c <= 0x9fff)) {
                    var initials = PinyinHelper.GetInitial(token);
                    if(initials.Length >= MinTokenLength) {
                        tokens.Add(initials);
                    }

                    // Add full pinyin
                    var fullPinyin = PinyinHelper.GetPinyin(token);
                    if(fullPinyin.Length >= MinTokenLength) {
                        tokens.Add(fullPinyin);
                    }
                }
            }
        }
        return tokens;
    }

    /// <summary>
    /// Search engine: for given query, perform standard index lookup (exact, prefix, substring).
    /// If the input resembles pinyin, perform pinyin lookup as well. Results are sorted by total score
    /// and modification time. Supports filtering by vault and limits results to top items for performance.
    /// </summary>
    public IReadOnlyList<SearchResult> Search(string query, int skip, int take, string? vaultName = null) {
        if(string.IsNullOrWhiteSpace(query)) {
            return GetAllFilesSortedByTime(skip, take, vaultName);
        }

        var queryTokens = Tokenize(query);
        var resultScores = new ConcurrentDictionary<string, SearchResult>();

        // 1. Standard index lookup
        Parallel.ForEach(queryTokens, token => SearchToken(token, resultScores));

        // 3. Convert to list and filter
        var results = resultScores.Values.ToList();

        if(!string.IsNullOrWhiteSpace(vaultName)) {
            results = [.. results.Where(r => r.VaultName == vaultName)];
        }

        // 4. Sort
        results.Sort((a, b) => {
            // Order by total score descending
            if(a.TotalScore != b.TotalScore)
                return b.TotalScore.CompareTo(a.TotalScore);

            // If scores equal, order by last modified descending
            return b.LastModifiedTicks.CompareTo(a.LastModifiedTicks);
        });

        return [.. results.Skip(skip).Take(take)];
    }

    /// <summary>
    /// Get all files (when no query). Sorted by last modified descending; newest files are prioritized.
    /// </summary>
    /// <param name="vaultName"></param>
    /// <returns></returns>
    public IReadOnlyList<SearchResult> GetAllFilesSortedByTime(int skip = 0, int take = Int32.MaxValue, string? vaultName = null) {
        var files = string.IsNullOrWhiteSpace(vaultName)
            ? _fileIndex.Values
            : _fileIndex.Values.Where(f => f.VaultName == vaultName);

        return [.. files.Select(f => new SearchResult
                    {
                        FilePath = f.FilePath,
                        VaultName = f.VaultName,
                        FileName = f.FileName,
                        Extension = f.Extension,
                        Length = f.Length,
                        Tags = f.Tags,
                        LastModifiedTicks = f.LastModifiedTicks,
                        DisplayTitle = f.Title ?? f.FileName,
                        TotalScore = 0,
                        MatchDetails = []
                    })
                    .OrderByDescending(r => r.LastModifiedTicks)
                    .Skip(skip)
                    .Take(take)];
    }

    /// <summary>
    /// Remove all tag entries for the specified file from the inverted index
    /// </summary>
    public void RemoveTagsFromIndex(string filePath, string[] tags) {
        if(tags == null || tags.Length == 0)
            return;

        foreach(var tag in tags) {
            var tokens = Tokenize(tag);
            foreach(var token in tokens) {
                RemovePostingForFile(token, filePath);
            }
        }
    }

    /// <summary>
    /// Add tag entries of the specified file into the inverted index
    /// </summary>
    public void AddTagsToIndex(string filePath, string[] tags) {
        if(tags == null || tags.Length == 0)
            return;

        foreach(var tag in tags) {
            // Reuse tokenizer + indexing logic for Tags
            AddTokensToIndex(tag, filePath, MatchType.Tags);
        }
    }

    /// <summary>
    /// Remove content tokens of the specified file from the inverted index
    /// </summary>
    public void RemoveContentFromIndex(string filePath, string content) {
        if(string.IsNullOrWhiteSpace(content))
            return;

        var tokens = Tokenize(content);
        foreach(var token in tokens) {
            RemovePostingForFile(token, filePath);
        }
    }

    /// <summary>
    /// Add content tokens of the specified file into the inverted index
    /// </summary>
    public void AddContentToIndex(string filePath, string content) {
        if(string.IsNullOrWhiteSpace(content))
            return;

        // 复用已有的分词+索引逻辑，类型为 Content
        AddTokensToIndex(content, filePath, MatchType.Content);
    }

    /// <summary>
    /// Remove all PostingEntry entries under a token that point to the specified file.
    /// If the list becomes empty, remove the key to save memory.
    /// </summary>
    public void RemovePostingForFile(string token, string filePath) {
        if(!_invertedIndex.TryGetValue(token, out var postings))
            return;

        // Since multiple threads may run concurrently, lock the list for safety
        lock(postings) {
            // Remove all entries that match the file path
            postings.RemoveAll(p => p.FilePath == filePath);

            // If the list is empty, consider removing the key to reduce memory usage
            if(postings.Count == 0) {
                // Note: _invertedIndex.TryRemove may fail if another thread added new entries; this is safe
                _invertedIndex.TryRemove(token, out _);
            }
        }
    }


    /// <summary>
    /// Index update: when a file is modified or added, reprocess it and update the index and vault file list
    /// </summary>
    public void AddOrUpdateFile(string filePath, string vaultName) {
        try {
            // Remove old index
            RemoveFile(filePath);

            // Add new index
            var entry = FileReaderFactory.GetReader(filePath).ReadEntry(filePath, vaultName);
            _fileIndex.TryAdd(filePath, entry);
            BuildInvertedIndex(entry);

            if(_vaultDictionaries.TryGetValue(vaultName, out var repo)) {
                lock(repo.VaultFiles) {
                    repo.VaultFiles.Add(filePath);
                }
            }
        } catch(Exception ex) {
            LogHelper.Error($"Failed to update file index in [{filePath}]:", ex);
        }
    }

    /// <summary>
    /// Update index: when a file is deleted, remove its related entries from the index and update the vault file list
    /// </summary>
    /// <param name="filePath"></param>
    public void RemoveFile(string filePath) {
        if(_fileIndex.TryRemove(filePath, out var entry)) {
            // Remove from inverted index (simplified; a full cleanup may be performed in background)
            foreach(var token in Tokenize(entry.FileName + entry.Title +
                string.Join(" ", entry.Tags ?? []))) {
                if(_invertedIndex.TryGetValue(token, out var postings)) {
                    lock(postings) {
                        postings.RemoveAll(p => p.FilePath == filePath);
                    }
                }
            }

            // Remove from vault file list
            if(_vaultDictionaries.TryGetValue(entry.VaultName, out var repo)) {
                lock(repo.VaultFiles) {
                    repo.VaultFiles.Remove(filePath);
                }
            }
        }
    }


    /// <summary>
    /// Search for a single word, query in the order of exact match, prefix match, 
    /// and substring match, and calculate scores based on match type and position, updating the result set
    /// </summary>
    private void SearchToken(string token, ConcurrentDictionary<string, SearchResult> results) {
        var lowerToken = token.ToLower(CultureInfo.CurrentCulture);

        // Strategy 1: Exact Match
        if(_invertedIndex.TryGetValue(lowerToken, out var exactMatches)) {
            foreach(var posting in exactMatches) {
                if(_fileIndex.TryGetValue(posting.FilePath, out var fileEntry)) {
                    var score = CalculateScore(posting.MatchType, MatchPosition.Exact);
                    UpdateResult(results, fileEntry, posting.MatchType, lowerToken, score);
                }
            }
        }

        // Strategy 2: Prefix Matching (if word length >= 2)
        if(lowerToken.Length >= 2) {
            var prefixMatches = _invertedIndex
                .Where(kvp => kvp.Key.StartsWith(lowerToken, StringComparison.OrdinalIgnoreCase))
                .SelectMany(kvp => kvp.Value);

            foreach(var posting in prefixMatches) {
                if(_fileIndex.TryGetValue(posting.FilePath, out var fileEntry)) {
                    var score = CalculateScore(posting.MatchType, MatchPosition.Prefix);
                    UpdateResult(results, fileEntry, posting.MatchType, lowerToken, score);
                }
            }
        }

        // Strategy 3: Substring Matching (Only When Exact Match Is Not Found)
        var hasResults = !results.IsEmpty;
        if(!hasResults && lowerToken.Length >= 2) {
            var substringMatches = _invertedIndex
                .Where(kvp => kvp.Key.Contains(lowerToken))
                .SelectMany(kvp => kvp.Value);

            foreach(var posting in substringMatches) {
                if(_fileIndex.TryGetValue(posting.FilePath, out var fileEntry)) {
                    var score = CalculateScore(posting.MatchType, MatchPosition.Substring);
                    UpdateResult(results, fileEntry, posting.MatchType, lowerToken, score);
                }
            }
        }
    }

    /// <summary>
    /// Calculate the match score
    /// </summary>
    private static int CalculateScore(MatchType type, MatchPosition position) {
        return (int)type + (int)position;
    }

    /// <summary>
    /// Update or create search results. 
    /// If the result already exists, add to the score and match details; 
    /// otherwise, create a new result.
    /// </summary>
    private static void UpdateResult(ConcurrentDictionary<string, SearchResult> results, FileEntry fileEntry, MatchType matchType, string matchedWord, int score) {
        results.AddOrUpdate(fileEntry.FilePath, new SearchResult {
            FilePath = fileEntry.FilePath,
            VaultName = fileEntry.VaultName,
            FileName = fileEntry.FileName,
            Extension = fileEntry.Extension,
            Tags = fileEntry.Tags,
            Length = fileEntry.Length,
            LastModifiedTicks = fileEntry.LastModifiedTicks,
            DisplayTitle = fileEntry.Title ?? fileEntry.FileName,

            TotalScore = score,
            MatchDetails = [(matchedWord, matchType, score)]
        },
        (key, existing) => {
            existing.TotalScore += score;
            existing.MatchDetails.Add((matchedWord, matchType, score));
            return existing;
        });
    }

    private void Clear() {
        _rebuildLock.EnterWriteLock();
        try {
            // Change state to running
            _indexState = IndexStateEnum.Pending;
            // 2. Stop the old watchers
            _watcherManager?.StopWatching();
            // 3. Clear all old data
            _invertedIndex.Clear();
            _fileIndex.Clear();
            _vaultDictionaries.Clear();
        } finally {
            _rebuildLock.ExitWriteLock();
        }
        _indexLock?.Dispose();
    }

    public void Dispose() {
        Clear();
    }
}

