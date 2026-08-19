param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("win-x64", "linux-x64", "linux-arm64")]
    [string] $RuntimeIdentifier
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "src\Zitie.Desktop\Zitie.Desktop.csproj"
$publishRoot = Join-Path $repositoryRoot "artifacts\publish"
$outputPath = Join-Path $publishRoot "$RuntimeIdentifier\Zitie.Desktop"
$targetFramework = "net10.0"
$resolvedPublishRoot = [IO.Path]::GetFullPath($publishRoot)
$resolvedOutputPath = [IO.Path]::GetFullPath($outputPath)

if (-not $resolvedOutputPath.StartsWith($resolvedPublishRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to replace unexpected publish path: $resolvedOutputPath"
}

if (Test-Path -LiteralPath $resolvedOutputPath) {
    Remove-Item -LiteralPath $resolvedOutputPath -Recurse -Force
}

Remove-Item Env:xskj-lib -ErrorAction SilentlyContinue

$publishArguments = @(
    "publish",
    $projectPath,
    "-c", "Release",
    "-f", $targetFramework,
    "-r", $RuntimeIdentifier,
    "--self-contained", "true",
    "-p:RestoreForce=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-o", $resolvedOutputPath
)

if ($RuntimeIdentifier -eq "win-x64") {
    # Zitie 依赖 Prism / CodeWF.Log（反射）与 SkiaSharp，NativeAOT 兼容性风险高，
    # 故 Windows 也采用单文件 + 不裁剪，保证打包后开箱即用。
    $publishArguments += @(
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false"
    )
}
else {
    $publishArguments += @(
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false"
    )
}

Write-Host "Publishing Zitie.Desktop for $RuntimeIdentifier..."
& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "Zitie.Desktop publish failed for $RuntimeIdentifier with exit code $LASTEXITCODE."
}

Write-Host "Published Zitie.Desktop to $resolvedOutputPath"
