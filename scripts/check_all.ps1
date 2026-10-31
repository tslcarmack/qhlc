$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

dotnet restore qhlc.sln | Out-Null

$env:QHLC_ACTIVE_CHG = if ($env:QHLC_ACTIVE_CHG) { $env:QHLC_ACTIVE_CHG } else { "CHG-0001" }

Write-Host "== dotnet format =="
dotnet format --verify-no-changes qhlc.sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== governance check all (CHG=$($env:QHLC_ACTIVE_CHG)) =="
dotnet run --project tools/Qhlc.Governance -- check all
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== dotnet test =="
dotnet test qhlc.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "check_all 全部通过。"
