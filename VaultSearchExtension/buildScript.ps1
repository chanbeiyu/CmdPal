<#
.SYNOPSIS
    编译 MSIX 包并签名（可选）
.DESCRIPTION
    1. 根据参数替换 .csproj 和 .appxmanifest 中的身份、发布者、版本等信息
    2. 生成 x64 / ARM64 的 MSIX 包
    3. 将两个架构的 MSIX 合并为一个 .msixbundle
    4. 对 bundle 进行代码签名（非 Prod 环境）
.PARAMETER Env
    环境名称：Dev / Prod
.EXAMPLE
    .\build.ps1 -Env Dev
    .\build.ps1 -Env Prod
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Dev", "Test", "Prod")]
    [string]$Env
)

# 设置编码，避免部分日志乱码
[console]::OutputEncoding = New-Object System.Text.UTF8Encoding

# ---------- 1. 日志初始化（与脚本同目录） ----------
$now = Get-Date -Format "yyyy-MM-dd"
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Get-Location }
$logFile = Join-Path $scriptDir "$now.log"
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

# ---------- 2. 工具函数：递归合并JSON对象（环境覆盖基础配置） ----------
function Merge-JsonObject {
    param(
        [Parameter(Mandatory)]
        [PSObject]$BaseObj,
        [Parameter(Mandatory)]
        [PSObject]$OverrideObj
    )
    $result = $BaseObj.PSObject.Copy()

    foreach ($prop in $OverrideObj.PSObject.Properties) {
        $key = $prop.Name
        $ovVal = $prop.Value

        # 如果两边都是对象，递归合并
        if ($result.$key -is [PSCustomObject] -and $ovVal -is [PSCustomObject]) {
            $result.$key = Merge-JsonObject -BaseObj $result.$key -OverrideObj $ovVal
        }
        else {
            # 直接覆盖值
            $result.$key = $ovVal
        }
    }
    return $result
}

# 辅助：以 UTF-8 无 BOM 保存 XmlDocument
function Save-XmlNoBom {
    param(
        [Parameter(Mandatory)] [xml]$XmlDoc,
        [Parameter(Mandatory)] [string]$Path
    )
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)  # 无 BOM
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $XmlDoc.Save($writer)
    } finally {
        $writer.Close()
    }
}

# ---------- 3. 加载配置逻辑 ----------
$baseJsonPath = Join-Path $scriptDir "Properties/config/appsettings.json"
$envJsonPath = Join-Path $scriptDir "Properties/config/appsettings.$Env.json"

# 校验基础配置必须存在
if (-not (Test-Path $baseJsonPath -PathType Leaf)) {
    Write-Log "基础配置文件不存在：$baseJsonPath"
    exit 1
}

# 读取基础配置
$baseContent = Get-Content $baseJsonPath -Raw -Encoding utf8
$baseConfig = $baseContent | ConvertFrom-Json

# 读取环境配置（可选，不存在不报错）
$finalConfig = $baseConfig
if (Test-Path $envJsonPath -PathType Leaf) {
    $envContent = Get-Content $envJsonPath -Raw -Encoding utf8
    $envConfig = $envContent | ConvertFrom-Json
    # 合并，环境配置覆盖基础
    $finalConfig = Merge-JsonObject -BaseObj $baseConfig -OverrideObj $envConfig
}
else {
    Write-Log "环境配置文件 $envJsonPath 不存在，仅使用基础配置"
}

$name = $finalConfig.Name
$version = $finalConfig.Version
$identityName = $finalConfig.IdentityName
$identityPublisher = $finalConfig.IdentityPublisher
$publisherDisplayName = $finalConfig.PublisherDisplayName
$cert = $finalConfig.Cert
$password = $finalConfig.Password
$path = $finalConfig.Path
$writerType = $finalConfig.WriterType

# ---------- 4. 配置信息输出 ----------
Write-Log "==================== 当前构建环境：$Env ===================="
Write-Log "======> 项目名称：$name"
Write-Log "======> 项目版本：$version"
Write-Log "======> 包名称：$identityName"
Write-Log "======> 发布者：$identityPublisher"
Write-Log "======> 发布者显示名称：$publisherDisplayName"
Write-Log "======> 证书文件位置：$cert"
Write-Log "======> 证书签名密码：$password"
Write-Log "======> 打包输出位置：$path"
Write-Log "======> 参数修改方式：$writerType"


# ---------- 5. 预处理：替换 .csproj 和 .appxmanifest 中的元数据 ----------

Write-Log "开始替换项目元数据..."

