#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/generate_dirty_data.py
# Mục đích: Sinh 20 dòng dữ liệu khách hàng giả lập CHƯA CHUẨN (gọn gàng, vừa vặn 1 màn hình)
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

HO_LIST = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"]
DEM_LIST = ["Văn", "Thị", "Đình", "Đức", "Hữu", "Minh", "Quang", "Tuấn", "Ngọc", "Thanh", "Quốc", "Hải", "Xuân", "Thành"]
TEN_LIST = ["Anh", "Bình", "Cường", "Dũng", "Giang", "Hải", "Hùng", "Huy", "Khánh", "Linh", "Long", "Minh", "Nam", "Phong", "Quân", "Sơn", "Thảo", "Tuấn", "Tùng", "Vinh", "Việt"]

def generate_random_vn_name():
    return f"{random.choice(HO_LIST)} {random.choice(DEM_LIST)} {random.choice(TEN_LIST)}"

def generate_dirty_customers(n=20):
    rows = []
    tiers = ['regular', 'silver', 'gold', 'vip']

    for i in range(1, n + 1):
        full_name = generate_random_vn_name()
        # Đảm bảo SĐT gốc hoàn toàn duy nhất tránh va chạm ngẫu nhiên
        clean_phone = f"0977{100000 + i}"
        email_user = re.sub(r'[^a-z0-9]', '', full_name.lower())[:8] + str(random.randint(10, 99))
        email = f"{email_user}@gmail.com"
        username = f"user_{i}_{clean_phone[-4:]}"
        balance = str(random.choice([20000, 50000, 100000, 200000, 500000]))
        points = str(random.randint(10, 500))
        tier = random.choice(tiers)

        # CÀI ĐẶT RÕ RÀNG CÁC TRƯỜNG HỢP LỖI BẨN TRONG 20 DÒNG (ĐỂ THẦY NHÌN THẤY NGAY)
        if i == 1:
            # Dòng 1: SĐT dính mã quốc tế +84 (cứu được -> chuẩn hóa bỏ +84 về 0977...)
            clean_phone = "+84" + clean_phone[1:]
        elif i == 2:
            # Dòng 2: Tên viết thường toàn bộ + thừa khoảng trắng, SĐT dính dấu chấm
            full_name = "   lê   thanh   phong   "
            clean_phone = f"{clean_phone[:4]}.{clean_phone[4:7]}.{clean_phone[7:]}"
        elif i == 3:
            # Dòng 3: Tên in hoa toàn bộ và thụt lề thừa khoảng trắng (cứu được -> Title Case)
            full_name = f"   {full_name.upper()}   "
        elif i == 4:
            # Dòng 4: Số dư âm vô lý (lỗi vi phạm tài chính -> cách ly vào rejected_rows)
            balance = "-50000"
        elif i == 5:
            # Dòng 5: SĐT cụt chỉ có 5 số (lỗi nặng -> cách ly vào rejected_rows)
            clean_phone = "12345"
        elif i == 6:
            # Dòng 6: Tên lộn xộn hoa thường, email có khoảng trắng và chữ hoa (cứu được -> chuẩn hóa đầy đủ)
            full_name = "  pHạM   tHị   nAm  "
            email = "   PhamThiNam@gmail.com   "
        elif i == 7:
            # Dòng 7: Tên viết thường, SĐT dính dấu chấm phân cách (cứu được -> Title Case và bỏ dấu chấm)
            full_name = "võ xuân bình"
            clean_phone = f"{clean_phone[:4]}.{clean_phone[4:7]}.{clean_phone[7:]}"
        elif i == 8:
            # Dòng 8: Số dư âm vô lý (lỗi vi phạm tài chính -> cách ly vào rejected_rows)
            balance = "-100000"
        elif i == 9:
            # Dòng 9: SĐT cụt chỉ có 4 số (lỗi nặng -> cách ly vào rejected_rows)
            clean_phone = "8888"
        elif i == 10:
            # Dòng 10: Tên in hoa toàn bộ thừa khoảng trắng
            full_name = f"   {full_name.upper()}   "
        elif i == 11:
            # Dòng 11: Tên viết thường thừa khoảng trắng
            full_name = "  đỗ   đình   minh  "
        elif i == 14:
            # Dòng 14: Tên pha trộn chữ hoa chữ thường
            full_name = "tRầN   đỨc   bÌnH"
        elif i == 17:
            # Dòng 17: Tên in hoa toàn bộ
            full_name = "   NGÔ THANH NAM   "
        # Các dòng còn lại là khách hàng hợp lệ với đa dạng họ tên Việt Nam

        rows.append((full_name, clean_phone, email, username, balance, points, tier, "BATCH_DIRTY_01"))

    return rows

def load_to_staging(rows):
    print(f"[*] Đang kết nối tới PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}...")
    conn = get_db_connection()
    try:
        with conn.cursor() as cur:
            # Xóa và reset ID trong staging để raw_id luôn bắt đầu từ 1 đến 20
            cur.execute("TRUNCATE staging.stg_customers RESTART IDENTITY;")
            
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
            print(f"[+] Nạp dữ liệu staging thành công (đúng {len(rows)} bản ghi thô)!")
            print("[*] Gói gọn đúng 20 dòng để vừa vặn hiển thị trên 1 màn hình máy chiếu!")
    finally:
        conn.close()

if __name__ == "__main__":
    records = generate_dirty_customers(20)
    load_to_staging(records)
