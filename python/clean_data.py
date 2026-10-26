#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/clean_data.py
# Mục đích: Làm sạch và chuẩn hóa dữ liệu từ staging.stg_customers
#           - Chuẩn hóa SĐT về dạng 0xxxxxxxxx, họ tên Title Case, email lowercase
#           - Đẩy bản ghi không cứu được vào staging.rejected_rows kèm lý do
#           - Nạp bản ghi hợp lệ vào core.customer
#           - Xuất báo cáo thống kê trước/sau vào evidence/clean_data_stats.txt
# ============================================================================

import os
import sys
import re
import json

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def load_env_file():
    env_vars = {}
    env_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), ".env")
    if os.path.exists(env_path):
        with open(env_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    k, v = line.split("=", 1)
                    env_vars[k.strip()] = v.strip().strip("'").strip('"')
    return env_vars

env = load_env_file()
DB_HOST = env.get("DB_HOST", os.getenv("DB_HOST", "127.0.0.1"))
DB_PORT = env.get("DB_PORT", os.getenv("DB_PORT", "5432"))
DB_NAME = env.get("DB_NAME", os.getenv("DB_NAME", "netcafe_db"))
DB_USER = env.get("DB_USER", os.getenv("DB_USER", "postgres"))
DB_PASSWORD = env.get("DB_PASSWORD", os.getenv("DB_PASSWORD", ""))
DB_SSL_MODE = env.get("DB_SSL_MODE", os.getenv("DB_SSL_MODE", "prefer"))

def get_db_connection():
    try:
        import psycopg2
        return psycopg2.connect(
            host=DB_HOST,
            port=DB_PORT,
            dbname=DB_NAME,
            user=DB_USER,
            password=DB_PASSWORD,
            sslmode=DB_SSL_MODE.lower() if DB_SSL_MODE else "prefer"
        )
    except ImportError:
        import psycopg
        ssl_str = f"?sslmode={DB_SSL_MODE.lower()}" if DB_SSL_MODE else ""
        conn_str = f"postgresql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}:{DB_PORT}/{DB_NAME}{ssl_str}"
        return psycopg.connect(conn_str)

PHONE_REGEX = re.compile(r"^0[35789][0-9]{8}$")
EMAIL_REGEX = re.compile(r"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$")

def clean_and_load():
    print(f"[*] Bắt đầu tiến trình làm sạch dữ liệu từ staging trên {DB_HOST}...")

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
    seen_emails = set()
    seen_usernames = set()

    conn = get_db_connection()
    try:
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

            # Lấy danh sách SĐT, Email, Username đã tồn tại trong core.customer để tránh vi phạm Unique
            cur.execute("SELECT phone FROM core.customer;")
            for p in cur.fetchall():
                seen_phones.add(p[0])
            cur.execute("SELECT email FROM core.customer WHERE email IS NOT NULL;")
            for em in cur.fetchall():
                seen_emails.add(em[0])
            cur.execute("SELECT username FROM core.customer;")
            for un in cur.fetchall():
                seen_usernames.add(un[0])

            # Mật khẩu hash mặc định (mã hóa bcrypt giả lập hoặc chuỗi hash chuẩn)
            cur.execute("SELECT crypt('Pass@123456', gen_salt('bf'));")
            default_pwd_hash = cur.fetchone()[0]

            for row in raw_rows:
                raw_id, raw_name, raw_phone, raw_email, raw_username, raw_balance, raw_points, raw_tier = row
                raw_dict = {
                    "raw_id": raw_id, "name": raw_name, "phone": raw_phone,
                    "email": raw_email, "username": raw_username, "balance": raw_balance
                }

                # Chuẩn hóa họ tên: cắt khoảng trắng dư thừa, đưa về Title Case
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

                # Kiểm tra số dư không âm
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
                    email = None
                elif email:
                    seen_emails.add(email)

                # Chuẩn hóa Username
                username = raw_username.strip().lower() if raw_username else f"cust_{phone[-6:]}"
                if username in seen_usernames:
                    username = f"u_{phone[-6:]}"
                seen_usernames.add(username)

                tier = raw_tier if raw_tier in ['regular', 'silver', 'gold', 'vip'] else 'regular'
                points = int(raw_points) if raw_points and str(raw_points).isdigit() else 0

                seen_phones.add(phone)
                clean_records.append((
                    name, phone, email, username, default_pwd_hash, int(balance), points, tier, True
                ))

            stats["clean_loaded"] = len(clean_records)
            stats["rejected_total"] = len(rejected_records)

            # Xóa các dòng rejected cũ của batch này
            cur.execute("DELETE FROM staging.rejected_rows WHERE source_table = 'core.customer';")

            # 2. Ghi nhận các bản ghi bị từ chối vào staging.rejected_rows
            if rejected_records:
                print(f"[*] Đang ghi nhận {len(rejected_records)} bản ghi không cứu được vào staging.rejected_rows...")
                try:
                    from psycopg2.extras import execute_values
                    query = "INSERT INTO staging.rejected_rows (source_table, raw_record, error_reason) VALUES %s;"
                    execute_values(cur, query, rejected_records)
                except ImportError:
                    for rej in rejected_records:
                        cur.execute("INSERT INTO staging.rejected_rows (source_table, raw_record, error_reason) VALUES (%s, %s, %s);", rej)

            # 3. Nạp các bản ghi sạch vào core.customer
            if clean_records:
                print(f"[*] Đang nạp {len(clean_records)} bản ghi sạch vào core.customer...")
                try:
                    from psycopg2.extras import execute_values
                    query = """
                        INSERT INTO core.customer 
                        (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier, is_active)
                        VALUES %s;
                    """
                    execute_values(cur, query, clean_records)
                except ImportError:
                    for cln in clean_records:
                        cur.execute("""
                            INSERT INTO core.customer 
                            (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier, is_active)
                            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s);
                        """, cln)

            conn.commit()
    finally:
        conn.close()

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
