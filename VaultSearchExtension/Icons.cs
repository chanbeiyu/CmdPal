using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace CmdPal.VaultSearchExtension;

/// <summary>
/// https://learn.microsoft.com/zh-cn/windows/apps/design/iconography/segoe-fluent-icons-font
/// </summary>
internal static class Icons {

    internal static IconInfo App { get; } = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
    internal static IconInfo TextFile { get; } = IconHelpers.FromRelativePath("Assets\\txt.png");
    internal static IconInfo MarkdownFile { get; } = IconHelpers.FromRelativePath("Assets\\markdown.png");
    internal static IconInfo Obsidian { get; } = IconHelpers.FromRelativePath("Assets\\obsidian.png");
    internal static IconInfo Folder { get; } = IconHelpers.FromRelativePath("Assets\\folder.png");

    internal static IconInfo Copy { get; } = new("\ue8c8");
    internal static IconInfo Search { get; } = new("\ue721");
    internal static IconInfo Command { get; } = new("\ue756");
    internal static IconInfo Settings { get; } = new("\ue713");
    internal static IconInfo Cloud { get; } = new("\ue753");
    internal static IconInfo Help { get; } = new("\ue897");

    internal static IconInfo GetFile(string file) {
        return file.EndsWith("md", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith("markdown", StringComparison.OrdinalIgnoreCase) ? MarkdownFile : TextFile;
    }


}
