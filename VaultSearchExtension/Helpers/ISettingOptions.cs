namespace CmdPal.VaultSearchExtension.Helpers;

internal interface ISettingOptions
{
    string VaultPath { get; }
    bool EnableTagsIndex { get; }
    bool EnableContentIndex { get; }
    bool EnableShowDetails { get; }
    bool EnableObsidianVault { get; }

}