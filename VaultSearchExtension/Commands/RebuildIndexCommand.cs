using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Indexer;
using CmdPal.VaultSearchExtension.Indexer.Vaults;
using CmdPal.VaultSearchExtension.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.ObjectModel;

namespace CmdPal.VaultSearchExtension.Commands;

internal sealed partial class RebuildIndexCommand: InvokableCommand {

    private readonly ISettingOptions _settingOptions;

    public RebuildIndexCommand(ISettingOptions settingOptions) {
        Name = Resource.item_reload_index_title;
        Icon = Icons.Cloud;
        _settingOptions = settingOptions;
    }

    public override CommandResult Invoke() {
        ReadOnlySet<VaultEntry> vaultSet = VaultManager.Initialize(_settingOptions);
        FileCacheManager.Instance.RebuildIndex(vaultSet, _settingOptions);
        return CommandResult.ShowToast(Resource.item_reload_index_toast);
    }

}
