# Cấp password cho đúng ba login role của ứng dụng.
# Không ghi password vào source và không dùng database owner để chạy Admin.

[CmdletBinding()]
param(
    [ValidateSet('qa', 'main')]
    [string]$Target = 'qa'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$envPath = if ($env:NETCAFE_ENV_FILE) { $env:NETCAFE_ENV_FILE } else { Join-Path $repoRoot '.env' }
if (-not (Test-Path -LiteralPath $envPath)) { throw "Không tìm thấy .env: $envPath" }

$values = @{}
foreach ($line in Get-Content -LiteralPath $envPath) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    $parts = $trimmed.Split('=', 2)
    if ($parts.Count -eq 2) { $values[$parts[0].Trim()] = $parts[1].Trim().Trim('"').Trim("'") }
}
foreach ($key in @(
    'DB_HOST', 'DB_PORT', 'DB_NAME', 'DB_USER', 'DB_PASSWORD', 'DB_OWNER_USER', 'DB_OWNER_PASSWORD',
    'DB_ADMIN_USER', 'DB_ADMIN_PASSWORD', 'DB_MANAGER_USER', 'DB_MANAGER_PASSWORD',
    'DB_CASHIER_USER', 'DB_CASHIER_PASSWORD')) {
    $environmentValue = [Environment]::GetEnvironmentVariable($key)
    if ($environmentValue) { $values[$key] = $environmentValue }
}

$database = if ($Target -eq 'main') { 'netcafe_db' } else { 'netcafe_qa' }
$hostName = $values['DB_HOST']
$port = if ($values['DB_PORT']) { $values['DB_PORT'] } else { '5432' }
$adminUser = if ($values['DB_OWNER_USER']) { $values['DB_OWNER_USER'] } else { $values['DB_USER'] }
$adminPassword = if ($values['DB_OWNER_PASSWORD']) { $values['DB_OWNER_PASSWORD'] } else { $values['DB_PASSWORD'] }
if (-not $hostName -or -not $adminUser -or -not $adminPassword) {
    throw 'Cần DB_HOST, DB_OWNER_USER và DB_OWNER_PASSWORD của database owner/DBA để provision role.'
}

$pgCandidates = @()
if ($env:PG_BIN) { $pgCandidates += Join-Path $env:PG_BIN 'psql.exe' }
$pgCandidates += Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'bin\psql.exe' }
$psql = $pgCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $psql) { throw 'Không tìm thấy psql.exe.' }

$accounts = @(
    @{ Group = 'role_admin'; User = if ($values['DB_ADMIN_USER']) { $values['DB_ADMIN_USER'] } else { 'nv_admin' }; Password = $values['DB_ADMIN_PASSWORD'] },
    @{ Group = 'role_manager'; User = if ($values['DB_MANAGER_USER']) { $values['DB_MANAGER_USER'] } else { 'nv_manager' }; Password = $values['DB_MANAGER_PASSWORD'] },
    @{ Group = 'role_cashier'; User = if ($values['DB_CASHIER_USER']) { $values['DB_CASHIER_USER'] } else { 'nv_thao' }; Password = $values['DB_CASHIER_PASSWORD'] }
)

$env:PGPASSWORD = $adminPassword
foreach ($account in $accounts) {
    if ($account.User -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { throw "Username không hợp lệ: $($account.User)" }
    if ([string]::IsNullOrWhiteSpace($account.Password)) { throw "Thiếu password cho $($account.Group)." }
    $quotedUser = '"' + $account.User.Replace('"', '""') + '"'
    $escapedPassword = $account.Password.Replace("'", "''")
    $sql = @"
DO `$`$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '$($account.User)') THEN
        CREATE ROLE $quotedUser LOGIN;
    END IF;
END `$`$;
ALTER ROLE $quotedUser WITH LOGIN PASSWORD '$escapedPassword';
GRANT $($account.Group) TO $quotedUser;
"@
    & $psql -X -v ON_ERROR_STOP=1 -h $hostName -p $port -U $adminUser -d $database -c $sql
    if ($LASTEXITCODE -ne 0) { throw "Provision thất bại cho $($account.User)." }
}

Write-Host "Đã provision ba role ứng dụng trên $database. Password không được ghi ra log." -ForegroundColor Green