# ---------- 5.1. 替换 VaultSearchExtension.csproj ----------
$csprojPath = Join-Path $scriptDir "VaultSearchExtension.csproj"
if (Test-Path $csprojPath) {
    Write-Log "更新 $csprojPath（使用 XML API）..."
    try {
        if($writerType -ieq "xml") {
            # ---------- xml 方式 ----------
            [xml]$csprojXml = Get-Content $csprojPath -Raw -Encoding utf8
            # 找到包含目标元素的 PropertyGroup
            $pg = $csprojXml.Project.PropertyGroup | Where-Object { $_.AppxPackageIdentityName -or $_.AppxPackagePublisher -or $_.AppxPackageVersion } | Select-Object -First 1
            if (-not $pg) { $pg = $csprojXml.Project.PropertyGroup[0] }
            $pg.AppxPackageIdentityName = $identityName
            $pg.AppxPackagePublisher = $identityPublisher
            $pg.AppxPackageVersion = $version
            Save-XmlNoBom -XmlDoc $csprojXml -Path $csprojPath
        } else {
            # ---------- 字符串替换方式 ----------
            $csprojContent = Get-Content $csprojPath -Raw
            $csprojContent = $csprojContent -replace `
                '<AppxPackageIdentityName>.*?</AppxPackageIdentityName>',
                "<AppxPackageIdentityName>$identityName</AppxPackageIdentityName>"
            $csprojContent = $csprojContent -replace `
                '<AppxPackagePublisher>.*?</AppxPackagePublisher>',
                "<AppxPackagePublisher>$identityPublisher</AppxPackagePublisher>"
            $csprojContent = $csprojContent -replace `
                '<AppxPackageVersion>[\d\.]+</AppxPackageVersion>',
                "<AppxPackageVersion>$version</AppxPackageVersion>"
            $csprojContent | Out-File $csprojPath -NoNewline -Encoding utf8
        }

        Write-Log " .csproj 修改完成（XML）"
    } catch {
        Write-Log "替换 .csproj 失败: $_"
        exit 1
    }
} else {
    Write-Log "警告：未找到 $csprojPath，跳过 .csproj 替换"
}

# ---------- 5.2. 替换 Package.appxmanifest ----------
$manifestPath = Join-Path $scriptDir "Package.appxmanifest"
if (Test-Path $manifestPath) {
    Write-Log "更新 $manifestPath ..."
    try {

        if($writerType -ieq "xml") {
            # ---------- xml 方式 ----------
            [xml]$manifestXml = Get-Content $manifestPath -Raw -Encoding utf8
            $identityNode = $manifestXml.SelectSingleNode("/*/*[local-name()='Identity']")
            if ($identityNode) {
                $identityNode.SetAttribute("Name", $identityName)
                $identityNode.SetAttribute("Publisher", $identityPublisher)
                $identityNode.SetAttribute("Version", $version)
            }
            $pdn = $manifestXml.SelectSingleNode("//PublisherDisplayName")
            if ($pdn) { $pdn.InnerText = $publisherDisplayName }
            Save-XmlNoBom -XmlDoc $manifestXml -Path $manifestPath
        } else {
            # ---------- 字符串替换方式 ----------
            $manifestContent = Get-Content $manifestPath -Raw
            # 替换 Identity 元素的 Name, Publisher, Version
            $manifestContent = $manifestContent -replace `
                '(Identity[^>]+?)Name="[^"]*"',
                "`$1Name=`"$identityName`""
            $manifestContent = $manifestContent -replace `
                '(Identity[^>]+?)Publisher="[^"]*"',
                "`$1Publisher=`"$identityPublisher`""
            $manifestContent = $manifestContent -replace `
                '(Identity[^>]+?)Version="[\d\.]+"',
                "`$1Version=`"$version`""
            if ($manifestContent -match '<PublisherDisplayName>[^<]*</PublisherDisplayName>') {
                $manifestContent = $manifestContent -replace `
                    '<PublisherDisplayName>[^<]*</PublisherDisplayName>',
                    "<PublisherDisplayName>$publisherDisplayName</PublisherDisplayName>"
            }
            $manifestContent | Out-File $manifestPath -NoNewline -Encoding utf8
        }

        Write-Log " .appxmanifest 替换完成"
    } catch {
        Write-Log "替换 .appxmanifest 失败: $_"
        exit 1
    }
} else {
    Write-Log "错误：未找到 $manifestPath"
    exit 1
}


# ---------- 6. 生成 MSIX ----------
Write-Log "本次构建有效版本: $version"
Write-Log "身份信息: Name=$identityName, Publisher=$identityPublisher, PublisherDisplayName=$publisherDisplayName"

Write-Log "开始生成 x64 MSIX..."
$buildX64 = "dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=x64 -p:AppxPackageDir=`"AppPackages\x64\`""
Invoke-WithLogging $buildX64

Write-Log "开始生成 ARM64 MSIX..."
$buildARM64 = "dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=ARM64 -p:AppxPackageDir=`"AppPackages\ARM64\`""
Invoke-WithLogging $buildARM64


# ---------- 7. 查找 MSIX 并合并 ----------
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

## ---------- 7.1. 复制 MSIX 文件 ----------

# 确保 path 目录存在
if (-not (Test-Path $path)) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

Write-Log "将 MSIX 复制到 $path ..."
Copy-Item $x64File.FullName -Destination $path -Force
Copy-Item $arm64File.FullName -Destination $path -Force
Write-Log "复制完成"


## ---------- 7.2. 合并 Bundle 文件 ----------

Write-Log "开始合并 Bundle..."

# 检查 makeappx
if (-not (Get-Command makeappx -ErrorAction SilentlyContinue)) {
    Write-Log "未找到 makeappx， 请安装 Windows SDK 或将 makeappx 加入 PATH"
    exit 1
}

$bundleFile = Join-Path $path "${name}_${version}_Bundle.msixbundle"
$makeappxCmd = "makeappx bundle -d `"$path`" -p `"$bundleFile`""
Invoke-WithLogging $makeappxCmd


# ---------- 8. 签名（可选） ----------
if ($Env -ieq "Prod") {
    Write-Log "检测到 Prod 环境，跳过签名步骤"
} else {
    Write-Log "开始对 Bundle 签名..."

    # 检查 winapp 是否存在
    if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
        Write-Log "未找到 winapp 命令，请确保 Windows App SDK 工具已安装并在 PATH 中"
        exit 1
    }

    $winappCmd = "winapp sign `"$bundleFile`" `"$cert`" --password `"$password`""
    Invoke-WithLogging $winappCmd
}

Write-Log "所有步骤完成。输出目录: $path"
Write-Log "日志文件: $logFile"