param(
    [ValidateSet("all", "net40", "net8.0-windows")]
    [string]$Framework = "all",

    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",

    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish"
$releaseRoot = Join-Path $artifactsRoot "releases"
$projectPath = Join-Path $repoRoot "kursach.csproj"

function Initialize-CleanDirectory([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Path is empty."
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $allowedRoot = [System.IO.Path]::GetFullPath($artifactsRoot)
    if (-not $fullPath.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean path outside artifacts: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath | Out-Null
}

function Copy-PublishedLayout([string]$RawPath, [string]$PackagePath) {
    $appPath = Join-Path $PackagePath "app"
    $contentPath = Join-Path $PackagePath "content"
    $logsPath = Join-Path $PackagePath "logs"

    New-Item -ItemType Directory -Path $appPath, $contentPath, $logsPath | Out-Null

    Get-ChildItem -LiteralPath $RawPath -Force | Where-Object { $_.Name -ne "content" } | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $appPath -Recurse -Force
    }

    $sourceContent = Join-Path $repoRoot "src"
    if (-not (Test-Path -LiteralPath (Join-Path $sourceContent "content.v2.json"))) {
        throw "Source content file is missing: $sourceContent"
    }

    Get-ChildItem -LiteralPath $sourceContent -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $contentPath -Recurse -Force
    }
}

function Publish-Net40 {
    $packagePath = Join-Path $releaseRoot "selfinstruction-net40"

    Initialize-CleanDirectory $packagePath

    dotnet build $projectPath -f net40 -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $rawPath = Join-Path $repoRoot "bin\$Configuration\net40"
    if (-not (Test-Path -LiteralPath $rawPath)) {
        throw "Build output directory is missing: $rawPath"
    }

    Copy-PublishedLayout $rawPath $packagePath
}

function Publish-Net8 {
    $rawPath = Join-Path $publishRoot "net8-$Runtime"
    $packagePath = Join-Path $releaseRoot "selfinstruction-net8-$Runtime"

    Initialize-CleanDirectory $rawPath
    Initialize-CleanDirectory $packagePath

    dotnet publish $projectPath -f net8.0-windows -c $Configuration -r $Runtime --self-contained true -o $rawPath -p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Copy-PublishedLayout $rawPath $packagePath
}

function Compress-Releases {
    if ($SkipZip) {
        return
    }

    Get-ChildItem -LiteralPath $releaseRoot -Directory | ForEach-Object {
        $zipPath = "$($_.FullName).zip"
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }

        Compress-Archive -Path (Join-Path $_.FullName "*") -DestinationPath $zipPath -Force
    }
}

New-Item -ItemType Directory -Path $artifactsRoot, $publishRoot, $releaseRoot -Force | Out-Null

if ($Framework -eq "all" -or $Framework -eq "net40") {
    Publish-Net40
}

if ($Framework -eq "all" -or $Framework -eq "net8.0-windows") {
    Publish-Net8
}

Compress-Releases
