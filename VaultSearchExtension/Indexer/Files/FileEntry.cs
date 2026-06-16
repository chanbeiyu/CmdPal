namespace CmdPal.VaultSearchExtension.Indexer.Files;

internal struct FileEntry(int id, string filePath, string vaultName, string fileName,
                 string extension, string title, string[] tags, long length, long lastModifiedTicks,
                 string contentPreview)
{
    internal int Id = id;
    internal string FilePath = filePath;
    internal string VaultName = vaultName;
    internal string FileName = fileName;
    internal string Extension = extension;
    internal string Title = title;
    internal string[] Tags = tags ?? [];
    internal long Length = length;
    internal long LastModifiedTicks = lastModifiedTicks;
    internal string Content = contentPreview;

    public override readonly string ToString()
    {
        return $"Id={Id}, VaultName={VaultName}, Title={Title}, Tags={Tags}, FileName={FileName}, Extension={Extension}, FilePath={FilePath}";
    }
}
