param([switch] $Check)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'T4 tool restore failed.' }
    $template = Join-Path $repositoryRoot 'src/Zitie.Desktop/I18n/Language.tt'
    $output = Join-Path $repositoryRoot 'src/Zitie.Desktop/I18n/Language.cs'
    $generatedOutput = Join-Path ([IO.Path]::GetTempPath()) ('zitie-t4-' + [Guid]::NewGuid().ToString('N') + '.cs')
    try {
        dotnet tool run t4 -- $template -o $generatedOutput
        if ($LASTEXITCODE -ne 0) { throw 'T4 transformation failed.' }
        $generated = [IO.File]::ReadAllText($generatedOutput).ReplaceLineEndings("`n")
        if ($Check) {
            if ([IO.File]::ReadAllText($output).ReplaceLineEndings("`n") -cne $generated) { throw 'Stale T4 keys. Run scripts/sync_localization_keys.ps1.' }
        } else { [IO.File]::WriteAllText($output, $generated, [Text.UTF8Encoding]::new($false)) }
    } finally { if (Test-Path -LiteralPath $generatedOutput) { Remove-Item -LiteralPath $generatedOutput -Force } }

    Write-Host 'Zitie T4 已验证。'
} finally { Pop-Location }
