// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CmdPal.VaultSearchExtension;

[Guid("fe01f7ac-d6bb-4172-9a85-e640f77e28f6")]
public sealed partial class VaultSearchExtension: IExtension, IDisposable {
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly VaultSearchExtensionCommandsProvider _provider = new();

    public VaultSearchExtension(ManualResetEvent extensionDisposedEvent) {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType) {
        return providerType switch {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}
