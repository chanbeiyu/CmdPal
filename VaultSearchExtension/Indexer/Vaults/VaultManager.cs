using CmdPal.VaultSearchExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CmdPal.VaultSearchExtension.Indexer.Vaults;

internal sealed class VaultManager {

    public static ReadOnlyDictionary<string, Filter> VaultFilters { get; private set; } = new ReadOnlyDictionary<string, Filter>(new Dictionary<string, Filter>(0));
    public static ReadOnlySet<VaultEntry> VaultEntries { get; private set; } = [];

    public static ReadOnlySet<VaultEntry> Initialize(ISettingOptions settingOptions) {
        HashSet<VaultEntry> vaults = [];
        if(settingOptions.EnableObsidianVault) {
            vaults.UnionWith(GetObsidianVaults());
        }
        if(!string.IsNullOrWhiteSpace(settingOptions.VaultPath)) {
            vaults.UnionWith(GetFolderVaults(settingOptions.VaultPath));
        }

        vaults = [.. vaults.DistinctBy(v => v.Id)];

        VaultFilters = vaults.ToDictionary(o => $"vault.filter.{o.Id}", o => new Filter() {
            Id = $"vault.filter.{o.Id}",
            Name = $"{o.VaultName}",
            Icon = o.Icon,
        }).AsReadOnly();

        ExtensionHost.LogMessage(new LogMessage($"===> Find Vaults：{vaults} VaultFilters：{VaultFilters}"));

        VaultEntries = vaults.AsReadOnly();
        return VaultEntries;
    }

    private static HashSet<VaultEntry> GetFolderVaults(string folders, string split = ";") {
        ExtensionHost.LogMessage(new LogMessage($"===> Find Folder Vaults From {folders}"));

        if(string.IsNullOrWhiteSpace(folders))
            return [];

        var vaultEntries = folders.Split(split, StringSplitOptions.RemoveEmptyEntries)
            .Where(f => Directory.Exists(f))
            .DistinctBy(f => f)
            .Select(f => new VaultEntry(Path.GetFileName(Path.TrimEndingDirectorySeparator(f)), f, VaultTypeEnum.Folder, Icons.Folder))
            .ToHashSet();

        ExtensionHost.LogMessage(new LogMessage($"===> Find {vaultEntries.Count} Folder Vaults: {vaultEntries}"));

        return vaultEntries;
    }

    private static HashSet<VaultEntry> GetObsidianVaults() {
        // Windows 配置路径 %APPDATA%\Obsidian\obsidian.json
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string obsidianConfigPath = Path.Combine(appData, "Obsidian", "obsidian.json");

        ExtensionHost.LogMessage(new LogMessage($"===> Find Obsidian Vaults Settings: {obsidianConfigPath}"));

        if(!File.Exists(obsidianConfigPath)) {
            ExtensionHost.LogMessage(new LogMessage("Obsidian configuration file doesn't exist, please open Obsidian at least once first"));
            return [];
        }

        try {
            string jsonContent = File.ReadAllText(obsidianConfigPath);
            var root = JsonDocument.Parse(jsonContent).RootElement;
            if(!root.TryGetProperty("vaults", out JsonElement vaultsEle) || vaultsEle.ValueKind != JsonValueKind.Object) {
                return [];
            }
            var vaultEntries = new HashSet<VaultEntry>();
            foreach(var prop in vaultsEle.EnumerateObject()) {
                var childObj = prop.Value;
                if(childObj.TryGetProperty("path", out JsonElement pathEle) && pathEle.ValueKind == JsonValueKind.String) {
                    string? p = pathEle.GetString();
                    if(p is not null && Directory.Exists(p))
                        vaultEntries.Add(new VaultEntry(Path.GetFileName(Path.TrimEndingDirectorySeparator(p)), p, VaultTypeEnum.Obsidian, Icons.Obsidian));
                }
            }

            ExtensionHost.LogMessage(new LogMessage($"===> Find {vaultEntries.Count} Obsidian Vaults: {vaultEntries}"));
            return vaultEntries;
        } catch(Exception ex) {
            ExtensionHost.LogMessage(new LogMessage($"===> Find Obsidian Vaults Error: {ex.Message}"));
            return [];
        }
    }
}
