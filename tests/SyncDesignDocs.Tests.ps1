$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot 'sync-design-docs.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('design-sync-test-' + [System.Guid]::NewGuid().ToString('N'))
$source = Join-Path $tempRoot 'source'
$target = Join-Path $tempRoot 'target'
$report = Join-Path $tempRoot 'latest-sync-report.md'

New-Item -ItemType Directory -Path $source, $target | Out-Null
New-Item -ItemType Directory -Path (Join-Path $source '归档'), (Join-Path $target '归档') | Out-Null
Set-Content -LiteralPath (Join-Path $source '新增.md') -Value 'new from source' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $source '变更.md') -Value 'updated from source' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $source '保留.md') -Value 'same content' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $source '归档\子文档.md') -Value 'nested source' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '变更.md') -Value 'old target' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '保留.md') -Value 'same content' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '保留.md.meta') -Value 'keep this meta' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '归档.meta') -Value 'keep folder meta' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '删除.md') -Value 'remove me' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $target '删除.md.meta') -Value 'remove orphan meta' -Encoding UTF8

try {
    $scriptOutput = & pwsh -NoProfile -ExecutionPolicy Bypass -File $scriptPath -SourcePath $source -TargetPath $target -ReportPath $report 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Sync script failed with exit code $LASTEXITCODE. Output: $scriptOutput" }

    if (-not (Test-Path -LiteralPath (Join-Path $target '新增.md'))) { throw 'Expected added file to be copied.' }
    if ((Get-Content -LiteralPath (Join-Path $target '变更.md') -Raw).Trim() -ne 'updated from source') { throw 'Expected changed file to be updated.' }
    if (Test-Path -LiteralPath (Join-Path $target '删除.md')) { throw 'Expected removed target file to be deleted.' }
    if (Test-Path -LiteralPath (Join-Path $target '删除.md.meta')) { throw 'Expected deleted target file meta to be deleted.' }
    if (-not (Test-Path -LiteralPath (Join-Path $target '保留.md.meta'))) { throw 'Expected retained target file meta to be preserved.' }
    if (-not (Test-Path -LiteralPath (Join-Path $target '归档.meta'))) { throw 'Expected retained target folder meta to be preserved.' }
    if (-not (Test-Path -LiteralPath (Join-Path $target '归档\子文档.md'))) { throw 'Expected nested file to be copied.' }
    if (-not (Test-Path -LiteralPath $report)) { throw 'Expected sync report to be written.' }
    if (-not ((Get-Content -LiteralPath $report -Raw) -match 'Added')) { throw 'Expected report to include Added section.' }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
