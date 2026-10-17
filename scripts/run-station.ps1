# Trách nhiệm: khởi chạy NetCafe.Client bằng station role và computer_code cố định.
# Credential lấy từ .env local; không dùng DB_USER/DB_PASSWORD của Admin và không in password.

param(
    [string]$ComputerCode = "PC001"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$envPath = if ($env:NETCAFE_ENV_FILE) { $env:NETCAFE_ENV_FILE } else { Join-Path $repoRoot ".env" }
if (-not (Test-Path -LiteralPath $envPath)) { throw "Không tìm thấy .env: $envPath" }

$values = @{}
foreach ($line in Get-Content -LiteralPath $envPath) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    $parts = $trimmed.Split('=', 2)
    if ($parts.Count -eq 2) { $values[$parts[0].Trim()] = $parts[1].Trim().Trim('"').Trim("'") }
}
foreach ($key in @('DB_HOST', 'DB_PORT', 'DB_NAME', 'DB_SSL_MODE', 'STATION_DB_USER', 'STATION_DB_PASSWORD')) {
    if ([Environment]::GetEnvironmentVariable($key)) { $values[$key] = [Environment]::GetEnvironmentVariable($key) }
}
if (-not $values['STATION_DB_USER'] -or -not $values['STATION_DB_PASSWORD']) {
    throw "Thiếu STATION_DB_USER hoặc STATION_DB_PASSWORD; station không được dùng credential owner."
}

$env:NETCAFE_COMPUTER_CODE = $ComputerCode
$env:STATION_DB_USER = $values['STATION_DB_USER']
$env:STATION_DB_PASSWORD = $values['STATION_DB_PASSWORD']
$env:DB_HOST = $values['DB_HOST']
$env:DB_PORT = $values['DB_PORT']
$env:DB_NAME = $values['DB_NAME']
$env:DB_SSL_MODE = $values['DB_SSL_MODE']

dotnet run --project (Join-Path $repoRoot "app\NetCafe.Client\NetCafe.Client.csproj")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
