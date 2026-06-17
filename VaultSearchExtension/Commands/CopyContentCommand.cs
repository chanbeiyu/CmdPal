using CmdPal.VaultSearchExtension.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal.VaultSearchExtension.Commands;

internal sealed partial class CopyContentCommand: InvokableCommand {
    public string Text { get; set; }

    public CopyContentCommand(string text) {
        Text = text;
        Name = Resource.action_copy_content;
        Icon = Icons.Copy;
    }

    public override ICommandResult Invoke() {
        ClipboardHelper.SetText(Text);
        return CommandResult.ShowToast(Resource.action_copy_toast);
    }
}
