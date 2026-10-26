#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/etl.py
# Mục đích: Luồng ETL tự động hóa toàn diện:
#           Extract -> Sinh dữ liệu bẩn -> Transform/Clean -> Bulk Load (COPY)
#           -> Sinh các phiên chơi thực tế có phân bố cao điểm + Cài đặt bất thường có chủ đích
# ============================================================================

import os
import sys
import random
from datetime import datetime, timedelta
import psycopg
from dotenv import load_dotenv

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

from generate_dirty_data import generate_dirty_customers, load_to_staging
from clean_data import clean_and_load

load_dotenv()

DB_HOST = os.getenv("DB_HOST", "127.0.0.1")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "netcafe_qa")
DB_USER = os.getenv("DB_USER", "postgres")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")

CONN_STRING = f"host={DB_HOST} port={DB_PORT} dbname={DB_NAME} user={DB_USER} password={DB_PASSWORD}"

def seed_realistic_activity():
    """Sinh các phiên chơi thực tế, đơn gọi món và cài đặt các tình huống bất thường có chủ đích"""
    print("[*] Sinh dữ liệu phiên chơi, đơn F&B và tình huống bất thường có chủ đích...")
    random.seed(42)

    with psycopg.connect(CONN_STRING) as conn:
        with conn.cursor() as cur:
            # 1. Lấy danh sách khách hàng và máy
            cur.execute("SELECT customer_id FROM core.customer ORDER BY customer_id;")
            cust_ids = [r[0] for r in cur.fetchall()]

            cur.execute("SELECT computer_id, type_id FROM core.computer ORDER BY computer_id;")
            comps = cur.fetchall()

            cur.execute("SELECT employee_id FROM hr.employee WHERE emp_role IN ('admin', 'manager', 'cashier');")
            emp_ids = [r[0] for r in cur.fetchall()]

            cur.execute("SELECT type_id, hourly_rate FROM core.computer_type;")
            rates = {r[0]: r[1] for r in cur.fetchall()}

            # Sinh ~200 phiên chơi lịch sử trong 30 ngày qua
            now = datetime.now()
            session_rows = []
            invoice_rows = []

            for i in range(1, 201):
                c_id = random.choice(cust_ids)
                comp_id, t_id = random.choice(comps)
                opened_by = random.choice(emp_ids)
                hourly_rate = rates[t_id]

                # Phân bố thời gian: tập trung vào 18h - 23h (giờ cao điểm)
                day_offset = random.randint(1, 25)
                hour = random.choices(
                    population=list(range(24)),
                    weights=[1, 1, 1, 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 7, 8, 10, 14, 18, 22, 20, 15, 8, 4]
                )[0]
                start_dt = now - timedelta(days=day_offset, hours=now.hour - hour, minutes=random.randint(0, 59))
                duration_hours = random.choice([0.5, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0])
                end_dt = start_dt + timedelta(hours=duration_hours)
                play_amt = round(duration_hours * float(hourly_rate))

                session_rows.append((
                    c_id, comp_id, opened_by, start_dt, end_dt, 'finished', hourly_rate, play_amt, '{"game":"League of Legends"}'
                ))

            # Nạp các phiên chơi bằng COPY
            with cur.copy("""
                COPY core.gaming_session (
                    customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied, play_amount, game_info
                ) FROM STDIN
            """) as copy:
                for s in session_rows:
                    copy.write_row(s)

            # Lấy các session vừa tạo để sinh hóa đơn tương ứng
            cur.execute("SELECT session_id, customer_id, opened_by, play_amount, end_dt FROM (SELECT session_id, customer_id, opened_by, play_amount, end_time as end_dt FROM core.gaming_session WHERE play_amount IS NOT NULL ORDER BY session_id DESC LIMIT 200) t;")
            inserted_sessions = cur.fetchall()

            invoice_data = []
            payment_data = []
            for s_id, cust_id, emp_id, play_amt, end_dt in inserted_sessions:
                tot_amt = play_amt
                invoice_data.append((
                    s_id, cust_id, emp_id, play_amt, 0, 0, 0, tot_amt, 'paid', end_dt, end_dt
                ))

            with cur.copy("""
                COPY billing.invoice (
                    session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount, status, issued_at, paid_at
                ) FROM STDIN
            """) as copy:
                for inv in invoice_data:
                    copy.write_row(inv)

            # Lấy invoice_ids để sinh payment tương ứng
            cur.execute("SELECT invoice_id, customer_id, cashier_id, total_amount, paid_at FROM billing.invoice ORDER BY invoice_id DESC LIMIT 200;")
            inserted_invoices = cur.fetchall()
            for inv_id, cust_id, emp_id, tot_amt, paid_at in inserted_invoices:
                payment_data.append((
                    'invoice', inv_id, cust_id, 'balance', tot_amt, emp_id, paid_at, f"Thanh toán phiên"
                ))

            with cur.copy("""
                COPY billing.payment (
                    kind, invoice_id, customer_id, method, amount, employee_id, paid_at, reference
                ) FROM STDIN
            """) as copy:
                for p in payment_data:
                    copy.write_row(p)

            # ----------------------------------------------------------------
            # CÀI ĐẶT CÁC TÌNH HUỐNG BẤT THƯỜNG CÓ CHỦ ĐÍCH CHO 5 MODULE AI
            # ----------------------------------------------------------------
            print("[*] Cài đặt 4 tình huống bất thường có chủ đích cho các module AI...")

            # 1. Tình huống bất thường 1: Phiên chơi quên đóng > 35 tiếng (cho Module 4)
            cur.execute("""
                INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied, play_amount)
                VALUES (%s, %s, %s, now() - INTERVAL '38 hours', now() - INTERVAL '2 hours', 'finished', 12000, 432000);
            """, (cust_ids[0], comps[0][0], emp_ids[0]))

            # 2. Tình huống bất thường 2: Giao dịch nạp tiền lớn đột biến 15.000.000 VNĐ (cho Module 4 Z-score)
            cur.execute("""
                INSERT INTO billing.payment (kind, invoice_id, customer_id, method, amount, employee_id, paid_at, reference)
                VALUES ('topup', NULL, %s, 'bank_transfer', 15000000, %s, now() - INTERVAL '1 day', 'Nạp tiền bất thường lớn');
            """, (cust_ids[1], emp_ids[0]))

            # 3. Tình huống bất thường 3: Khách hàng trùng tên và gần giống số điện thoại (cho Module 3 pg_trgm)
            cur.execute("""
                INSERT INTO core.customer (full_name, phone, username, password_hash, balance)
                VALUES ('Tran Van Binh', '0912345679', 'binh_tran_dup', crypt(gen_random_uuid()::text, gen_salt('bf')), 50000)
                ON CONFLICT (phone) DO NOTHING;
            """)

            # Làm mới Materialized Views
            cur.execute("REFRESH MATERIALIZED VIEW reporting.mv_monthly_revenue;")
            cur.execute("REFRESH MATERIALIZED VIEW reporting.mv_top_customers;")
            cur.execute("REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;")

            conn.commit()
            print("[+] Hoàn tất sinh hoạt động thực tế và cài đặt tình huống AI!")

