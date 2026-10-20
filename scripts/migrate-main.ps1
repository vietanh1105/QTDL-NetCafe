# Trách nhiệm: backup, preflight và áp dụng migration 08 cùng contract Admin cho netcafe_db.
# Không reset, seed, ETL overwrite hoặc chạy sql/00_create_database.sql trên database chính.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

function Read-EnvFile([string]$path) {
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
        $parts = $trimmed.Split('=', 2)
        if ($parts.Count -eq 2) {
            $values[$parts[0].Trim()] = $parts[1].Trim().Trim('"').Trim("'")
        }
    }
    return $values
}

$envPath = if ($env:NETCAFE_ENV_FILE) { $env:NETCAFE_ENV_FILE } else { Join-Path $repoRoot '.env' }
if (-not (Test-Path -LiteralPath $envPath)) { throw "Không tìm thấy .env: $envPath" }
$config = Read-EnvFile $envPath
foreach ($key in @('DB_HOST', 'DB_PORT', 'DB_NAME', 'DB_USER', 'DB_PASSWORD', 'DB_SSL_MODE', 'STATION_SERVICE_EMPLOYEE_ID', 'STATION_DB_USER', 'STATION_DB_PASSWORD')) {
    $environmentValue = [Environment]::GetEnvironmentVariable($key)
    if ($environmentValue) { $config[$key] = $environmentValue }
}

$dbName = $config['DB_NAME']
if ($dbName -ne 'netcafe_db') { throw "Migration chính bị chặn: DB_NAME phải là netcafe_db, hiện là $dbName." }
if (-not $config['DB_PASSWORD']) { throw "Thiếu DB_PASSWORD; không ghi credential vào source hoặc log." }
if (-not $config['STATION_SERVICE_EMPLOYEE_ID']) { throw "Thiếu STATION_SERVICE_EMPLOYEE_ID; phải trỏ nhân viên active hiện có." }
$serviceEmployeeId = 0
if (-not [int]::TryParse($config['STATION_SERVICE_EMPLOYEE_ID'], [ref]$serviceEmployeeId) -or $serviceEmployeeId -le 0) {
    throw "STATION_SERVICE_EMPLOYEE_ID phải là số nguyên dương."
}
$stationRole = if ($config['STATION_DB_USER']) { $config['STATION_DB_USER'] } else { 'station_runtime' }
if ($stationRole -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw "STATION_DB_USER có ký tự không hợp lệ." }
if (-not $config['STATION_DB_PASSWORD']) { throw "Thiếu STATION_DB_PASSWORD; station không được dùng DB owner." }

$pgBin = if ($env:PG_BIN) { Join-Path $env:PG_BIN 'psql.exe' } else { $null }
$pgDump = if ($env:PG_BIN) { Join-Path $env:PG_BIN 'pg_dump.exe' } else { $null }
if (-not $pgBin) {
    $pgBin = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'bin\psql.exe' } |
        Where-Object { Test-Path $_ } | Select-Object -First 1
    $pgDump = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'bin\pg_dump.exe' } |
        Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not (Test-Path $pgBin) -or -not (Test-Path $pgDump)) { throw 'psql.exe and pg_dump.exe are required before migration.' }

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupDir = Join-Path $repoRoot "backup\main-migration-$timestamp"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$env:PGPASSWORD = $config['DB_PASSWORD']
$env:PGSSLMODE = if ($config['DB_SSL_MODE']) { $config['DB_SSL_MODE'].ToLowerInvariant() } else { 'require' }
$hostName = $config['DB_HOST']
$port = $config['DB_PORT']
$user = $config['DB_USER']

$dumpPath = Join-Path $backupDir 'netcafe_db_before_08.dump'
& $pgDump -h $hostName -p $port -U $user -d $dbName --format=custom --file=$dumpPath
if ($LASTEXITCODE -ne 0) { throw 'pg_dump backup failed; migration was not started.' }

$countSql = @"
SELECT 'computer', count(*) FROM core.computer
UNION ALL SELECT 'customer', count(*) FROM core.customer
UNION ALL SELECT 'product', count(*) FROM inventory.product
UNION ALL SELECT 'invoice', count(*) FROM billing.invoice
UNION ALL SELECT 'gaming_session', count(*) FROM core.gaming_session;
"@
& $pgBin -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -Atc $countSql | Set-Content -LiteralPath (Join-Path $backupDir 'row-count-before.txt')
if ($LASTEXITCODE -ne 0) { throw 'Preflight row-count failed; migration was not started.' }

$migrationPaths = @(
    (Join-Path $repoRoot 'sql\08_station_customer.sql'),
    (Join-Path $repoRoot 'sql\10_admin_operations.sql')
)
foreach ($migrationPath in $migrationPaths) {
    $migrationText = Get-Content -Raw -LiteralPath $migrationPath
    foreach ($forbiddenToken in @('DROP DATABASE', 'DROP SCHEMA', 'TRUNCATE', 'DELETE FROM')) {
        if ($migrationText -match [regex]::Escape($forbiddenToken)) {
            throw "Migration bị chặn vì chứa thao tác phá hủy: $forbiddenToken ($migrationPath)"
        }
    }
    & $pgBin -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -f $migrationPath
    if ($LASTEXITCODE -ne 0) { throw "Migration thất bại: $migrationPath; không chạy reset/seed." }
}

$quotedRole = '"' + $stationRole.Replace('"', '""') + '"'
$escapedStationPassword = $config['STATION_DB_PASSWORD'].Replace("'", "''")
$configureSql = @'
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM hr.employee WHERE employee_id = __SERVICE_ID__ AND is_active = TRUE) THEN
        RAISE EXCEPTION 'STATION_SERVICE_EMPLOYEE_ID is not an active employee.' USING ERRCODE = '23514';
    END IF;
