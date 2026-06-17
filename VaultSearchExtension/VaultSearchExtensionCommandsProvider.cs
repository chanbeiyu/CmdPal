// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Indexer;
using CmdPal.VaultSearchExtension.Indexer.Vaults;
using CmdPal.VaultSearchExtension.Pages;
using CmdPal.VaultSearchExtension.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.ObjectModel;

namespace CmdPal.VaultSearchExtension;

public partial class VaultSearchExtensionCommandsProvider: CommandProvider {
    private readonly ICommandItem[] _topLevelCommands;
    private readonly IFallbackCommandItem[] _fallbackCommands;
    private readonly SettingsManager _settingsManager = new();

    public VaultSearchExtensionCommandsProvider() {
        DisplayName = Resource.app_display_title;
        Icon = Icon = Icons.App;
        Settings = _settingsManager.Settings;

        _topLevelCommands = [
            new CommandItem(new VaultDynamicListPage(_settingsManager)) {
                Title = Resource.app_display_title,
                Subtitle = string.Empty,
                Icon = Icons.App,
                //Subtitle = Resource.app_display_subtitle,

                MoreCommands = [
                   new CommandContextItem(Settings.SettingsPage){
                        Title = Resource.action_settings,
                   },
               ],
            },
        ];

        _fallbackCommands = [
             new VaultFallbackItem(_settingsManager)
         ];
    }

    public override ICommandItem[] TopLevelCommands() => _topLevelCommands;

    public override IFallbackCommandItem[] FallbackCommands() => _fallbackCommands;

    public override void InitializeWithHost(IExtensionHost host) {
        ExtensionHost.Initialize(host);
        if(_settingsManager.VaultPathSettinged) {
            ReadOnlySet<VaultEntry> vaultSet = VaultManager.Initialize(_settingsManager);
            FileCacheManager.Instance.RebuildIndex(vaultSet, _settingsManager);
        }
    }

}
