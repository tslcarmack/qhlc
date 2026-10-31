param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,

    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$sourceRoot = Split-Path -Parent $PSScriptRoot
$TargetPath = (Resolve-Path $TargetPath).Path

$copyPaths = @(
    "governance",
    "tools/Qhlc.Governance",
    "scripts/check_all.ps1",
    "scripts/install-manifest.txt"
)

$mergePaths = @(
    ".cursor/rules",
    ".cursor/skills"
)

function Copy-Tree($relative) {
    $from = Join-Path $sourceRoot $relative
    $to = Join-Path $TargetPath $relative
    if (-not (Test-Path $from)) { return }
    if ($WhatIf) {
        Write-Host "[WhatIf] Copy $from -> $to"
        return
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $to) | Out-Null
    Copy-Item -Path $from -Destination $to -Recurse -Force
    Write-Host "Copied $relative"
}

foreach ($path in $copyPaths) {
    Copy-Tree $path
}

foreach ($path in $mergePaths) {
    $from = Join-Path $sourceRoot $path
    $to = Join-Path $TargetPath $path
    if (-not (Test-Path $from)) { continue }
    if (Test-Path $to) {
        $bak = Join-Path $TargetPath ".cursor/rules.bak/$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        if ($WhatIf) {
            Write-Host "[WhatIf] Backup existing $to to $bak"
        } else {
            Write-Host "目标已存在 $path，请人工合并。参考源：$from"
        }
    } else {
        Copy-Tree $path
    }
}

$ciSource = Join-Path $sourceRoot ".github/workflows/ci.yml"
$ciTarget = Join-Path $TargetPath ".github/workflows/ci.yml"
if (Test-Path $ciTarget) {
    Write-Host "目标已有 ci.yml，请人工对比：$ciSource"
} elseif (Test-Path $ciSource) {
    Copy-Tree ".github/workflows/ci.yml"
}

Write-Host "完成。请根据目标项目调整 governance/registry/modules.yaml 中的程序集路径。"
