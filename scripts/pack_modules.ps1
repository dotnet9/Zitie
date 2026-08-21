param(
    [Parameter(Mandatory = $true)]
    [string] $SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$resolvedSource = (Resolve-Path -LiteralPath $SourceDirectory).Path
$resolvedOutput = [IO.Path]::GetFullPath($OutputDirectory)

if ($resolvedOutput.Equals($resolvedSource, [StringComparison]::OrdinalIgnoreCase) -or
    $resolvedOutput.StartsWith($resolvedSource + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to write template packages into the template source: $resolvedOutput"
}

New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

if (Get-ChildItem -LiteralPath $resolvedOutput -Directory -ErrorAction SilentlyContinue) {
    throw "Published template directory must not contain unpacked template folders: $resolvedOutput"
}

Get-ChildItem -LiteralPath $resolvedOutput -File -Filter "*.zi" -ErrorAction SilentlyContinue |
    Remove-Item -Force

$templateDirectories = @(Get-ChildItem -LiteralPath $resolvedSource -Directory | Sort-Object Name)
foreach ($templateDirectory in $templateDirectories) {
    $modulePath = Join-Path $templateDirectory.FullName "module.yml"
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
        throw "Template source is missing module.yml: $($templateDirectory.FullName)"
    }

    $packagePath = Join-Path $resolvedOutput "$($templateDirectory.Name).zi"
    $archive = [IO.Compression.ZipFile]::Open($packagePath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $sourcePrefix = $templateDirectory.FullName.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        foreach ($file in Get-ChildItem -LiteralPath $templateDirectory.FullName -File -Recurse | Sort-Object FullName) {
            $entryName = $file.FullName.Substring($sourcePrefix.Length).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $file.FullName,
                $entryName,
                [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }
}

Write-Host "Packed $($templateDirectories.Count) template modules to $resolvedOutput"
