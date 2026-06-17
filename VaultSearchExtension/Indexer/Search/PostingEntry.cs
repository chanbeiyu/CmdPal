namespace CmdPal.VaultSearchExtension.Indexer.Search;

internal struct PostingEntry {
    internal string FilePath;
    internal MatchType MatchType;
    internal MatchPosition MatchPosition;

    public override readonly int GetHashCode() => FilePath.GetHashCode();

    public readonly bool Equals(PostingEntry other) {
        return FilePath == other.FilePath;
    }

    public override readonly bool Equals(object? obj) {
        return obj is PostingEntry p && Equals(p);
    }

}