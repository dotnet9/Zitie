param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("win-x64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string] $RuntimeIdentifier
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "src\Zitie.Desktop\Zitie.Desktop.csproj"
$moduleSourcePath = Join-Path $repositoryRoot "resources\modules"
$modulePackScript = Join-Path $PSScriptRoot "pack_modules.ps1"
$publishRoot = Join-Path $repositoryRoot "artifacts\publish"
$outputPath = Join-Path $publishRoot "$RuntimeIdentifier\Zitie.Desktop"
$targetFramework = if ($RuntimeIdentifier -like "win-*") { "net10.0-windows" } else { "net10.0" }
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

if ($RuntimeIdentifier -like "win-*") {
    # Windows：NativeAOT 单文件。反射保留由 Roots.xml（TrimmerRootDescriptor）声明。
    # StripSymbols 去除符号；IlcSingleThreaded 减少内存占用。VC-LTL / YY-Thunks 兼容旧系统。
    $publishArguments += @(
        "-p:PublishAot=true",
        "-p:PublishTrimmed=true",
        "-p:StripSymbols=true",
        "-p:IlcSingleThreaded=true",
        "-p:TreatWarningsAsErrors=false",
        "-p:ILLinkTreatWarningsAsErrors=false"
    )
}
else {
    # macOS / Linux：同样走 NativeAOT（完整反射元数据保全 Prism / CodeWF.Log，配方与 win-x64 一致）
    $publishArguments += @(
        "-p:PublishAot=true",
        "-p:PublishTrimmed=true",
        "-p:PublishSingleFile=false",
        "-p:IlcGenerateCompleteTypeMetadata=true",
        "-p:IlcTrimMetadata=false",
        "-p:IlcSingleThreaded=true",
        "-p:StripSymbols=false",
        "-p:TreatWarningsAsErrors=false",
        "-p:ILLinkTreatWarningsAsErrors=false"
    )
}

Write-Host "Publishing Zitie.Desktop for $RuntimeIdentifier..."
& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "Zitie.Desktop publish failed for $RuntimeIdentifier with exit code $LASTEXITCODE."
}

& $modulePackScript `
    -SourceDirectory $moduleSourcePath `
    -OutputDirectory (Join-Path $resolvedOutputPath "resources\modules")

Get-ChildItem -LiteralPath $resolvedOutputPath -Recurse -File -Filter "*.pdb" -ErrorAction SilentlyContinue |
    Remove-Item -Force

Write-Host "Published Zitie.Desktop to $resolvedOutputPath"
