using CmdPal.VaultSearchExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal.VaultSearchExtension.Indexer.Vaults;

internal struct VaultEntry(string vaultName, string vaultRootPath, VaultTypeEnum vaultType, IconInfo icon) {
    internal string Id = UtileHelper.ComputeMD5(vaultRootPath);
    internal string VaultName = vaultName;
    internal string VaultRootPath = vaultRootPath;
    internal VaultTypeEnum VaultType = vaultType;
    internal IconInfo Icon = icon;

    public override readonly string ToString() {
        return $"Id:={Id}, VaultName={VaultName}, VaultRootPath={VaultRootPath}, VaultType={VaultType}, Icon={Icon}";
    }
}