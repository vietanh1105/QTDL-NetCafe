#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/generate_dirty_data.py
# Mục đích: Sinh dữ liệu khách hàng giả lập CHƯA CHUẨN (~12-15% bản ghi bẩn)
#           và nạp vào staging.stg_customers để phục vụ bài toán Data Quality
# ============================================================================

import os
import sys
import random
import re
from datetime import datetime

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

# Đọc cấu hình từ .env thủ công hoặc qua dotenv
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

# Kết nối CSDL hỗ trợ cả psycopg2 và psycopg
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

RANDOM_SEED = 42
random.seed(RANDOM_SEED)

# Ngân hàng họ tên tiếng Việt (Fallback không cần thư viện bên ngoài)
HO_LIST = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"]
DEM_LIST = ["Văn", "Thị", "Đình", "Đức", "Hữu", "Minh", "Quang", "Tuấn", "Ngọc", "Thanh", "Quốc", "Hải", "Xuân", "Thành"]
TEN_LIST = ["Anh", "Bình", "Cường", "Dũng", "Giang", "Hải", "Hùng", "Huy", "Khánh", "Linh", "Long", "Minh", "Nam", "Phong", "Quân", "Sơn", "Thảo", "Tuấn", "Tùng", "Vinh", "Việt"]

def generate_random_vn_name():
    return f"{random.choice(HO_LIST)} {random.choice(DEM_LIST)} {random.choice(TEN_LIST)}"

def generate_dirty_customers(n=500):
    rows = []
    tiers = ['regular', 'silver', 'gold', 'vip']

    for i in range(1, n + 1):
        full_name = generate_random_vn_name()
        clean_phone = "0" + random.choice(["3", "5", "7", "8", "9"]) + f"{random.randint(10000000, 99999999)}"
        email_user = re.sub(r'[^a-z0-9]', '', full_name.lower())[:8] + str(random.randint(10, 99))
        email = f"{email_user}@gmail.com"
        username = f"user_{i}_{clean_phone[-4:]}"
        balance = str(random.choice([0, 20000, 50000, 100000, 200000, 500000]))
        points = str(random.randint(0, 1500))
        tier = random.choice(tiers)

        # Xác suất ~15% sinh dữ liệu bẩn
        dirty_flag = random.random() < 0.15

        if dirty_flag:
            error_type = random.choice([
                'phone_plus84', 'phone_dot_space', 'phone_invalid_short',
                'name_casing_space', 'email_invalid', 'negative_balance',
                'duplicate_near'
            ])

            if error_type == 'phone_plus84':
                clean_phone = "+84" + clean_phone[1:]
            elif error_type == 'phone_dot_space':
                clean_phone = f"{clean_phone[:4]}.{clean_phone[4:7]}.{clean_phone[7:]}"
            elif error_type == 'phone_invalid_short':
                clean_phone = "12345"
            elif error_type == 'name_casing_space':
                full_name = f"   {full_name.upper()}   "
            elif error_type == 'email_invalid':
                email = "sai-email-khong-hop-le"
            elif error_type == 'negative_balance':
                balance = "-50000"
            elif error_type == 'duplicate_near':
                full_name = "Nguyen Van Hung"
                clean_phone = "0987654321"

        rows.append((full_name, clean_phone, email, username, balance, points, tier, "BATCH_DIRTY_01"))

    return rows

def load_to_staging(rows):
    print(f"[*] Đang kết nối tới PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}...")
    conn = get_db_connection()
    try:
        with conn.cursor() as cur:
            # Xóa batch cũ trong staging
            cur.execute("DELETE FROM staging.stg_customers WHERE source_batch = 'BATCH_DIRTY_01';")
            
            print(f"[*] Đang nạp {len(rows)} dòng dữ liệu bẩn vào staging.stg_customers...")
            try:
                from psycopg2.extras import execute_values
                query = """
                    INSERT INTO staging.stg_customers 
                    (raw_full_name, raw_phone, raw_email, raw_username, raw_balance, raw_points, raw_tier, source_batch)
                    VALUES %s;
                """
                execute_values(cur, query, rows)
            except ImportError:
                with cur.copy("COPY staging.stg_customers (raw_full_name, raw_phone, raw_email, raw_username, raw_balance, raw_points, raw_tier, source_batch) FROM STDIN") as copy:
                    for r in rows:
                        copy.write_row(r)
            
            conn.commit()
            print("[+] Nạp dữ liệu staging thành công (500 bản ghi thô)!")
    finally:
        conn.close()

if __name__ == "__main__":
    records = generate_dirty_customers(500)
    load_to_staging(records)
