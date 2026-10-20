# Trách nhiệm: backup, preflight và loại bỏ core.booking an toàn trên QA hoặc main.
# Liên kết: sql/09_remove_booking.sql; không reset, seed, truncate hay cascade database.

[CmdletBinding()]
param(
    [ValidateSet('qa', 'main')]
    [string]$Target = 'qa'
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

function Read-EnvFile([string]$path) {
    $values = @{}
    if (-not (Test-Path -LiteralPath $path)) { return $values }
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

function Get-PgTool([string]$name) {
    $candidates = @()
    if ($env:PG_BIN) { $candidates += Join-Path $env:PG_BIN "$name.exe" }
    $candidates += Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "bin\$name.exe" }
    $command = Get-Command "$name.exe" -ErrorAction SilentlyContinue
    if ($command) { $candidates += $command.Source }
    return $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

$fileConfig = Read-EnvFile (Join-Path $repoRoot '.env')
$hostName = if ($env:DB_HOST) { $env:DB_HOST } else { $fileConfig['DB_HOST'] }
$port = if ($env:DB_PORT) { $env:DB_PORT } else { $fileConfig['DB_PORT'] }
$user = if ($env:DB_USER) { $env:DB_USER } else { $fileConfig['DB_USER'] }
$password = if ($env:DB_PASSWORD) { $env:DB_PASSWORD } else { $fileConfig['DB_PASSWORD'] }
$dbName = if ($Target -eq 'main') { $fileConfig['DB_NAME'] } else { 'netcafe_qa' }

if ($Target -eq 'main' -and $dbName -ne 'netcafe_db') {
    throw "Migration main bị chặn: DB_NAME phải là netcafe_db, hiện là $dbName."
}
if ($Target -eq 'qa' -and $dbName -ne 'netcafe_qa') {
    throw "Migration QA bị chặn: database phải là netcafe_qa."
}
if ([string]::IsNullOrWhiteSpace($hostName) -or [string]::IsNullOrWhiteSpace($user) -or [string]::IsNullOrWhiteSpace($password)) {
    throw "Thiếu DB_HOST, DB_USER hoặc DB_PASSWORD cho mục tiêu $Target; không ghi credential vào source/log."
}

$psql = Get-PgTool 'psql'
$pgDump = Get-PgTool 'pg_dump'
if (-not $psql -or -not $pgDump) { throw 'Cần psql.exe và pg_dump.exe trước khi migration.' }

$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupDir = Join-Path $repoRoot "backup\booking-removal-$Target-$timestamp"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$env:PGPASSWORD = $password
$env:PGSSLMODE = if ($fileConfig['DB_SSL_MODE']) { $fileConfig['DB_SSL_MODE'].ToLowerInvariant() } else { 'disable' }

$dumpPath = Join-Path $backupDir "${dbName}_before_booking_removal.dump"
& $pgDump -h $hostName -p $port -U $user -d $dbName --format=custom --file=$dumpPath
if ($LASTEXITCODE -ne 0) { throw 'pg_dump backup thất bại; migration chưa bắt đầu.' }

$rowCountSql = @"
SELECT 'customer', count(*) FROM core.customer
UNION ALL SELECT 'computer', count(*) FROM core.computer
UNION ALL SELECT 'gaming_session', count(*) FROM core.gaming_session
UNION ALL SELECT 'invoice', count(*) FROM billing.invoice
UNION ALL SELECT 'payment', count(*) FROM billing.payment
UNION ALL SELECT 'food_order', count(*) FROM inventory.food_order
UNION ALL SELECT 'employee', count(*) FROM hr.employee
UNION ALL SELECT 'product', count(*) FROM inventory.product
ORDER BY 1;
"@
$beforeCounts = ((& $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -Atc $rowCountSql) -join "`n").Trim()
if ($LASTEXITCODE -ne 0) { throw 'Không đọc được row count trước migration.' }
$beforeCounts | Set-Content -LiteralPath (Join-Path $backupDir 'row-count-before.txt') -Encoding utf8

$dependencySql = @"
DO `$`$
DECLARE
    v_booking REGCLASS;
    v_dependency TEXT;
BEGIN
    v_booking := to_regclass('core.booking');
    IF v_booking IS NULL THEN
        RAISE NOTICE 'core.booking không tồn tại trước migration.';
        RETURN;
    END IF;
    SELECT pg_describe_object(d.classid, d.objid, d.objsubid)
    INTO v_dependency
    FROM pg_depend AS d
    WHERE d.refobjid = v_booking
      AND d.deptype = 'n'
      AND NOT (d.classid = 'pg_class'::regclass AND d.objid = v_booking);
    IF v_dependency IS NOT NULL THEN
        RAISE EXCEPTION 'core.booking còn dependency ngoài phạm vi: %', v_dependency USING ERRCODE = '2BP01';
    END IF;
END
`$`$;
"@
& $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -c $dependencySql | Set-Content -LiteralPath (Join-Path $backupDir 'preflight.txt') -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Catalog preflight thất bại; migration chưa chạy.' }

$migrationPath = Join-Path $repoRoot 'sql\09_remove_booking.sql'
& $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -f $migrationPath | Set-Content -LiteralPath (Join-Path $backupDir 'migration.log') -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Migration xóa booking thất bại.' }

$afterCounts = ((& $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -Atc $rowCountSql) -join "`n").Trim()
if ($LASTEXITCODE -ne 0) { throw 'Không đọc được row count sau migration.' }
$afterCounts | Set-Content -LiteralPath (Join-Path $backupDir 'row-count-after.txt') -Encoding utf8
if ($beforeCounts -ne $afterCounts) { throw 'Row count nghiệp vụ thay đổi; dừng để điều tra, không tự rollback.' }

$verifySql = @"
DO `$`$
BEGIN
    IF to_regclass('core.booking') IS NOT NULL THEN
        RAISE EXCEPTION 'core.booking vẫn tồn tại sau migration.';
    END IF;
END
`$`$;
SELECT current_database(), current_user, EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'btree_gist') AS btree_gist_kept;
"@
& $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $user -d $dbName -c $verifySql | Set-Content -LiteralPath (Join-Path $backupDir 'verification-after.txt') -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Verification sau migration thất bại.' }

Write-Host "Booking removal PASS: $Target / $dbName. Backup và evidence: $backupDir" -ForegroundColor Green
