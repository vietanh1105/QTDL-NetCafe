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
if ([string]::IsNullOrWhiteSpace($env:DB_PASSWORD)) {
    throw "QA reset bị dừng trước khi kết nối: cần đặt DB_PASSWORD cho PostgreSQL local."
}
$env:PGPASSWORD = $env:DB_PASSWORD

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

$serviceEmployeeId = if ($env:STATION_SERVICE_EMPLOYEE_ID) { $env:STATION_SERVICE_EMPLOYEE_ID } else {
    $query = & $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $qaDatabase -Atc "SELECT employee_id FROM hr.employee WHERE is_active = TRUE ORDER BY employee_id LIMIT 1;"
    if ($LASTEXITCODE -ne 0) { throw "Không lấy được nhân viên active cho station." }
    $query.Trim()
}
if ($serviceEmployeeId -notmatch '^[1-9][0-9]*$') { throw "STATION_SERVICE_EMPLOYEE_ID phải là ID nhân viên active." }
$stationRole = if ($env:STATION_DB_USER) { $env:STATION_DB_USER } else { "station_runtime" }
if ($stationRole -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw "STATION_DB_USER có ký tự không hợp lệ." }
$stationPassword = if ($env:STATION_DB_PASSWORD) { $env:STATION_DB_PASSWORD.Replace("'", "''") } else { $null }
$quotedStationRole = '"' + $stationRole.Replace('"', '""') + '"'
$configSql = @"
DO `$`$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM hr.employee WHERE employee_id = $serviceEmployeeId AND is_active = TRUE) THEN
        RAISE EXCEPTION 'STATION_SERVICE_EMPLOYEE_ID không trỏ nhân viên active.' USING ERRCODE = '23514';
    END IF;
END `$`$;
INSERT INTO core.station_runtime_config(config_key, config_value, updated_at)
VALUES ('service_employee_id', '$serviceEmployeeId', clock_timestamp())
ON CONFLICT (config_key) DO UPDATE SET config_value = EXCLUDED.config_value, updated_at = EXCLUDED.updated_at;
DO `$`$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '$stationRole') THEN
        CREATE ROLE $quotedStationRole LOGIN;
    END IF;
END `$`$;
GRANT role_station TO $quotedStationRole;
"@
if ($stationPassword) { $configSql += "`nALTER ROLE $quotedStationRole WITH LOGIN PASSWORD '$stationPassword';" }
& $psql -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $qaDatabase -c $configSql
if ($LASTEXITCODE -ne 0) { throw "Cấu hình station QA thất bại." }

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
