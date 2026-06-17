using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;

namespace CmdPal.VaultSearchExtension.Helpers;

internal sealed class LogHelper {
    public static void Success(string msg) {
        ExtensionHost.LogMessage(new LogMessage() {
            State = MessageState.Success,
            Message = $"[VaultSearch] ===> {msg}",
        });
    }

    public static void Info(string msg) {
        ExtensionHost.LogMessage(new LogMessage() {
            State = MessageState.Info,
            Message = $"[VaultSearch] ===> {msg}"
        });
    }

    public static void Warning(string msg) {
        ExtensionHost.LogMessage(new LogMessage() {
            State = MessageState.Warning,
            Message = $"[VaultSearch] ===> {msg}"
        });
    }

    public static void Error(string msg) {
        ExtensionHost.LogMessage(new LogMessage() {
            State = MessageState.Error,
            Message = $"[VaultSearch] ===> {msg}"
        });
    }

    public static void Error(string msg, Exception ex) {
        ExtensionHost.LogMessage(new LogMessage() {
            State = MessageState.Error,
            Message = $"[VaultSearch] ===> {msg}, E: {ex.Message}"
        });
    }

    public static void Debug(string msg) {
        System.Diagnostics.Debug.WriteLine($"===> {msg}");
    }

}
