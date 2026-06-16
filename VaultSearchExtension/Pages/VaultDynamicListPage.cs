using CmdPal.VaultSearchExtension.Commands;
using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Indexer.Vaults;
using CmdPal.VaultSearchExtension.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;

namespace CmdPal.VaultSearchExtension.Pages;

internal sealed partial class VaultDynamicListPage : DynamicListPage
{
    private bool isLoading;
    private int pageCursor;
    private readonly int pageLimit = 10;
    private string? _filterName;
    private readonly List<IListItem> dynamicListItems = [];

    private readonly SettingsManager _settingsManager;

    public VaultDynamicListPage(SettingsManager settingsManager, string query = "")
    {
        Id = Resource.page_dynamic_id;
        Name = Resource.page_dynamic_name;
        Title = Resource.page_dynamic_title;
        Icon = Icons.App;

        HasMoreItems = true;
        SearchText = query;
        PlaceholderText = Resource.page_dynamic_placeholder;

        _settingsManager = settingsManager;

        var filters = new VaultFilters();
        filters.PropChanged += FiltersPropChanged;
        Filters = filters;
    }

    public override bool ShowDetails => _settingsManager.EnableShowDetails;

    private void FiltersPropChanged(object sender, IPropChangedEventArgs args)
    {
        VaultFilters? vaultFilters = sender as VaultFilters;
        if (vaultFilters is not null)
        {
            if (VaultManager.VaultFilters.TryGetValue(vaultFilters.CurrentFilterId, out var filter))
                _filterName = filter.Name;
            else
                _filterName = null;
        }

        pageCursor = 0;
        dynamicListItems.Clear();
        LoadNextItems();
        RaiseItemsChanged();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        pageCursor = 0;
        dynamicListItems.Clear();
        LoadNextItems();
        RaiseItemsChanged();
    }

    public override void LoadMore()
    {
        if (isLoading) return;
        _ = LoadNextItems();
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        if (VaultManager.VaultEntries.Count == 0)
            return [SettingsItem(Resource.item_no_vault_path_title, Resource.item_no_vault_path_subtitle)];
        if (!string.IsNullOrWhiteSpace(_settingsManager.VaultPath) && !Directory.Exists(_settingsManager.VaultPath))
            return [SettingsItem(Resource.item_error_vault_path_title, Resource.item_error_vault_path_subtitle)];
        if (string.IsNullOrWhiteSpace(SearchText))
            return [];
        if (SearchText.StartsWith(Resource.constants_command_start!, StringComparison.OrdinalIgnoreCase))
            return CommandItems();

        return [.. dynamicListItems];
    }


    private IReadOnlyList<IListItem> LoadNextItems()
    {
        isLoading = true;
        IReadOnlyList<IListItem> items = SearchHelper.Search(SearchText, pageCursor, pageLimit, _filterName);
        if (items.Count > 0)
        {
            dynamicListItems.AddRange(items);
            pageCursor += pageLimit;
            HasMoreItems = true;
        }
        else
        {
            HasMoreItems = false;
        }
        isLoading = false;
        return items;
    }

    private IListItem[] CommandItems()
    {
        return [
            SettingsItem(Resource.item_settings_title, Resource.item_settings_subtitle),
            new ListItem()
            {
                Title = Resource.item_reload_index_title,
                Subtitle = Resource.item_reload_index_subtitle,
                Icon = Icons.Cloud,
                Command = new RebuildIndexCommand(_settingsManager)
            },
            new ListItem(new HelpPage())
        ];
    }

    private ListItem SettingsItem(string title, string subTitle)
    {
        return new ListItem(_settingsManager.Settings.SettingsPage)
        {
            Title = title,
            Subtitle = subTitle,
            Icon = Icons.Settings
        };
    }

}
