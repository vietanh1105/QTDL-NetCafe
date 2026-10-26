#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# Môn: Quản trị Cơ sở Dữ liệu (QTDL)
# File: python/test_all_ai_modules.py
# Mục đích: Chạy thử nghiệm và hiển thị kết quả trực quan của toàn bộ 5 Module AI
# ============================================================================

import os
import sys

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

def print_separator(title):
    print("\n" + "=" * 80)
    print(f"   {title}")
    print("=" * 80)

def run_all_ai_modules():
    conn = get_db_connection()
    conn.autocommit = True
    cur = conn.cursor()

    print("*" * 80)
    print("      TIẾN TRÌNH THỰC THI & KIỂM TRA 5 MODULE IN-DATABASE AI POSTGRESQL       ")
    print(f"[*] Kết nối CSDL: {DB_HOST}:{DB_PORT}/{DB_NAME}")
    print("*" * 80)

    # -------------------------------------------------------------------------
    # MODULE 1: AI RISK SCORING & ALERT
    # -------------------------------------------------------------------------
    print_separator("MODULE 1: CẢNH BÁO AN NINH & CHẤM ĐIỂM RỦI RO (AI RISK SCORING)")
    print("[*] Cơ chế: Trigger tự động chấm điểm Risk Score khi có biến động tài chính lớn.")
    print("[*] Lệnh SQL thực thi: SELECT alert_id, risk_score, risk_level, reason FROM ai.ai_alert ORDER BY alert_id DESC LIMIT 4;")
    
    cur.execute("SELECT alert_id, risk_score, risk_level, reason FROM ai.ai_alert ORDER BY alert_id DESC LIMIT 4;")
    alerts = cur.fetchall()
    print(f"[+] Tìm thấy {len(alerts)} cảnh báo an ninh đã được AI ghi nhận:")
    for a in alerts:
        print(f"    - Cảnh báo #{a[0]} | Điểm: {a[1]}/100 | Mức độ: [{str(a[2]).upper()}] | Lý do: {a[3]}")

    # -------------------------------------------------------------------------
    # MODULE 2: INDEX RECOMMENDATION ENGINE
    # -------------------------------------------------------------------------
    print_separator("MODULE 2: TỰ ĐỘNG KHUYẾN NGHỊ CHỈ MỤC (INDEX RECOMMENDATION ENGINE)")
    print("[*] Cơ chế: Quét pg_stat_user_tables & Foreign Keys để phát hiện bảng chậm.")
    print("[*] Lệnh SQL thực thi: SELECT recommendation_id, table_name, column_list, suggested_sql FROM ai.fn_recommend_indexes() LIMIT 3;")
    
    cur.execute("SELECT recommendation_id, table_name, column_list, suggested_sql FROM ai.fn_recommend_indexes() LIMIT 3;")
    indexes = cur.fetchall()
    print(f"[+] AI đề xuất các câu lệnh tạo chỉ mục để tăng tốc độ truy vấn:")
    for idx in indexes:
        print(f"    - Khuyến nghị #{idx[0]} trên bảng [{idx[1]}] (cột '{idx[2]}'):")
        print(f"      -> Cú pháp tối ưu: {idx[3]}")

    # -------------------------------------------------------------------------
    # MODULE 3: DUPLICATE CUSTOMER DETECTION
    # -------------------------------------------------------------------------
    print_separator("MODULE 3: PHÁT HIỆN TÀI KHOẢN TRÙNG LẶP (DUPLICATE DETECTION VIA PG_TRGM)")
    print("[*] Cơ chế: Thuật toán Trigram + unaccent so khớp họ tên tiếng Việt & SĐT.")
    print("[*] Lệnh SQL thực thi: SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason FROM ai.fn_detect_duplicates(0.65);")
    
    cur.execute("SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason FROM ai.fn_detect_duplicates(0.65);")
    dups = cur.fetchall()
    print(f"[+] Tìm thấy {len(dups)} cặp khách hàng nghi vấn trùng lặp (chống gian lận):")
    for d in dups:
        print(f"    - Cặp #{d[0]}: '{d[1]}' vs '{d[2]}' | Độ tương đồng: {float(d[3]):.1%}")
        print(f"      -> Nguyên nhân AI: {d[4]}")

    # -------------------------------------------------------------------------
    # MODULE 4: STATISTICAL ANOMALY DETECTION (Z-SCORE)
    # -------------------------------------------------------------------------
    print_separator("MODULE 4: PHÁT HIỆN BẤT THƯỜNG THỐNG KÊ (ANOMALY DETECTION VIA Z-SCORE)")
    print("[*] Cơ chế: Công thức Z = |X - Mean| / StdDev để nhận diện phiên chơi dị biệt.")
    print("[*] Lệnh SQL thực thi: SELECT flag_id, anomaly_type, subject_table, metric_value, z_score FROM ai.fn_detect_anomalies(1.5);")
    
    cur.execute("SELECT flag_id, anomaly_type, subject_table, metric_value, z_score FROM ai.fn_detect_anomalies(1.5);")
    anoms = cur.fetchall()
    print(f"[+] Tìm thấy {len(anoms)} bản ghi ngoại lai vượt ngưỡng phân phối chuẩn:")
    for an in anoms:
        unit = "phút" if "SESSION" in an[1] else "VNĐ"
        print(f"    - Cờ #{an[0]} [{an[1]}] trên bảng '{an[2]}':")
        print(f"      -> Giá trị đo được: {float(an[3]):,.0f} {unit} | Điểm Z-Score: {float(an[4])} (Vượt ngưỡng rủi ro)")

    # -------------------------------------------------------------------------
    # MODULE 5: SMART PROMOTION SUGGESTION
    # -------------------------------------------------------------------------
    print_separator("MODULE 5: GỢI Ý CHƯƠNG TRÌNH KHUYẾN MÃI THÔNG MINH (SMART PROMOTION)")
    print("[*] Cơ chế: Khai phá Materialized Views tìm giờ thấp điểm & phân nhóm VIP Churn.")
    print("[*] Lệnh SQL thực thi: SELECT sug_id, suggestion_type, target, details FROM ai.promotion_suggestion;")
    
    cur.execute("SELECT sug_id, suggestion_type, target, details FROM ai.promotion_suggestion;")
    promos = cur.fetchall()
    print(f"[+] Tìm thấy {len(promos)} đề xuất chiến lược kinh doanh thông minh:")
    for p in promos:
        details = p[3] if isinstance(p[3], dict) else {}
        action = details.get('recommended_action', '')
        rationale = details.get('rationale', '')
        print(f"    - Đề xuất #{p[0]} [{p[1]}] -> Mục tiêu: {p[2]}")
        print(f"      -> Hành động gợi ý: {action}")
        print(f"      -> Phân tích logic : {rationale}")

    print("\n" + "*" * 80)
    print("      HOÀN TẤT KIỂM TRA TOÀN DIỆN 5/5 MODULE AI! HỆ THỐNG SẴN SÀNG DEMO      ")
    print("*" * 80)

    cur.close()
    conn.close()

if __name__ == "__main__":
    run_all_ai_modules()
