
# 设置编码，避免部分日志乱码
[console]::OutputEncoding = New-Object System.Text.UTF8Encoding

<#
.SYNOPSIS
    编译 MSIX 包并签名（可选）
.DESCRIPTION
    1. 可选替换 Package.appxmanifest 中的版本号（仅当显式传入 --version）
    2. 生成 x64 / ARM64 的 MSIX 包
    3. 将两个架构的 MSIX 合并为一个 .msixbundle
    4. 对 bundle 进行代码签名（可通过 --ss 跳过）
.PARAMETER name
    项目名称，默认 VaultSearchExtension
.PARAMETER version
    项目版本，仅显式传入时才会替换清单中的版本号，默认 0.0.1.0
.PARAMETER cert
    签名证书路径，默认 <name>_TemporaryKey.pfx
.PARAMETER pwd
    证书密码，默认 chanbeiyu
.PARAMETER path
    打包文件输入/输出目录，默认 ./AppPackages/AppX
.PARAMETER ss
    开关，存在该参数则跳过签名步骤
.EXAMPLE
    .\build.ps1 --name MyApp --version 1.2.3.4 --path .\out
    .\build.ps1 --ss --cert mycert.pfx
#>

param(
    [string]$name = "VaultSearchExtension", 
    [string]$version = "0.0.1.0",
    [string]$cert,
    [string]$pwd = "chanbeiyu",
    [string]$path = "./AppPackages/AppX",
    [switch]$ss
)

# 设置 cert 的默认值（依赖 $name）
if (-not $PSBoundParameters.ContainsKey('cert')) { 
    $cert = "${name}_TemporaryKey.pfx"
}

# ---------- 日志初始化（与脚本同目录） ----------
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Get-Location }
$logFile = Join-Path $scriptDir "$name.log"
"" | Out-File $logFile -Encoding utf8

function Write-Log {
    param([string]$Message)
    $timeStamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$timeStamp] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line -Encoding utf8
}

function Invoke-WithLogging {
    param([string]$Command)
    Write-Log "执行命令: $Command"

    # 执行命令，同时将输出追加到日志文件，控制台同步显示
    Invoke-Expression "& $Command 2>&1" -ErrorVariable errOutput |
        ForEach-Object {
            $_ | Out-File -Append $logFile -Encoding utf8
            Write-Host $_
        }

    if ($LASTEXITCODE -ne 0) {
        Write-Log "命令失败，退出代码: $LASTEXITCODE"
        Write-Log "请查看上方输出或日志文件获取详细错误"
        exit $LASTEXITCODE
    }
    Write-Log "命令执行成功"
}

# ---------- 0. 版本号处理 ----------
$effectiveVersion = $version
if ($PSBoundParameters.ContainsKey('version')) {
    Write-Log "已传递版本号 $version ，开始替换 Package.appxmanifest 中的 Version..."
    try {
        $manifestPath = "./Package.appxmanifest"
        $content = Get-Content $manifestPath -Raw
        $newContent = $content -replace '(Identity[^>]+?)Version="[\d\.]+"', "`$1Version=`"$version`""
        $newContent | Out-File $manifestPath -NoNewline -Encoding utf8
        Write-Log "版本号替换成功：$version"
    }
    catch {
        Write-Log "替换版本号失败: $_"
        exit 1
    }
}
else {
    Write-Log "未显式传递 --version，从清单中读取当前版本号..."
    try {
        $manifest = Get-Content "./Package.appxmanifest" -Raw
        if ($manifest -match 'Identity[^>]+?Version="([\d\.]+)"') {
            $effectiveVersion = $Matches[1]
            Write-Log "从清单读取到的版本号: $effectiveVersion"
        }
        else {
            Write-Log "无法从清单读取版本号，将使用默认值 $version"
        }
    }
    catch {
        Write-Log "读取清单失败，使用默认版本 $version"
    }
}
Write-Log "本次构建有效版本: $effectiveVersion"

# ---------- 1. 生成 MSIX ----------
Write-Log "开始生成 x64 MSIX..."
$buildX64 = "dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=x64 -p:AppxPackageDir=`"AppPackages\x64\`""
Invoke-WithLogging $buildX64

Write-Log "开始生成 ARM64 MSIX..."
$buildARM64 = "dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=ARM64 -p:AppxPackageDir=`"AppPackages\ARM64\`""
Invoke-WithLogging $buildARM64

# ---------- 2. 查找 MSIX 并合并 ----------
Write-Log "查找生成的 MSIX 文件..."
$msixList = Get-ChildItem -Path "AppPackages" -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue
if (-not $msixList) {
    Write-Log "在 AppPackages 下未找到 MSIX，尝试 bin\ 目录..."
    $msixList = Get-ChildItem -Path "bin" -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue
}
if (-not $msixList) {
    Write-Log "未找到任何 MSIX 文件，退出"
    exit 1
}
Write-Log "找到的 MSIX 文件:"
$msixList | ForEach-Object { Write-Log ("  " + $_.FullName) }

$x64File = $msixList | Where-Object { $_.Name -like "*_x64.msix" } | Select-Object -First 1
$arm64File = $msixList | Where-Object { $_.Name -like "*_arm64.msix" } | Select-Object -First 1

if (-not $x64File -or -not $arm64File) {
    Write-Log "未能同时找到 x64 和 arm64 的 MSIX，请检查生成结果"
    exit 1
}

# 确保 path 目录存在
if (-not (Test-Path $path)) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

Write-Log "将 MSIX 复制到 $path ..."
Copy-Item $x64File.FullName -Destination $path -Force
Copy-Item $arm64File.FullName -Destination $path -Force
Write-Log "复制完成"

Write-Log "开始合并 Bundle..."
$bundleFile = Join-Path $path "${name}_${effectiveVersion}_Bundle.msixbundle"
$makeappxCmd = "makeappx bundle -d `"$path`" -p `"$bundleFile`""
Invoke-WithLogging $makeappxCmd

# ---------- 3. 签名（可选） ----------
if ($ss) {
    Write-Log "检测到 --ss 参数，跳过签名步骤"
}
else {
    Write-Log "开始对 Bundle 签名..."
    $winappCmd = "winapp sign `"$bundleFile`" `"$cert`" --password `"$pwd`""
    Invoke-WithLogging $winappCmd
}

Write-Log "所有步骤完成。输出目录: $path"
Write-Log "日志文件: $logFile"