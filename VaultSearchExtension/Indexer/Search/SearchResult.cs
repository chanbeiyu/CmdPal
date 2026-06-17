using System.Collections.Generic;

namespace CmdPal.VaultSearchExtension.Indexer.Search;

internal sealed class SearchResult {
    internal required string FilePath;
    internal required string VaultName;
    internal required string FileName;
    internal required string Extension;
    internal required long Length;
    internal required string[] Tags;
    internal long LastModifiedTicks;
    internal required string DisplayTitle;

    internal int TotalScore;
    internal required List<(string MatchedWord, MatchType Type, int Score)> MatchDetails;


}