END $$;
INSERT INTO core.station_runtime_config(config_key, config_value, updated_at)
VALUES ('service_employee_id', '__SERVICE_ID__', clock_timestamp())
ON CONFLICT (config_key) DO UPDATE SET config_value = EXCLUDED.config_value, updated_at = EXCLUDED.updated_at;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '__STATION_ROLE__') THEN
        CREATE ROLE __QUOTED_ROLE__ LOGIN;
    END IF;
END $$;
GRANT role_station TO __QUOTED_ROLE__;
ALTER ROLE __QUOTED_ROLE__ WITH LOGIN PASSWORD '__STATION_PASSWORD__';
'@
$configureSql = $configureSql.Replace('__SERVICE_ID__', $serviceEmployeeId.ToString())
$configureSql = $configureSql.Replace('__STATION_ROLE__', $stationRole)
$configureSql = $configureSql.Replace('__QUOTED_ROLE__', $quotedRole)
$configureSql = $configureSql.Replace('__STATION_PASSWORD__', $escapedStationPassword)
& $pgBin -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -c $configureSql
if ($LASTEXITCODE -ne 0) { throw 'Service employee/station role configuration failed.' }

$verifySql = @'
SELECT current_database(), current_user;
DO $$
DECLARE
    v_missing_columns TEXT;
BEGIN
    IF current_database() <> 'netcafe_db' THEN
        RAISE EXCEPTION 'Verification is running against the wrong database.';
    END IF;
    IF to_regclass('reporting.vw_computer_grid') IS NULL THEN
        RAISE EXCEPTION 'Missing reporting.vw_computer_grid after migration.';
    END IF;
    SELECT string_agg(required_column, ', ' ORDER BY required_column)
    INTO v_missing_columns
    FROM (VALUES
        ('current_play_amount'),
        ('current_service_amount'),
        ('current_balance'),
        ('last_heartbeat_at')
    ) AS required(required_column)
    WHERE NOT EXISTS (
        SELECT 1
        FROM information_schema.columns c
        WHERE c.table_schema = 'reporting'
          AND c.table_name = 'vw_computer_grid'
          AND c.column_name = required.required_column
    );
    IF v_missing_columns IS NOT NULL THEN
        RAISE EXCEPTION 'View is missing columns: %', v_missing_columns;
    END IF;
    IF to_regprocedure('inventory.fn_customer_menu_v2()') IS NULL
       OR to_regprocedure('core.sp_customer_login(character varying,text,character varying)') IS NULL
       OR to_regprocedure('core.sp_station_heartbeat(bigint,integer,integer)') IS NULL
       OR to_regprocedure('core.sp_station_logout(bigint,integer,integer)') IS NULL
       OR to_regprocedure('core.sp_reconcile_stale_sessions(integer)') IS NULL
       OR to_regprocedure('inventory.sp_receive_stock(integer,integer)') IS NULL
       OR to_regprocedure('inventory.sp_update_order_status(bigint,character varying,integer)') IS NULL
       OR to_regprocedure('core.sp_update_service_request_status(bigint,character varying,integer)') IS NULL
       OR to_regprocedure('billing.sp_void_invoice(bigint,integer)') IS NULL
       OR to_regprocedure('reporting.sp_refresh_materialized_views()') IS NULL
       OR to_regprocedure('hr.sp_create_maintenance(integer,integer,text)') IS NULL THEN
        RAISE EXCEPTION 'Station contract function is missing after migration.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'role_station') THEN
        RAISE EXCEPTION 'role_station is missing after migration.';
    END IF;
    IF NOT EXISTS (
        SELECT 1
        FROM pg_auth_members m
        JOIN pg_roles member_role ON member_role.oid = m.member
        JOIN pg_roles parent_role ON parent_role.oid = m.roleid
        WHERE member_role.rolname = '__STATION_ROLE__'
          AND parent_role.rolname = 'role_station'
    ) THEN
        RAISE EXCEPTION 'Station runtime role has not been granted role_station.';
    END IF;
END $$;
SELECT to_regclass('core.service_request'),
       to_regprocedure('inventory.fn_customer_menu_v2()'),
       to_regprocedure('core.sp_customer_login(character varying,text,character varying)'),
       to_regprocedure('core.sp_station_heartbeat(bigint,integer,integer)'),
       to_regprocedure('core.sp_station_logout(bigint,integer,integer)'),
       to_regprocedure('core.sp_reconcile_stale_sessions(integer)');
SELECT column_name FROM information_schema.columns WHERE table_schema = 'core' AND table_name = 'gaming_session' AND column_name IN ('last_heartbeat_at', 'close_reason') ORDER BY column_name;
SELECT column_name FROM information_schema.columns WHERE table_schema = 'reporting' AND table_name = 'vw_computer_grid' AND column_name IN ('current_play_amount', 'current_service_amount', 'current_balance', 'last_heartbeat_at') ORDER BY column_name;
SELECT rolname FROM pg_roles WHERE rolname IN ('role_station', '__STATION_ROLE__') ORDER BY rolname;
'@
$verifySql = $verifySql.Replace('__STATION_ROLE__', $stationRole)
& $pgBin -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -Atc $verifySql | Set-Content -LiteralPath (Join-Path $backupDir 'verification-after.txt')
if ($LASTEXITCODE -ne 0) { throw 'Verification sau migration thất bại.' }

& $pgBin -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -Atc $countSql | Set-Content -LiteralPath (Join-Path $backupDir 'row-count-after.txt')
if ($LASTEXITCODE -ne 0) { throw 'Row-count sau migration thất bại.' }
Write-Host "Station and Admin migrations completed on $dbName. Backup and evidence: $backupDir" -ForegroundColor Green
