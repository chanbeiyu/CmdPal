# Repository Guidelines

## Project Structure & Module Organization

This repository contains a .NET Command Palette extension for searching Obsidian vaults and local files. The solution entry point is `CmdPal.sln`; the main project is `VaultSearchExtension/VaultSearchExtension.csproj`.

- `VaultSearchExtension/*.cs`: extension entry point, command provider, icons, and core wiring.
- `VaultSearchExtension/Pages/`: Command Palette pages and list items.
- `VaultSearchExtension/Properties/`: resources, launch settings, publish profiles, and config JSON.
- `VaultSearchExtension/Assets/`: MSIX and Command Palette image assets.
- `Directory.Build.props` and `Directory.Packages.props`: shared analyzer, platform, and NuGet version settings.

Treat `bin/`, `obj/`, `AppPackages/`, logs, `.pfx`, and generated `.cer` files as build artifacts unless packaging work requires them.

## Build, Test, and Development Commands

- `dotnet restore CmdPal.sln`: restore centrally managed NuGet packages.
- `dotnet build CmdPal.sln -c Debug -p:Platform=x64`: compile a local x64 debug build.
- `dotnet build CmdPal.sln -c Release -p:Platform=x64`: produce a release build with MSIX tooling enabled.
- `powershell -ExecutionPolicy Bypass -File VaultSearchExtension/buildScript.ps1`: run the packaging/signing script when creating distributable MSIX output.

Visual Studio is the primary development path. Open `CmdPal.sln`, use Clean/Rebuild after project file changes, and use **Build > Deploy** to register the package.

## Coding Style & Naming Conventions

Use C# with nullable reference types enabled. Follow the existing file-scoped namespace style (`namespace CmdPal.VaultSearchExtension;`). Public types and methods use PascalCase, private fields use `_camelCase`, and locals/parameters use camelCase.

Keep UI strings in `.resx` files instead of hardcoding text. Put package versions only in `Directory.Packages.props`. The repo enables .NET analyzers in Recommended mode, so fix warnings before submitting.

## Testing Guidelines

No dedicated test project is currently present. For logic-heavy changes, add focused tests in a new test project. Until automated coverage exists, validate with:

- `dotnet build CmdPal.sln -c Debug -p:Platform=x64`
- Visual Studio Deploy
- Manual Command Palette checks for vault search, fallback search, settings, localization, and help pages

## Commit & Pull Request Guidelines

Recent history uses short imperative summaries such as `Add readme file`, `Fix: ...`, and Chinese `Fix` descriptions. Keep commits concise and scoped, for example `Fix search reset behavior`.

Pull requests should describe the user-visible change, list validation steps, link issues when available, and include screenshots or recordings for UI changes. Note packaging, certificate, manifest, or localization updates explicitly.

## Security & Configuration Tips

Do not commit personal vault paths, secrets, production certificates, or machine-specific `.csproj.user` changes. Review `Package.appxmanifest`, publish profiles, and `Properties/config/*.json` when changing package identity, signing, or deployment behavior.