def run_pipeline():
    print("============================================================")
    print("BẮT ĐẦU LUỒNG ETL TỰ ĐỘNG (PIPELINE)")
    print("============================================================")
    # Bước 0: Reset dữ liệu về seed ban đầu để pipeline luôn chạy lại an toàn
    print("[*] Khởi tạo dữ liệu cơ sở từ sql/06_seed_base.sql...")
    with psycopg.connect(CONN_STRING) as conn:
        with conn.cursor() as cur:
            with open("sql/06_seed_base.sql", "r", encoding="utf-8") as f:
                seed_sql = f.read()
            cleaned_sql = "\n".join(l for l in seed_sql.splitlines() if not l.strip().startswith("\\c"))
            cur.execute(cleaned_sql)
            conn.commit()

    # Bước 1: Sinh 500 dòng bẩn nạp vào staging
    dirty_data = generate_dirty_customers(500)
    load_to_staging(dirty_data)

    # Bước 2: Chuẩn hóa, lọc dữ liệu bẩn và load vào core.customer
    clean_and_load()

    # Bước 3: Sinh dữ liệu hoạt động phòng net thực tế
    seed_realistic_activity()
    print("============================================================")
    print("LUỒNG ETL HOÀN TẤT THÀNH CÔNG 100%!")
    print("============================================================")

if __name__ == "__main__":
    run_pipeline()
