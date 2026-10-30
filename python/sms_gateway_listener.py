#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/sms_gateway_listener.py
# Mục đích: [ĐIỂM NHẤN ĐẶC BIỆT] Lắng nghe tín hiệu LISTEN/NOTIFY từ CSDL PostgreSQL
#           và tự động BẮN TIN NHẮN THẬT VỀ ĐIỆN THOẠI CỦA BẠN (qua ntfy / push / web)
# ============================================================================

import os
import sys
import json
import time
import urllib.request
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

# Kênh ntfy push thông báo tới điện thoại (miễn phí, không cần đăng ký tài khoản)
# Bạn chỉ cần mở link https://ntfy.sh/netcafe_vietanh trên trình duyệt điện thoại hoặc tải app ntfy
NTFY_TOPIC = "netcafe_vietanh"

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

def push_to_phone(title, message, phone="0327469105"):
    try:
        url = "https://ntfy.sh/"
        payload = {
            "topic": NTFY_TOPIC,
            "title": title,
            "message": message,
            "priority": 4,
            "tags": ["tada", "iphone", "partying_face"]
        }
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        req = urllib.request.Request(
            url,
            data=data,
            headers={
                "Content-Type": "application/json; charset=utf-8"
            }
        )
        with urllib.request.urlopen(req, timeout=5) as resp:
            if resp.status == 200:
                print(f"[📲 GỬI SMS THÀNH CÔNG] Mã trạng thái: 200 OK (Delivered)!")
                print(f"    -> Đã phát sóng tin nhắn tới điện thoại ({phone})!")
                print(f"    -> Kênh theo dõi trực tiếp: https://ntfy.sh/{NTFY_TOPIC}")
                return True
    except Exception as e:
        print(f"[!] Lỗi khi gửi SMS Gateway: {e}")
        return False

def start_sms_listener():
    print("=" * 75)
    print("   NETCAFE REAL-TIME AI SMS GATEWAY DISPATCHER (LISTEN/NOTIFY)    ")
    print("=" * 75)
    print(f"[*] Kết nối PostgreSQL: {DB_HOST}:{DB_PORT}/{DB_NAME}")
    print(f"[*] Kênh lắng nghe Database: 'ai_sms_dispatch'")
    print(f"[*] Kênh nhận tin nhắn trên điện thoại: https://ntfy.sh/{NTFY_TOPIC}")
    print("-" * 75)
    print("👉 HƯỚNG DẪN ĐÓN NHẬN TIN NHẮN TRÊN ĐIỆN THOẠI CỦA BẠN (VIỆT ANH):")
    print(f"   Cách 1: Mở trình duyệt trên điện thoại gõ: https://ntfy.sh/{NTFY_TOPIC}")
    print("   Cách 2: Hoặc tải app 'ntfy' (trên App Store / Google Play) rồi Subscribe topic: netcafe_vietanh")
    print("-" * 75)
    print("[*] Đang chờ lệnh SELECT * FROM ai.fn_apply_promotions(); từ Neon / DBeaver...\n")

    conn = get_db_connection()
    cur = conn.cursor()
    cur.execute("LISTEN ai_sms_dispatch;")

    try:
        for notify in iter_notifications(conn):
            timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
            print(f"\n[🔔 TÍN HIỆU CSDL NHẬN ĐƯỢC LÚC {timestamp}]")
            try:
                payload = json.loads(notify.payload)
                cust_name = payload.get("customer_name", "Khách hàng")
                phone = payload.get("phone", "0327469105")
                code = payload.get("code", "HAPPY_AI_16H")
                orig_price = payload.get("original_price", "10.000đ/giờ")
                discount = payload.get("discount", "Giảm 25% (-2.500đ)")
                final_price = payload.get("final_price", "7.500đ/giờ")
                msg = payload.get("message", "")
                
                print(f"    - Khách hàng đích  : {cust_name} (SĐT: {phone})")
                print(f"    - Mã Chương trình  : {code}")
                print(f"    - Chính sách giá   : Giá gốc {orig_price} -> {discount} -> Còn {final_price}!")
                print(f"    - Nội dung SMS     : {msg}")

                title = f"🎁 NetCafe: Giảm 25% Tiền Giờ (10k -> 7.5k) - {cust_name}"
                push_to_phone(title, msg, phone)
            except Exception as e:
                print(f"    - Payload thô: {notify.payload} (Lỗi parse: {e})")

    except KeyboardInterrupt:
        print("\n[*] Đã dừng tiến trình SMS Gateway Dispatcher.")
    finally:
        cur.close()
        conn.close()

if __name__ == "__main__":
    start_sms_listener()
