# ==============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# Script: backup/backup.ps1
# Mục đích: Tự động sao lưu dự phòng CSDL PostgreSQL 17 (Windows PowerShell)
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$DB_HOST = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$DB_PORT = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$DB_NAME = if ($env:DB_NAME) { $env:DB_NAME } else { "netcafe_qa" }
$DB_USER = if ($env:DB_USER) { $env:DB_USER } else { "postgres" }
if (-not $env:PGPASSWORD) { $env:PGPASSWORD = "" }

$PG_BIN = "C:\Program Files\PostgreSQL\17\bin"
$PG_DUMP = Join-Path $PG_BIN "pg_dump.exe"
if (-not (Test-Path $PG_DUMP)) {
    $PG_DUMP = "pg_dump"
}

$BACKUP_DIR = $PSScriptRoot
$TIMESTAMP = (Get-Date).ToString("yyyyMMdd_HHmmss")
$DUMP_FILE = Join-Path $BACKUP_DIR "netcafe_${TIMESTAMP}.dump"
$SQL_FILE  = Join-Path $BACKUP_DIR "netcafe_${TIMESTAMP}.sql"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "BẮT ĐẦU SAO LƯU DỰ PHÒNG CSDL: $DB_NAME" -ForegroundColor Cyan
Write-Host "Thời gian: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "==================================================================" -ForegroundColor Cyan

# 1. Sao lưu định dạng nén Custom Format (-Fc) cho pg_restore
Write-Host "1. Đang tạo bản sao lưu nhị phân nén (-Fc)..." -NoNewline
& $PG_DUMP -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -Fc -f $DUMP_FILE
Write-Host " [HOÀN TẤT]" -ForegroundColor Green
$dumpSize = (Get-Item $DUMP_FILE).Length / 1KB
Write-Host "   -> File: $DUMP_FILE ($([Math]::Round($dumpSize, 2)) KB)" -ForegroundColor DarkGray

# 2. Sao lưu định dạng văn bản thuần SQL (-Fp) cho kiểm tra thủ công
Write-Host "2. Đang tạo bản sao lưu văn bản SQL (-Fp)..." -NoNewline
& $PG_DUMP -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -Fp --clean --if-exists -f $SQL_FILE
Write-Host " [HOÀN TẤT]" -ForegroundColor Green
$sqlSize = (Get-Item $SQL_FILE).Length / 1KB
Write-Host "   -> File: $SQL_FILE ($([Math]::Round($sqlSize, 2)) KB)" -ForegroundColor DarkGray

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "SAO LƯU THÀNH CÔNG!" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Cyan
