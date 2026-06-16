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
            new MarkdownContent("### Commands list\n"),
            new MarkdownContent("- [ > ] + [ Space ␣ ] to display the command \n"),
        ];
    }

}
