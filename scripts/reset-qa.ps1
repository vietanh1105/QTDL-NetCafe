# Mục đích: reset duy nhất database QA netcafe_qa rồi áp dụng schema/migration/seed.
# Không nhận tên database production; không gọi sql/00_create_database.sql vì file đó dành cho demo netcafe_db.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$qaDatabase = if ($env:QA_DB_NAME) { $env:QA_DB_NAME } else { "netcafe_qa" }
if ($qaDatabase -ne "netcafe_qa") {
    throw "QA reset bị chặn: QA_DB_NAME phải đúng netcafe_qa."
}

$pgCandidates = @()
if ($env:PG_BIN) { $pgCandidates += (Join-Path $env:PG_BIN "psql.exe") }
$pgCandidates += Get-ChildItem "C:\Program Files\PostgreSQL" -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName "bin\psql.exe" }
$psql = $pgCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $psql) {
    $command = Get-Command psql -ErrorAction SilentlyContinue
    if ($command) { $psql = $command.Source }
}
if (-not $psql) { throw "Không tìm thấy psql.exe; đặt PG_BIN trước khi chạy QA." }
$hostName = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$port = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$user = if ($env:DB_USER) { $env:DB_USER } else { "postgres" }

function Invoke-QA([string]$sql) {
    & $psql -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d postgres -c $sql
    if ($LASTEXITCODE -ne 0) { throw "psql failed: $sql" }
}

Invoke-QA "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$qaDatabase' AND pid <> pg_backend_pid();"
Invoke-QA "DROP DATABASE IF EXISTS $qaDatabase WITH (FORCE);"
Invoke-QA "CREATE DATABASE $qaDatabase TEMPLATE template0 ENCODING 'UTF8';"

$files = @(
    "sql/01_schema.sql",
    "sql/02_functions_triggers.sql",
    "sql/03_views.sql",
    "sql/04_security.sql",
    "sql/05_indexes_partition.sql",
    "sql/06_seed_base.sql",
    "sql/07_ai_modules.sql",
    "sql/08_station_customer.sql"
)

foreach ($relativePath in $files) {
    $sourcePath = Join-Path (Split-Path $PSScriptRoot -Parent) $relativePath
    $temporaryPath = Join-Path ([System.IO.Path]::GetTempPath()) ("netcafe_qa_" + [System.IO.Path]::GetRandomFileName() + ".sql")
    try {
        (Get-Content -Raw $sourcePath) -replace '(?m)^\\c\s+.*\r?\n', '' | Set-Content -Encoding utf8 $temporaryPath
        & $psql -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $qaDatabase -f $temporaryPath
        if ($LASTEXITCODE -ne 0) { throw "psql failed: $relativePath" }
    }
    finally {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    }
}

& $psql -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $qaDatabase -c @"
CREATE TABLE IF NOT EXISTS core.qa_reset_marker (
    marker_id INTEGER PRIMARY KEY,
    schema_version TEXT NOT NULL,
    reset_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);
INSERT INTO core.qa_reset_marker (marker_id, schema_version)
VALUES (1, '08_station_customer')
ON CONFLICT (marker_id) DO UPDATE SET schema_version = EXCLUDED.schema_version, reset_at = clock_timestamp();
"@
if ($LASTEXITCODE -ne 0) { throw "QA marker failed" }
Write-Host "QA reset complete: $qaDatabase / schema 08_station_customer" -ForegroundColor Green
