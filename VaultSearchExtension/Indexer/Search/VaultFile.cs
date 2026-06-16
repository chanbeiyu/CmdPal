using System.Collections.Generic;

namespace CmdPal.VaultSearchExtension.Indexer.Search;

internal sealed class VaultFile
{
    public required string VaultName;
    public required string VaultRootPath;
    public required HashSet<string> VaultFiles;
}
