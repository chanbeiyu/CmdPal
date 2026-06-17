using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Indexer.Vaults;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;

namespace CmdPal.VaultSearchExtension.Indexer;

internal enum ChangeType { Created, Modified, Deleted }

internal sealed class FileChangeEvent {
    internal required string FilePath;
    internal ChangeType ChangeType;
    internal required string VaultName;
    internal required string VaultRootPath;
    internal DateTime Timestamp;
}

internal sealed partial class FileWatcherManager(FileCacheManager cacheManager): IDisposable {

    // Key: Vault Path
    private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

    // FileChangeEvent Queue，Batch processing to reduce frequent index updates
    private readonly ConcurrentQueue<FileChangeEvent> _changeQueue = [];

    private Timer? _batchProcessTimer;

    /// <summary>
    /// Start monitoring the specified list of repositories, which are provided in the form of name and path.
    /// For each repository, create a FileSystemWatcher to monitor file system change events (creation, modification, deletion, renaming).
    /// When the watcher detects relevant events, add the event information (file path, change type, repository name and path) to the change event queue.
    /// Periodically process the change events in batches through a timer to update the index data in the cache manager.
    /// </summary>
    internal void StartWatching(ReadOnlySet<VaultEntry> vaultSet) {
        foreach(var vault in vaultSet) {
            try {
                var watcher = new FileSystemWatcher(vault.VaultRootPath) {
                    IncludeSubdirectories = true,
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName |
                                  NotifyFilters.LastWrite |
                                  NotifyFilters.CreationTime |
                                  NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true,
                    InternalBufferSize = 65536 // 64KB缓冲区，防止丢事件
                };

                var vaultName = vault.VaultName;
                var vaultRootPath = vault.VaultRootPath;

                watcher.Created += (s, e) => OnFileChanged(e.FullPath, ChangeType.Created, vaultName, vaultRootPath);
                watcher.Changed += (s, e) => OnFileChanged(e.FullPath, ChangeType.Modified, vaultName, vaultRootPath);
                watcher.Deleted += (s, e) => OnFileChanged(e.FullPath, ChangeType.Deleted, vaultName, vaultRootPath);
                watcher.Renamed += (s, e) => {
                    OnFileChanged(e.OldFullPath, ChangeType.Deleted, vaultName, vaultRootPath);
                    OnFileChanged(e.FullPath, ChangeType.Created, vaultName, vaultRootPath);
                };

                watcher.Error += (s, e) => {
                    LogHelper.Info($"file watcher fail: {vault.VaultName} - {e.GetException().Message}");
                };

                _watchers[vault.VaultRootPath] = watcher;
                LogHelper.Info($"start file watcher: {vault.VaultName} ({vault.VaultRootPath})");
            } catch(Exception ex) {
                LogHelper.Error($"start file watcher fail: {vault.VaultName}", ex);
            }
        }

        _batchProcessTimer = new Timer(ProcessBatchChanges, null, 500, 500);
    }

    private void OnFileChanged(string filePath, ChangeType changeType, string vaultName, string vaultRootPath) {
        var ext = Path.GetExtension(filePath)?.ToLower(CultureInfo.CurrentCulture);
        if(ext != ".md" && ext != ".txt") return;

        _changeQueue.Enqueue(new FileChangeEvent {
            FilePath = filePath,
            ChangeType = changeType,
            VaultName = vaultName,
            VaultRootPath = vaultRootPath,
            Timestamp = DateTime.UtcNow
        });
    }

    private void ProcessBatchChanges(object? state) {
        // Deduplication: Only handle the last change if the same file is modified multiple times
        var filesToProcess = new Dictionary<string, FileChangeEvent>();

        while(_changeQueue.TryDequeue(out var change)) {
            filesToProcess[change.FilePath] = change; // Keep the latest changes
        }

        foreach(var kvp in filesToProcess) {
            var change = kvp.Value;
            try {
                switch(change.ChangeType) {
                    case ChangeType.Created:
                        if(File.Exists(change.FilePath))
                            cacheManager.AddOrUpdateFile(change.FilePath, change.VaultName);
                        break;
                    case ChangeType.Modified:
                        if(File.Exists(change.FilePath))
                            cacheManager.AddOrUpdateFile(change.FilePath, change.VaultName);
                        break;
                    case ChangeType.Deleted:
                        cacheManager.RemoveFile(change.FilePath);
                        break;
                }
            } catch(Exception ex) {
                LogHelper.Error($"handle file changes fail: {change.FilePath}", ex);
            }
        }
    }

    public void StopWatching() {
        _batchProcessTimer?.Dispose();

        foreach(var watcher in _watchers.Values) {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        _changeQueue?.Clear();
    }

    public void Dispose() {
        StopWatching();
    }
}
