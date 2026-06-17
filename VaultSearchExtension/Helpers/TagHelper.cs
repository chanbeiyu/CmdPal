using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;
using System.Threading;

namespace CmdPal.VaultSearchExtension.Helpers;

internal sealed partial class TagHelper(string text): ITag {
    // 7 组 高对比度配对色：前景 + 背景（循环使用）
    private static readonly List<(OptionalColor Fore, OptionalColor Back)> _tagColors = new()
    {
        (ColorHelpers.FromArgb(255, 255, 255, 255), ColorHelpers.FromArgb(255,  76, 110, 245)), // 白 + 蓝紫
        (ColorHelpers.FromArgb(255, 255, 255, 255), ColorHelpers.FromArgb(255,  34, 139, 230)), // 白 + 蓝
        (ColorHelpers.FromArgb(255, 255, 255, 255), ColorHelpers.FromArgb(255,  21, 170, 191)), // 白 + 青
        (ColorHelpers.FromArgb(255,   0,   0,   0), ColorHelpers.FromArgb(255, 148, 216,  45)), // 黑 + 绿
        (ColorHelpers.FromArgb(255,   0,   0,   0), ColorHelpers.FromArgb(255, 255, 212,  59)), // 黑 + 黄
        (ColorHelpers.FromArgb(255, 255, 255, 255), ColorHelpers.FromArgb(255, 250,  82,  82)), // 白 + 红
        (ColorHelpers.FromArgb(255, 255, 255, 255), ColorHelpers.FromArgb(255, 155, 102, 239)), // 白 + 紫
    };

    // 自增索引，自动循环 0~6
    private static int _colorIndex;
    private static int NextColorIndex => Interlocked.Increment(ref _colorIndex) % _tagColors.Count;

    // 自动循环配色
    private readonly (OptionalColor Fore, OptionalColor Back) _color = _tagColors[NextColorIndex];

    public OptionalColor Background => _color.Back;
    public OptionalColor Foreground => _color.Fore;
    public IIconInfo Icon => default!;
    public string Text { get; } = text;
    public string ToolTip => Text;
}