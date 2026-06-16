
## Brief Description

This is a Command Palette extension project (VaultSearchExtension) for searching within Obsidian Vaults and local files. The project is built with .NET 10 and packaged/deployed using Windows MSIX tooling. Development and deployment are done in Visual Studio.

## Key Features
- Search files, content, and tags inside an Obsidian Vault or any folder
- Multilingual resource support (neutral resources in Simplified Chinese `zh-Hans`, English provided as an `en-US` satellite)
- Deployed as an MSIX package

## Prerequisites
- Windows 10 or later
- Visual Studio 2022/2026 (with MSIX packaging tools and Windows App SDK support)
- .NET 10 SDK

## Build & Deploy (common steps)

### Using Visual Studio
1. Open the solution `CmdPal.sln` in Visual Studio.
2. If you change `.csproj` (e.g., adding a `WinMDReference` or changing `PlatformTarget`), it's recommended to Clean, then Rebuild, and restart Visual Studio if necessary.
3. Deploy: use Visual Studio __Build > Deploy__ (note: use Deploy, not just Build) so the MSIX package and manifest registration take effect.

### Using `buildScript.ps1` packaging and signing script
1. By default the script uses the certificate file `${projectName}_TemporaryKey.pfx`
2. Logs are placed in the same directory as the script
3. Prerequisites:
   - `dotnet build` to produce MSIX (see: https://learn.microsoft.com/dotnet/core/tools/dotnet-build)
   - `makeappx` to create package bundles (see: https://learn.microsoft.com/windows/win32/appxpkg/make-appx-package--makeappx-exe-)
   - `winapp` CLI for signing (see: https://learn.microsoft.com/windows/apps/dev-tools/winapp-cli/)

> During development, if you need to reload the extension, use the __Reload__ command in the Command Palette.

## References
- Command Palette extension docs: https://learn.microsoft.com/windows/powertoys/command-palette/creating-an-extension
- .resx and resources management: https://learn.microsoft.com/dotnet/core/extensions/localization