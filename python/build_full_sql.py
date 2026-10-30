# Trách nhiệm: ghép các SQL nguồn thành sql/netcafe_full.sql theo đúng thứ tự triển khai.
# Liên kết: sql/00_create_database.sql đến sql/13_ai_demo_situations.sql; không chạy migration production.

import os
import sys
import argparse
import re

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
    "sql/11_report_alignment.sql",
    "sql/12_security_hardening.sql",
    "sql/13_ai_demo_situations.sql",
    "sql/14_defense_demo_fixes.sql",
]

TARGET_FILE = "sql/netcafe_full.sql"

def main():
    parser = argparse.ArgumentParser(description="Ghép SQL khởi tạo từ các module nguồn.")
    parser.add_argument("--cloud", action="store_true", help="Xuất SQL cho database Neon đang kết nối.")
    args = parser.parse_args()
    target_file = "sql/netcafe_cloud_init.sql" if args.cloud else TARGET_FILE
    print(f"=== TỔNG HỢP CSDL VÀO {target_file} ===")
    full_content = []
    
    header = """-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: netcafe_full.sql (TẬP LỆNH TỔNG HỢP TOÀN BỘ HỆ THỐNG)
-- Mô tả: File duy nhất cho phép giáo viên tái tạo toàn bộ hệ thống từ một máy chủ trống
--        Bao gồm: Database -> Schemas -> Tables -> Contraints -> Functions/Triggers 
--                 -> Views -> Security (RBAC/RLS) -> Indexes & Partition -> Seed Data -> 5 AI Modules
-- Liên kết: các file SQL nguồn trong SQL_FILES.
-- Đây là script TÁI TẠO DỮ LIỆU DEMO, không phải migration giữ dữ liệu.
-- Chỉ chạy trên database demo riêng; bản full có DROP DATABASE và lệnh psql.
-- ============================================================================
"""
    full_content.append(header)
    if args.cloud:
        full_content.append("-- Bản Cloud không tạo/xóa database; tái tạo bảng trong database đang kết nối.")
        full_content.append("BEGIN;\nSET LOCAL TIME ZONE 'Asia/Ho_Chi_Minh';")
        full_content.append(
            "DROP SCHEMA IF EXISTS core, inventory, billing, hr, audit, ai, reporting, staging CASCADE;"
        )

    for fpath in SQL_FILES:
        if args.cloud and fpath == "sql/00_create_database.sql":
            continue
        if not os.path.exists(fpath):
            print(f"[!] Lỗi: Không tìm thấy file {fpath}")
            sys.exit(1)
        print(f"-> Đang ghép {fpath}...")
        with open(fpath, "r", encoding="utf-8") as f:
            content = f.read()
        if args.cloud:
            content = re.sub(r"(?m)^\\c\s+.*$", "", content)
            if fpath == "sql/14_defense_demo_fixes.sql":
                content = re.sub(r"(?m)^(?:BEGIN|COMMIT);\s*$", "", content)
        content = "\n".join(line.rstrip() for line in content.splitlines()) + "\n"
        full_content.append(f"\n\n-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")
        full_content.append(f"-- BẮT ĐẦU NỘI DUNG TỪ: {fpath}")
        full_content.append(f"-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n")
        full_content.append(content)

    if args.cloud:
        full_content.append("COMMIT;")
    with open(target_file, "w", encoding="utf-8") as f:
        f.write("\n".join(full_content))

    size_kb = os.path.getsize(target_file) / 1024
    print(f"[+] ĐÃ TẠO THÀNH CÔNG {target_file} ({size_kb:.2f} KB)!")

if __name__ == "__main__":
    main()
