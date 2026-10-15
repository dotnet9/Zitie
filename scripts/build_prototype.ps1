param([switch] $Check)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $repositoryRoot 'design/assets'
$encoding = [Text.UTF8Encoding]::new($false)
$read = { param($path) [IO.File]::ReadAllText($path).ReplaceLineEndings("`n") }
$catalog = & $read (Join-Path $assets 'catalog.json')
$parsedCatalog = $catalog | ConvertFrom-Json
$images = [ordered]@{}
$imageNames = [Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in @($parsedCatalog.templates.image | Where-Object { $_ })) { $null = $imageNames.Add($name) }
foreach ($name in $imageNames) {
    $imagePath = Join-Path $repositoryRoot "docs/modules/$name"
    if (-not (Test-Path -LiteralPath $imagePath -PathType Leaf)) { throw "Missing prototype image: $name" }
    $extension = [IO.Path]::GetExtension($name).TrimStart('.').ToLowerInvariant()
    $mediaType = if ($extension -eq 'jpg') { 'jpeg' } else { $extension }
    $images[$name] = "data:image/$mediaType;base64," + [Convert]::ToBase64String([IO.File]::ReadAllBytes($imagePath))
}
foreach ($template in @($parsedCatalog.templates | Where-Object { -not $_.image })) {
    $imagePath = Join-Path $assets "previews/$($template.id).png"
    if (-not (Test-Path -LiteralPath $imagePath -PathType Leaf)) { throw "Missing native preview: $($template.id). Run UI verification with --export-previews." }
    $images[$template.id] = 'data:image/png;base64,' + [Convert]::ToBase64String([IO.File]::ReadAllBytes($imagePath))
}
$translations = [ordered]@{}
foreach ($culture in @('zh-CN', 'en-US')) {
    $translations[$culture] = (& $read (Join-Path $repositoryRoot "src/Zitie.Desktop/I18n/$culture.json")) | ConvertFrom-Json
}
$fontPath = Join-Path $assets 'wenkai-subset.woff2'
if (-not (Test-Path -LiteralPath $fontPath)) { throw 'Run scripts/subset_prototype_font.py before building the prototype.' }
$font = 'data:font/woff2;base64,' + [Convert]::ToBase64String([IO.File]::ReadAllBytes($fontPath))
$style = (& $read (Join-Path $assets 'app.css')).Replace('__FONT__', $font)
$palette = (& $read (Join-Path $assets 'palette-tokens.js')).Replace('window.QUICKAPP_PALETTES', 'window.ZITIE_PALETTES')
$data = "window.ZITIE_CATALOG=$catalog;`nwindow.ZITIE_IMAGES=" + ($images | ConvertTo-Json -Compress -Depth 5) + ";`nwindow.ZITIE_RESOURCES=" + ($translations | ConvertTo-Json -Compress -Depth 50) + ";`n$palette"
$script = & $read (Join-Path $assets 'app.js')
$document = (& $read (Join-Path $PSScriptRoot 'prototype-source/index.template.html')).Replace('/*__STYLE__*/', $style).Replace('/*__DATA__*/', $data.Replace('</script', '<\/script')).Replace('/*__APP__*/', $script)
$output = Join-Path $repositoryRoot 'design/index.html'
if ($Check) {
    if (-not (Test-Path -LiteralPath $output) -or (& $read $output) -cne $document) { throw 'Stale single-file prototype. Run scripts/build_prototype.ps1.' }
} else { [IO.File]::WriteAllText($output, $document, $encoding) }
Write-Host "Zitie 单文件原型已验证：$($parsedCatalog.templates.Count) 个模板，$($parsedCatalog.contents.Count) 条内容。"
