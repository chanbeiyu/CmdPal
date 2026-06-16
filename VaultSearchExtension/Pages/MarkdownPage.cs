using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using CmdPal.VaultSearchExtension.Indexer.Search;
using CmdPal.VaultSearchExtension.Properties;

namespace CmdPal.VaultSearchExtension.Pages;

internal sealed partial class MarkdownPage : ContentPage
{
    private readonly string _content;

    public MarkdownPage(SearchResult result, string content)
    {
        Id = Resource.page_markdown_id;
        Name = Resource.page_markdown_title;
        Title = result.DisplayTitle;
        Icon = Icons.GetFile(result.Extension);
        _content = content;
    }

    public override IContent[] GetContent()
    {
        return [
            new MarkdownContent(_content),
        ];
    }

}
