using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using CmdPal.VaultSearchExtension.Properties;

namespace CmdPal.VaultSearchExtension.Pages;

internal sealed partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        Id = Resource.page_help_id;
        Name = Resource.page_help_title;
        Title = Resource.page_help_title;
        Icon = Icons.Help;
    }

    public override IContent[] GetContent()
    {
        return [
            new MarkdownContent("## 主要功能"),
            new MarkdownContent("- 自动识别 Obsidian Vault"),
            new MarkdownContent("- 多个文件夹内文件搜索"),
            new MarkdownContent("- 文件名、Tags 及有限的内容搜索"),
            new MarkdownContent("- 拼音、首字母模糊搜索"),
            new MarkdownContent("## 插件可执行的操作"),
            new MarkdownContent("- 搜索页面输入 [ > ] + [ Space ␣ ] 显示插件可执行的操作"),
            new MarkdownContent("- 打开设置页面"),
            new MarkdownContent("- 重建索引，插件关闭后台重建索引，可能需要待定数秒，与文件数量大小有关"),
            new MarkdownContent("- 帮助：打开本页面 "),
            new MarkdownContent("## 设置页面"),
            new MarkdownContent("- 可配置多个目录，使用英文的分号 [;] 分隔即可。错误的配置或目录将被忽略"),
            new MarkdownContent("- 勾选 Obsidian Vault 后自动扫描 Obsidian Vault"),
            new MarkdownContent("- Obsidian 仓库需要在 Obsidian 中打开过生成配置才能自动识别，假如无法识别 Obsidian 仓库，推荐使用配置路径的方式达到同样的效果"),
            new MarkdownContent("- 勾选详情，选中文件后自动显示文件信息")
        ];
    }

}
