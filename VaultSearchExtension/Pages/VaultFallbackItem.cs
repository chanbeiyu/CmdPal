using Microsoft.CommandPalette.Extensions.Toolkit;
using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Properties;

namespace CmdPal.VaultSearchExtension.Pages;

internal sealed partial class VaultFallbackItem : FallbackCommandItem
{

    private static readonly NoOpCommand BaseCommandWithId = new() { Id = Resource.page_fallback_id! };

    private string _searchText = string.Empty;
    private readonly SettingsManager _settingsManager;

    public VaultFallbackItem(SettingsManager settingsManager) 
        : base(BaseCommandWithId, Resource.app_display_title!, Resource.page_fallback_id!)
    {
        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = Icons.App;

        _settingsManager = settingsManager;
    }

    public override void UpdateQuery(string query)
    {
        _searchText = query;
        Title = Resource.app_display_title!;
        Subtitle = Resource.page_fallback_title + query;
        Command = new VaultDynamicListPage(_settingsManager, _searchText);
    }

}
