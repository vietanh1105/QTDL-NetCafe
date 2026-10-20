# Trách nhiệm: ghép các SQL nguồn thành sql/netcafe_full.sql theo đúng thứ tự triển khai.
# Liên kết: sql/00_create_database.sql đến sql/10_admin_operations.sql; không chạy migration.

import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

SQL_FILES = [
    "sql/00_create_database.sql",
    "sql/01_schema.sql",
    "sql/02_functions_triggers.sql",
    "sql/03_views.sql",
    "sql/04_security.sql",
    "sql/05_indexes_partition.sql",
    "sql/06_seed_base.sql",
    "sql/07_ai_modules.sql",
    "sql/08_station_customer.sql",
    "sql/10_admin_operations.sql",
]

TARGET_FILE = "sql/netcafe_full.sql"

def main():
    print("=== TỔNG HỢP TOÀN BỘ CSDL VÀO sql/netcafe_full.sql ===")
    full_content = []
    
    header = """-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: netcafe_full.sql (TẬP LỆNH TỔNG HỢP TOÀN BỘ HỆ THỐNG)
-- Mô tả: File duy nhất cho phép giáo viên tái tạo toàn bộ hệ thống từ một máy chủ trống
--        Bao gồm: Database -> Schemas -> Tables -> Contraints -> Functions/Triggers 
--                 -> Views -> Security (RBAC/RLS) -> Indexes & Partition -> Seed Data -> 5 AI Modules
-- Tính chất: 100% IDEMPOTENT - Có thể chạy lại nhiều lần mà không phát sinh lỗi.
-- ============================================================================
"""
    full_content.append(header)

    for fpath in SQL_FILES:
        if not os.path.exists(fpath):
            print(f"[!] Lỗi: Không tìm thấy file {fpath}")
            sys.exit(1)
        print(f"-> Đang ghép {fpath}...")
        with open(fpath, "r", encoding="utf-8") as f:
            content = f.read()
        full_content.append(f"\n\n-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")
        full_content.append(f"-- BẮT ĐẦU NỘI DUNG TỪ: {fpath}")
        full_content.append(f"-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n")
        full_content.append(content)

    with open(TARGET_FILE, "w", encoding="utf-8") as f:
        f.write("\n".join(full_content))

    size_kb = os.path.getsize(TARGET_FILE) / 1024
    print(f"[+] ĐÃ TẠO THÀNH CÔNG {TARGET_FILE} ({size_kb:.2f} KB)!")

if __name__ == "__main__":
    main()
