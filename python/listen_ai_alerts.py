#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/listen_ai_alerts.py
# Mục đích: [ĐIỂM CỘNG XUẤT SẮC] Lắng nghe tín hiệu LISTEN/NOTIFY thời gian thực
#           kết hợp với Trigger In-Database AI cảnh báo an ninh trong PostgreSQL
# ============================================================================

import os
import sys
import json
import time
from notification_stream import iter_notifications

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def load_env_file():
    env_vars = {}
    env_path = os.getenv("NETCAFE_ENV_FILE") or os.path.join(
        os.path.dirname(os.path.dirname(os.path.abspath(__file__))), ".env"
    )
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
        conn = psycopg.connect(
            host=DB_HOST, port=DB_PORT, dbname=DB_NAME,
            user=DB_USER, password=DB_PASSWORD,
            sslmode=DB_SSL_MODE.lower() if DB_SSL_MODE else "prefer",
            autocommit=True,
        )
        return conn

def start_listening(channel="ai_security_alert"):
    print("=" * 70)
    print("   NETCAFE REAL-TIME AI SECURITY ALERT MONITOR (LISTEN/NOTIFY)    ")
    print("=" * 70)
    print(f"[*] Đang kết nối tới PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}...")
    
    conn = get_db_connection()
    cur = conn.cursor()
    cur.execute(f"LISTEN {channel};")
    
    print(f"[+] ĐÃ KÍCH HOẠT LẮNG NGHE KÊNH: '{channel}'")
    print("[*] Trạng thái: Đang chờ Trigger AI phát tín hiệu NOTIFY...")
    print("    (Mẹo demo: Mở DBeaver/pgAdmin thực hiện nạp tiền > 5.000.000 VNĐ hoặc sửa lương)")
    print("    Nhấn Ctrl + C để dừng lắng nghe.\n" + "-" * 70)

    try:
        for notify in iter_notifications(conn):
            timestamp = time.strftime('%Y-%m-%d %H:%M:%S')
            print(f"\n[🚨 CẢNH BÁO AN NINH PHÁT HIỆN LÚC {timestamp}]")
            print(f"    - Kênh (Channel)     : {notify.channel}")
            print(f"    - Tiến trình phát PID: {notify.pid}")

            try:
                payload = json.loads(notify.payload)
                print(f"    - Mã cảnh báo (ID)   : #{payload.get('alert_id')}")
                print(f"    - Mức độ rủi ro      : [{payload.get('risk_level', '').upper()}] (Score: {payload.get('risk_score')}/100)")
                print(f"    - Bảng bị tác động   : {payload.get('table')}")
                print(f"    - Tác nhân thực hiện : {payload.get('actor')}")
                print(f"    - Nguyên nhân AI     : {payload.get('reason')}")
            except Exception:
                print(f"    - Dữ liệu thô        : {notify.payload}")

            print("-" * 70)
    except KeyboardInterrupt:
        print("\n[*] Đã dừng tiến trình lắng nghe.")
    finally:
        cur.close()
        conn.close()

if __name__ == "__main__":
    start_listening("ai_security_alert")

