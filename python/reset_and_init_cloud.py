#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/reset_and_init_cloud.py
# Mục đích:
#   1. Xóa sạch CSDL trên Cloud (Wipe clean về 0 bảng) để chứng minh trạng thái ban đầu
#   2. Khởi tạo toàn bộ Database Schemas, Tables, Triggers, Views, RLS, Indexes, Seed Data
# ============================================================================

import os
import sys
import time

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
        conn = psycopg2.connect(
            host=DB_HOST,
            port=DB_PORT,
            dbname=DB_NAME,
            user=DB_USER,
            password=DB_PASSWORD,
            sslmode=DB_SSL_MODE.lower() if DB_SSL_MODE else "prefer"
        )
        conn.autocommit = True
        return conn
    except ImportError:
        import psycopg
        return psycopg.connect(
            host=DB_HOST, port=DB_PORT, dbname=DB_NAME,
            user=DB_USER, password=DB_PASSWORD,
            sslmode=DB_SSL_MODE.lower() if DB_SSL_MODE else "prefer",
            autocommit=True,
        )

def wipe_cloud():
    print("=" * 70)
    print("   TIẾN TRÌNH XÓA SẠCH TOÀN BỘ CSDL TRÊN CLOUD (WIPE CLEAN DB)   ")
    print("=" * 70)
    print(f"[*] Đang kết nối tới PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}...")
    conn = get_db_connection()
    try:
        with conn.cursor() as cur:
            print("[*] Đang DROP 8 schemas: core, inventory, billing, hr, audit, ai, reporting, staging...")
            cur.execute("DROP SCHEMA IF EXISTS core, inventory, billing, hr, audit, ai, reporting, staging CASCADE;")
            
            cur.execute("""
                SELECT count(table_name) 
                FROM information_schema.tables 
                WHERE table_schema IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging');
            """)
            count = cur.fetchone()[0]
            print(f"[+] XÓA THÀNH CÔNG! Số bảng còn lại trong 8 schemas: {count} (HOÀN TOÀN TRỐNG TRƠN)")
    finally:
        conn.close()

def init_cloud():
    print("=" * 70)
    print("   TIẾN TRÌNH KHỞI TẠO CSDL VÀ NẠP DỮ LIỆU MỒI (CLOUD INIT & SEED)   ")
    print("=" * 70)
    print(f"[*] Đang kết nối tới PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}...")
    print("[!] INIT tái tạo toàn bộ 8 schema và dữ liệu demo trong database đang chọn.")
    conn = get_db_connection()
    start_time = time.time()
    try:
        with conn.cursor() as cur:
            abs_path = os.path.join(
                os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                "sql", "netcafe_cloud_init.sql"
            )
            # File tổng hợp có BEGIN/COMMIT; lỗi khởi tạo không để lại database nửa chừng.
            with open(abs_path, "r", encoding="utf-8") as sql_file:
                cur.execute(sql_file.read())

            # Đếm số bảng và schemas đã tạo
            cur.execute("""
                SELECT n.nspname,
                       count(*) FILTER (WHERE c.relkind IN ('r','p') AND NOT c.relispartition)
                FROM pg_namespace n LEFT JOIN pg_class c ON c.relnamespace = n.oid
                WHERE n.nspname IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging')
                GROUP BY n.nspname
                ORDER BY n.nspname;
            """)
            stats = cur.fetchall()
            total_tables = sum(r[1] for r in stats)
            
            elapsed = time.time() - start_time
            print("=" * 70)
            print(f"[+] KHỞI TẠO THÀNH CÔNG TOÀN BỘ CSDL TRONG {elapsed:.2f} GIÂY!")
            print(f"[*] Schemas: {len(stats)} | Bảng logic (không tính partition/view): {total_tables}")
            for schema, count in stats:
                print(f"    - Schema '{schema}': {count} bảng")
            print("=" * 70)
    finally:
        conn.close()

if __name__ == "__main__":
    if len(sys.argv) > 1:
        arg = sys.argv[1].lower()
        if arg in ["--wipe", "-w", "wipe"]:
            wipe_cloud()
        elif arg in ["--init", "-i", "init"]:
            init_cloud()
        elif arg in ["--reset", "-r", "reset"]:
            wipe_cloud()
            print()
            init_cloud()
        else:
            print("Cách dùng: python python/reset_and_init_cloud.py [--wipe | --init | --reset]")
    else:
        print("\nCHỌN THAO TÁC CSDL TRÊN CLOUD:")
        print("  1. Xóa sạch CSDL trên Cloud (Wipe clean về 0 bảng)")
        print("  2. Khởi tạo toàn bộ CSDL và nạp dữ liệu mồi (Init & Seed)")
        print("  3. Reset toàn bộ (Xóa sạch rồi Khởi tạo lại ngay lập tức)")
        choice = input("Nhập lựa chọn (1/2/3) hoặc phím bất kỳ để thoát: ").strip()
        if choice == "1":
            wipe_cloud()
        elif choice == "2":
            init_cloud()
        elif choice == "3":
            wipe_cloud()
            print()
            init_cloud()
        else:
            print("Hủy thao tác.")

