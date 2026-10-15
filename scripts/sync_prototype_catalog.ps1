param([string] $CatalogPath = 'artifacts/ui-verification/catalog.json', [switch] $Check)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$source = if ([IO.Path]::IsPathRooted($CatalogPath)) { $CatalogPath } else { Join-Path $repositoryRoot $CatalogPath }
$target = Join-Path $repositoryRoot 'design/assets/catalog.json'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Run scripts/ui-verification before synchronizing the prototype catalog.' }
$document = [IO.File]::ReadAllText($source).ReplaceLineEndings("`n")
if ($Check) {
    if ([IO.File]::ReadAllText($target).ReplaceLineEndings("`n") -cne $document) { throw 'Stale prototype catalog. Run scripts/sync_prototype_catalog.ps1.' }
} else { [IO.File]::WriteAllText($target, $document, [Text.UTF8Encoding]::new($false)) }
Write-Host '实际应用与原型目录一致。'
