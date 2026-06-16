## 简要说明

这是一个针对 Command Palette 的扩展项目（VaultSearchExtension），用于在 Obsidian Vault / 本地文件中进行搜索。项目基于 .NET 10 和 Windows MSIX 打包/部署工具构建，使用 Visual Studio 进行开发和部署。

## 主要特性
- 在 Obsidian Vault 或 任意文件夹中 搜索文件、内容与标签
- 多语言资源支持（默认中性资源为简体中文 zh-Hans，英文作为 en-US 卫星资源）
- 以 MSIX 包方式部署

## 先决条件
- Windows 10 及以上
- Visual Studio 2022/2026（带 MSIX 打包工具和 Windows App SDK 支持）
- .NET 10 SDK

## 构建与部署（常用步骤）

### 使用 Visual Studio
1. 在 Visual Studio 中打开解决方案 CmdPal.sln。
2. 若更改了 csproj（如添加 WinMDReference 或更改 PlatformTarget），建议先 Clean，再 Rebuild，必要时重启 Visual Studio。
3. 部署：使用 Visual Studio 的 Build > Deploy（注意是 Deploy 而不是仅 Build），以便 MSIX 包和清单注册生效。

### 使用 buildScript.ps1 打包签名脚本
1. 默认使用 ${projectName}_TemporaryKey.pfx 证书文件
2. 日志在与脚本同目录下
3. 前置条件：
	- [dotnet build 生成 MSIX](https://learn.microsoft.com/zh-cn/dotnet/core/tools/dotnet-build)
	- [makeappx 创建捆绑包](https://learn.microsoft.com/zh-cn/windows/win32/appxpkg/make-appx-package--makeappx-exe-)
	- [winapp cli 签名](https://learn.microsoft.com/zh-cn/windows/apps/dev-tools/winapp-cli/)

> 若在开发过程中需重新加载扩展，使用 Command Palette 中的 Reload 命令。

## 常用参考
- Command Palette 扩展文档：https://learn.microsoft.com/windows/powertoys/command-palette/creating-an-extension
- .resx 与资源管理：https://learn.microsoft.com/dotnet/core/extensions/localization
