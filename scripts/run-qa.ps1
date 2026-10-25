# Mục đích: chạy build/test với database QA duy nhất, không fallback sang Neon.
# Liên kết: reset-qa.ps1, NetCafe.slnx và tests/*.sql.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"
$env:DB_NAME = "netcafe_qa"
$env:DB_HOST = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$env:DB_PORT = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$env:DB_USER = if ($env:DB_OWNER_USER) { $env:DB_OWNER_USER } elseif ($env:DB_USER) { $env:DB_USER } else { "postgres" }
if ($env:DB_OWNER_PASSWORD) { $env:DB_PASSWORD = $env:DB_OWNER_PASSWORD }
if (-not $env:DB_PASSWORD) { $env:DB_PASSWORD = "" }
if ([string]::IsNullOrWhiteSpace($env:DB_PASSWORD)) {
    throw "QA bị dừng trước khi kết nối: cần đặt DB_OWNER_PASSWORD cho PostgreSQL local để tránh treo chờ nhập."
}
$env:PGPASSWORD = $env:DB_PASSWORD

& (Join-Path $PSScriptRoot "reset-qa.ps1")
dotnet build (Join-Path (Split-Path $PSScriptRoot -Parent) "app/NetCafe.slnx") --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$pgCandidates = @()
if ($env:PG_BIN) { $pgCandidates += (Join-Path $env:PG_BIN "psql.exe") }
$pgCandidates += Get-ChildItem "C:\Program Files\PostgreSQL" -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName "bin\psql.exe" }
$psql = $pgCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $psql) { throw "Không tìm thấy psql.exe để chạy tests/station_customer.sql." }
$testSql = Join-Path (Split-Path $PSScriptRoot -Parent) "tests\station_customer.sql"
& $psql -v ON_ERROR_STOP=1 -h $env:DB_HOST -p $env:DB_PORT -U $env:DB_USER -d $env:DB_NAME -f $testSql
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$prepareStationSql = Join-Path (Split-Path $PSScriptRoot -Parent) "tests\verify_qa_station_prepare.sql"
$stationSql = Join-Path (Split-Path $PSScriptRoot -Parent) "tests\verify_qa_station.sql"
$assertStationSql = Join-Path (Split-Path $PSScriptRoot -Parent) "tests\verify_qa_station_assert.sql"
if (-not $env:STATION_DB_USER -or -not $env:STATION_DB_PASSWORD) {
    throw "QA station bị dừng: cần STATION_DB_USER và STATION_DB_PASSWORD."
}
& $psql -v ON_ERROR_STOP=1 -h $env:DB_HOST -p $env:DB_PORT -U $env:DB_USER -d $env:DB_NAME -f $prepareStationSql
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:PGPASSWORD = $env:STATION_DB_PASSWORD
& $psql -v ON_ERROR_STOP=1 -h $env:DB_HOST -p $env:DB_PORT -U $env:STATION_DB_USER -d $env:DB_NAME -f $stationSql
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:PGPASSWORD = $env:DB_PASSWORD
& $psql -v ON_ERROR_STOP=1 -h $env:DB_HOST -p $env:DB_PORT -U $env:DB_USER -d $env:DB_NAME -f $assertStationSql
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$aiSql = Join-Path (Split-Path $PSScriptRoot -Parent) "tests\verify_qa_ai.sql"
& $psql -v ON_ERROR_STOP=1 -h $env:DB_HOST -p $env:DB_PORT -U $env:DB_USER -d $env:DB_NAME -f $aiSql
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet test (Join-Path (Split-Path $PSScriptRoot -Parent) "app/NetCafe.Data.Tests/NetCafe.Data.Tests.csproj") --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "QA build and xUnit tests passed." -ForegroundColor Green
