namespace CmdPal.VaultSearchExtension.Indexer.Files;

internal interface IFileReader
{
    string ReadContent(string filePath);

    FileEntry ReadEntry(string filePath, string vaultName, int maxPreviewKB = 10);

}