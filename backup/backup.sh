#!/usr/bin/env bash
# ==============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# Script: backup/backup.sh
# Mục đích: Tự động sao lưu dự phòng CSDL PostgreSQL 17 (Linux / Docker)
# ==============================================================================

set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-netcafe_db}"
DB_USER="${DB_USER:-postgres}"
export PGPASSWORD="${PGPASSWORD:-vietanh1105@}"

BACKUP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
DUMP_FILE="${BACKUP_DIR}/netcafe_${TIMESTAMP}.dump"
SQL_FILE="${BACKUP_DIR}/netcafe_${TIMESTAMP}.sql.gz"

echo "=================================================================="
echo "BẮT ĐẦU SAO LƯU DỰ PHÒNG CSDL: ${DB_NAME}"
echo "Thời gian: $(date '+%Y-%m-%d %H:%M:%S')"
echo "=================================================================="

# 1. Sao lưu định dạng nén Custom Format (-Fc)
echo -n "1. Đang tạo bản sao lưu nhị phân nén (-Fc)... "
pg_dump -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -Fc -f "${DUMP_FILE}"
echo "[HOÀN TẤT]"
ls -lh "${DUMP_FILE}"

# 2. Sao lưu plain SQL và nén gzip
echo -n "2. Đang tạo bản sao lưu văn bản nén (SQL.gz)... "
pg_dump -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -Fp --clean --if-exists | gzip > "${SQL_FILE}"
echo "[HOÀN TẤT]"
ls -lh "${SQL_FILE}"

# 3. Dọn dẹp bản sao lưu cũ hơn 7 ngày
find "${BACKUP_DIR}" -name "netcafe_*.dump" -mtime +7 -delete || true
find "${BACKUP_DIR}" -name "netcafe_*.sql.gz" -mtime +7 -delete || true

echo "=================================================================="
echo "SAO LƯU DỰ PHÒNG THÀNH CÔNG!"
echo "=================================================================="
