#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/clean_data.py
# Mục đích: Làm sạch và chuẩn hóa dữ liệu từ staging.stg_customers
#           - Chuẩn hóa SĐT về dạng 0xxxxxxxxx, họ tên Title Case, email lowercase
#           - Đẩy bản ghi không cứu được vào staging.rejected_rows kèm lý do
#           - Nạp bản ghi hợp lệ vào core.customer bằng COPY
#           - Xuất báo cáo thống kê trước/sau vào evidence/clean_data_stats.txt
# ============================================================================

import os
import sys
import re
import json
from dotenv import load_dotenv
import psycopg

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

load_dotenv()

DB_HOST = os.getenv("DB_HOST", "127.0.0.1")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "netcafe_db")
DB_USER = os.getenv("DB_USER", "postgres")
DB_PASSWORD = os.getenv("DB_PASSWORD", "vietanh1105@")

CONN_STRING = f"host={DB_HOST} port={DB_PORT} dbname={DB_NAME} user={DB_USER} password={DB_PASSWORD}"

PHONE_REGEX = re.compile(r"^0[35789][0-9]{8}$")
EMAIL_REGEX = re.compile(r"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$")

def clean_and_load():
    print(f"[*] Bắt đầu tiến trình làm sạch dữ liệu từ staging...")

    stats = {
        "total_processed": 0,
        "clean_loaded": 0,
        "rejected_total": 0,
        "rejected_by_reason": {},
        "normalized_phone_count": 0,
        "normalized_name_count": 0
    }

    clean_records = []
    rejected_records = []
    seen_phones = set()

    with psycopg.connect(CONN_STRING) as conn:
        with conn.cursor() as cur:
            # 1. Đọc dữ liệu từ staging
            cur.execute("""
                SELECT raw_id, raw_full_name, raw_phone, raw_email, raw_username, 
                       raw_balance, raw_points, raw_tier
                FROM staging.stg_customers
                WHERE source_batch = 'BATCH_DIRTY_01'
                ORDER BY raw_id;
            """)
            raw_rows = cur.fetchall()
            stats["total_processed"] = len(raw_rows)

            # Lấy danh sách SĐT, Email, Username đã tồn tại trong core.customer để tránh trùng
            seen_emails = set()
            seen_usernames = set()
            cur.execute("SELECT phone FROM core.customer;")
            for p in cur.fetchall():
                seen_phones.add(p[0])
            cur.execute("SELECT email FROM core.customer WHERE email IS NOT NULL;")
            for em in cur.fetchall():
                seen_emails.add(em[0])
            cur.execute("SELECT username FROM core.customer;")
            for un in cur.fetchall():
                seen_usernames.add(un[0])

            # Lấy mật khẩu hash mặc định cho các khách mới
            cur.execute("SELECT crypt('Pass@123', gen_salt('bf'));")
            default_pwd_hash = cur.fetchone()[0]

            for row in raw_rows:
                raw_id, raw_name, raw_phone, raw_email, raw_username, raw_balance, raw_points, raw_tier = row
                raw_dict = {
                    "raw_id": raw_id, "name": raw_name, "phone": raw_phone,
                    "email": raw_email, "username": raw_username, "balance": raw_balance
                }

                # Chuẩn hóa họ tên
                name = " ".join(raw_name.split()).title() if raw_name else ""
                if name != raw_name:
                    stats["normalized_name_count"] += 1

                # Chuẩn hóa số điện thoại
                phone = raw_phone.strip() if raw_phone else ""
                phone = phone.replace(".", "").replace("-", "").replace(" ", "")
                if phone.startswith("+84"):
                    phone = "0" + phone[3:]
                    stats["normalized_phone_count"] += 1
                elif phone.startswith("84") and len(phone) == 11:
                    phone = "0" + phone[2:]
                    stats["normalized_phone_count"] += 1

                # Kiểm tra số điện thoại có đúng chuẩn Regex không
                if not PHONE_REGEX.match(phone):
                    rejected_records.append((
                        "core.customer", json.dumps(raw_dict, ensure_ascii=False), "PHONE_FORMAT_INVALID"
                    ))
                    stats["rejected_by_reason"]["PHONE_FORMAT_INVALID"] = stats["rejected_by_reason"].get("PHONE_FORMAT_INVALID", 0) + 1
                    continue

                # Kiểm tra trùng lặp số điện thoại
                if phone in seen_phones:
                    rejected_records.append((
                        "core.customer", json.dumps(raw_dict, ensure_ascii=False), "DUPLICATE_PHONE"
                    ))
                    stats["rejected_by_reason"]["DUPLICATE_PHONE"] = stats["rejected_by_reason"].get("DUPLICATE_PHONE", 0) + 1
                    continue

                # Kiểm tra số dư
                try:
                    balance = float(raw_balance)
                    if balance < 0:
                        rejected_records.append((
                            "core.customer", json.dumps(raw_dict, ensure_ascii=False), "NEGATIVE_BALANCE"
                        ))
                        stats["rejected_by_reason"]["NEGATIVE_BALANCE"] = stats["rejected_by_reason"].get("NEGATIVE_BALANCE", 0) + 1
                        continue
                except (ValueError, TypeError):
                    rejected_records.append((
                        "core.customer", json.dumps(raw_dict, ensure_ascii=False), "INVALID_BALANCE_VALUE"
                    ))
                    stats["rejected_by_reason"]["INVALID_BALANCE_VALUE"] = stats["rejected_by_reason"].get("INVALID_BALANCE_VALUE", 0) + 1
                    continue

                # Chuẩn hóa Email
                email = raw_email.strip().lower() if raw_email else None
                if email and not EMAIL_REGEX.match(email):
                    email = None
                if email and email in seen_emails:
                    email = None  # Cứu khách bằng cách bỏ email trùng
                elif email:
                    seen_emails.add(email)

                # Chuẩn hóa Username
                username = raw_username.strip().lower() if raw_username else f"cust_{phone[-6:]}"
                if username in seen_usernames:
                    username = f"u_{phone[-6:]}"
                seen_usernames.add(username)

                tier = raw_tier if raw_tier in ['regular', 'silver', 'gold', 'vip'] else 'regular'
                points = int(raw_points) if raw_points and raw_points.isdigit() else 0

                seen_phones.add(phone)
                clean_records.append((
                    name, phone, email, username, default_pwd_hash, int(balance), points, tier, True
                ))

            stats["clean_loaded"] = len(clean_records)
            stats["rejected_total"] = len(rejected_records)

            # 2. Ghi nhận các dòng bị từ chối vào staging.rejected_rows
            if rejected_records:
                print(f"[*] Ghi nhận {len(rejected_records)} bản ghi không cứu được vào staging.rejected_rows...")
                with cur.copy("COPY staging.rejected_rows (source_table, raw_record, error_reason) FROM STDIN") as copy:
                    for rej in rejected_records:
                        copy.write_row(rej)

            # 3. Nạp các bản ghi sạch vào core.customer bằng COPY
            if clean_records:
                print(f"[*] Nạp {len(clean_records)} bản ghi sạch vào core.customer bằng COPY...")
                with cur.copy("COPY core.customer (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier, is_active) FROM STDIN") as copy:
                    for cln in clean_records:
                        copy.write_row(cln)

            conn.commit()

    # 4. Xuất báo cáo thống kê
    os.makedirs("evidence", exist_ok=True)
    report_file = os.path.join("evidence", "clean_data_stats.txt")
    with open(report_file, "w", encoding="utf-8") as f:
        f.write("============================================================\n")
        f.write("BÁO CÁO THỐNG KÊ LÀM SẠCH VÀ CHUẨN HÓA DỮ LIỆU (ETL CLEANING)\n")
        f.write("============================================================\n\n")
        f.write(f"1. Tổng số dòng dữ liệu thô xử lý:           {stats['total_processed']} dòng\n")
        f.write(f"2. Số dòng chuẩn hóa và nạp thành công:      {stats['clean_loaded']} dòng\n")
        f.write(f"3. Số dòng không thể cứu (Đưa vào reject):    {stats['rejected_total']} dòng\n")
        f.write(f"   - Tỷ lệ dữ liệu bẩn / lỗi loại trừ:        {(stats['rejected_total']/stats['total_processed'])*100:.2f}%\n\n")
        f.write(f"4. Thống kê thao tác chuẩn hóa thành công:\n")
        f.write(f"   - Số điện thoại chuẩn hóa (+84/dấu chấm/cách): {stats['normalized_phone_count']} trường hợp\n")
        f.write(f"   - Họ tên cắt khoảng trắng thừa / Title Case:   {stats['normalized_name_count']} trường hợp\n\n")
        f.write(f"5. Chi tiết các lý do từ chối (staging.rejected_rows):\n")
        for reason, count in stats['rejected_by_reason'].items():
            f.write(f"   - {reason:30}: {count} dòng\n")
        f.write("\n============================================================\n")

    print(f"[+] Hoàn tất! Báo cáo thống kê đã lưu tại: {report_file}")
    with open(report_file, "r", encoding="utf-8") as f:
        print(f.read())

if __name__ == "__main__":
    clean_and_load()
