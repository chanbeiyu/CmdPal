using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.ObjectModel;
using System.IO;
using CmdPal.VaultSearchExtension.Indexer;
using CmdPal.VaultSearchExtension.Indexer.Vaults;
using CmdPal.VaultSearchExtension.Properties;

namespace CmdPal.VaultSearchExtension.Helpers;

internal sealed class SettingsManager : JsonSettingsManager, ISettingOptions
{
    private static readonly string _namespace = "vault.search";
    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";
    private readonly static string vaultPathNamespaed = Namespaced(nameof(VaultPath));
    private readonly static string tagsIndexNamespaed = Namespaced(nameof(EnableTagsIndex));
    private readonly static string contentTypeNamespaed = Namespaced(nameof(EnableContentIndex));
    private readonly static string showDetailsNamespaed = Namespaced(nameof(EnableShowDetails));
    private readonly static string obsidianVaultNamespaed = Namespaced(nameof(EnableObsidianVault));

    private readonly TextSetting _vaultPath = new(
        vaultPathNamespaed,
        Resource.settings_vault_path_title,
        Resource.settings_vault_path_description,
        string.Empty);

    private readonly ToggleSetting _enableTagsIndex = new(
        tagsIndexNamespaed,
        Resource.settings_index_tags_title!,
        Resource.settings_index_tags_description!,
        true);

    private readonly ToggleSetting _enableContentIndex = new(
        contentTypeNamespaed,
        Resource.settings_index_content_title!,
        Resource.settings_index_content_description!,
        false);

    private readonly ToggleSetting _enableShowDetails = new(
        showDetailsNamespaed,
        Resource.settings_show_details_title!,
        Resource.settings_show_details_description!,
        false);

    private readonly ToggleSetting _enableObsidianVault = new(
        obsidianVaultNamespaed,
        Resource.settings_obsidian_vault_title!,
        Resource.settings_obsidian_vault_description!,
        false);

    public string VaultPath => _vaultPath.Value ?? "";
    public bool EnableTagsIndex => _enableTagsIndex.Value;
    public bool EnableContentIndex => _enableContentIndex.Value;
    public bool EnableShowDetails => _enableShowDetails.Value;
    public bool EnableObsidianVault => _enableObsidianVault.Value;
    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{_namespace}.settings.json");
    }

    public bool VaultPathSettinged => !string.IsNullOrWhiteSpace(VaultPath) || EnableObsidianVault;

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_vaultPath);
        Settings.Add(_enableObsidianVault);
        //Settings.Add(_enableTagsIndex);
        //Settings.Add(_enableContentIndex);
        Settings.Add(_enableShowDetails);
        LoadSettings();

        Settings.SettingsChanged += (s, a) =>
        {
            SaveSettings();
            ReadOnlySet<VaultEntry> vaultSet = VaultManager.Initialize(this);
            FileCacheManager.Instance.RebuildIndex(vaultSet, this);
        };
    }

    public override string ToString()
    {
        return $"{{{nameof(VaultPath)}={VaultPath}, {nameof(EnableTagsIndex)}={EnableTagsIndex}, {nameof(EnableContentIndex)}={EnableContentIndex.ToString()}, {nameof(EnableShowDetails)}={EnableShowDetails}, {nameof(EnableObsidianVault)}={EnableObsidianVault}, {nameof(VaultPathSettinged)}={VaultPathSettinged.ToString()}, {nameof(Settings)}={Settings}, {nameof(FilePath)}={FilePath}}}";
    }
}
