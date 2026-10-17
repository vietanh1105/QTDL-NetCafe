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
from dotenv import load_dotenv
import psycopg
from faker import Faker

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

load_dotenv()

# Cố định random seed để kết quả hoàn toàn tái lập được (Idempotent)
RANDOM_SEED = 42
random.seed(RANDOM_SEED)
fake = Faker('vi_VN')
Faker.seed(RANDOM_SEED)

DB_HOST = os.getenv("DB_HOST", "127.0.0.1")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "netcafe_qa")
DB_USER = os.getenv("DB_USER", "postgres")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")

CONN_STRING = f"host={DB_HOST} port={DB_PORT} dbname={DB_NAME} user={DB_USER} password={DB_PASSWORD}"

def generate_dirty_customers(n=500):
    rows = []
    tiers = ['regular', 'silver', 'gold', 'vip', 'diamond_invalid']

    for i in range(1, n + 1):
        full_name = fake.name()
        clean_phone = "0" + random.choice(["3", "5", "7", "8", "9"]) + f"{random.randint(10000000, 99999999)}"
        email = fake.email()
        username = f"user_{i}_{clean_phone[-4:]}"
        balance = str(random.choice([0, 20000, 50000, 100000, 200000, 500000]))
        points = str(random.randint(0, 1500))
        tier = random.choice(['regular', 'silver', 'gold', 'vip'])

        # Xác suất ~15% sinh dữ liệu bẩn
        dirty_flag = random.random() < 0.15

        if dirty_flag:
            error_type = random.choice([
                'phone_plus84', 'phone_dot_space', 'phone_invalid_short',
                'name_casing_space', 'email_invalid', 'negative_balance',
                'duplicate_near'
            ])

            if error_type == 'phone_plus84':
                # SĐT lẫn mã quốc tế +84 (cứu được)
                clean_phone = "+84" + clean_phone[1:]
            elif error_type == 'phone_dot_space':
                # SĐT lẫn dấu chấm hoặc khoảng trắng: 091.234.5678 (cứu được)
                clean_phone = f"{clean_phone[:4]}.{clean_phone[4:7]}.{clean_phone[7:]}"
            elif error_type == 'phone_invalid_short':
                # SĐT quá ngắn không cứu được (phải vào rejected_rows)
                clean_phone = "12345"
            elif error_type == 'name_casing_space':
                # Tên thừa khoảng trắng, viết hoa lộn xộn (cứu được)
                full_name = f"   {full_name.upper()}   "
            elif error_type == 'email_invalid':
                # Email sai định dạng không có tên miền (cứu được bằng cách bỏ hoặc reject)
                email = "sai-email-khong-hop-le"
            elif error_type == 'negative_balance':
                # Số dư âm (vô lý, không cứu được)
                balance = "-50000"
            elif error_type == 'duplicate_near':
                # Khách hàng có họ tên gần giống (để Module AI 3 phát hiện)
                full_name = "Nguyen Van Hung"
                clean_phone = "0987654321"

        rows.append((full_name, clean_phone, email, username, balance, points, tier, "BATCH_DIRTY_01"))

    return rows

def load_to_staging(rows):
    print(f"[*] Kết nối tới PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}...")
    with psycopg.connect(CONN_STRING) as conn:
        with conn.cursor() as cur:
            # Xóa batch cũ trong staging
            cur.execute("DELETE FROM staging.stg_customers WHERE source_batch = 'BATCH_DIRTY_01';")
            
            print(f"[*] Nạp {len(rows)} dòng dữ liệu bẩn vào staging.stg_customers bằng COPY...")
            with cur.copy("COPY staging.stg_customers (raw_full_name, raw_phone, raw_email, raw_username, raw_balance, raw_points, raw_tier, source_batch) FROM STDIN") as copy:
                for r in rows:
                    copy.write_row(r)
            
            conn.commit()
            print("[+] Nạp dữ liệu staging thành công!")

if __name__ == "__main__":
    records = generate_dirty_customers(500)
    load_to_staging(records)
