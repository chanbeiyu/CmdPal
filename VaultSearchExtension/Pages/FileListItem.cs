using CmdPal.VaultSearchExtension.Commands;
using CmdPal.VaultSearchExtension.Helpers;
using CmdPal.VaultSearchExtension.Indexer.Files;
using CmdPal.VaultSearchExtension.Indexer.Search;
using CmdPal.VaultSearchExtension.Properties;
using Microsoft.CmdPal.Common.Commands;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Linq;
using Windows.System;

namespace CmdPal.VaultSearchExtension.Pages;

internal sealed partial class FileListItem: ListItem {
    public FileListItem(SearchResult result) {
        Title = result.FileName;
        Subtitle = result.FilePath;
        Icon = Icons.GetFile(result.Extension);
        Lazy<string> content = new(() => FileReaderFactory.GetReader(result.FilePath).ReadContent(result.FilePath));

        Command = new MarkdownPage(result, content.Value);
        MoreCommands = BuildMoreCommands(result, content.Value);

        Details = new Details {
            Title = result.DisplayTitle,
            Size = ContentSize.Medium,
            Metadata = BuildDetailsElement(result),
            Body = content.Value
        };
    }

    private static DetailsElement[] BuildDetailsElement(SearchResult result) => [
        new() {
            Key = Resource.detail_file_tag,
            Data = new DetailsTags()
            {
                Tags = [.. result.Tags.Select(t => new TagHelper(t))]
            }
        },
        new() {
            Key = Resource.detail_file_path,
            Data = new DetailsLink($"file://{result.FilePath}", result.FilePath)
        },
        new() {
            Key = Resource.detail_file_size,
            Data = new DetailsLink(result.Length + "")
        }, new()
        {
            Key = Resource.detail_file_last_modified,
            Data = new DetailsLink($"{result.LastModifiedTicks}")
        }
    ];


    private static CommandContextItem[] BuildMoreCommands(SearchResult result, string content) => [
        new CommandContextItem(new CopyContentCommand(content)){
            Title = Resource.action_copy,
        },
        //new CommandContextItem(new CopyPathCommand(result.FilePath)){
        //    Title = Resource.action_copy_path,
        //    RequestedShortcut = new KeyChord
        //    {
        //        Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
        //        Vkey = (int)VirtualKey.C,
        //        ScanCode = 0
        //    }
        //},
        //new CommandContextItem(new OpenFileCommand(result.FilePath)){
        //    Title = Resource.action_open,
        //    RequestedShortcut = new KeyChord
        //    {
        //        Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
        //        Vkey = (int)VirtualKey.O,
        //        ScanCode = 0
        //    }
        //},
        new CommandContextItem(new OpenWithCommand(result.FilePath)){
            Title = Resource.action_open_with,
            RequestedShortcut = new KeyChord
            {
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                Vkey = (int)VirtualKey.P,
                ScanCode = 0
            }
        },
        new CommandContextItem(new ShowFileInFolderCommand(result.FilePath)){
            Title = Resource.action_open_in_folder,
            RequestedShortcut = new KeyChord
            {
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                Vkey = (int)VirtualKey.E,
                ScanCode = 0
            }
        },
    ];

}
