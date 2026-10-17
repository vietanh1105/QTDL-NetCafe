import os
import sys
import psycopg
from dotenv import load_dotenv

# Reconfigure stdout for utf-8
sys.stdout.reconfigure(encoding='utf-8')

load_dotenv()
DB_HOST = os.getenv("DB_HOST", "localhost")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "netcafe_qa")
DB_USER = os.getenv("DB_USER", "postgres")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")

conn_info = f"host={DB_HOST} port={DB_PORT} dbname={DB_NAME} user={DB_USER} password={DB_PASSWORD}"

QUERIES = [
    {
        "id": "Q1_ACTIVE_SESSIONS",
        "title": "Truy vấn các phiên chơi đang hoạt động (Partial Index)",
        "sql": "EXPLAIN (ANALYZE, BUFFERS) SELECT session_id, customer_id, computer_id, start_time FROM core.gaming_session WHERE status = 'active';"
    },
    {
        "id": "Q2_TRIGRAM_SEARCH",
        "title": "Tìm kiếm khách hàng theo họ tên có ký tự đại diện '%Thành%' (GIN Trigram Index)",
        "sql": "EXPLAIN (ANALYZE, BUFFERS) SELECT customer_id, full_name, phone FROM core.customer WHERE full_name ILIKE '%Thành%';"
    },
    {
        "id": "Q3_JSONB_SPECS",
        "title": "Tìm máy trạm có GPU RTX 4070 trong JSONB specs (GIN jsonb_path_ops)",
        "sql": "EXPLAIN (ANALYZE, BUFFERS) SELECT computer_code, specs FROM core.computer WHERE specs @> '{\"gpu\": \"RTX 4070\"}'::jsonb;"
    },
    {
        "id": "Q4_FK_INVOICE_CUSTOMER",
        "title": "Truy vấn lịch sử hóa đơn theo customer_id (B-Tree FK Index)",
        "sql": "EXPLAIN (ANALYZE, BUFFERS) SELECT invoice_id, customer_id, total_amount, issued_at FROM billing.invoice WHERE customer_id = 5 ORDER BY issued_at DESC;"
    }
]

def run_explain(cur, query_sql):
    cur.execute(query_sql)
    rows = cur.fetchall()
    return "\n".join(r[0] for r in rows)

def main():
    print("=== ĐO LƯỜNG HIỆU NĂNG EXPLAIN ANALYZE TRƯỚC VÀ SAU KHI TẠO INDEX ===")
    
    with psycopg.connect(conn_info) as conn:
        with conn.cursor() as cur:
            # 1. Chạy trước khi tạo index
            print("1. Đang chạy EXPLAIN ANALYZE trước khi tối ưu hóa...")
            before_results = {}
            for q in QUERIES:
                before_results[q["id"]] = run_explain(cur, q["sql"])

            # 2. Đọc và thực thi sql/05_indexes_partition.sql
            print("2. Đang thực thi sql/05_indexes_partition.sql...")
            with open("sql/05_indexes_partition.sql", "r", encoding="utf-8") as f:
                index_sql = f.read()
            
            # Loại bỏ lệnh \c netcafe_db của psql
            cleaned_sql = "\n".join(line for line in index_sql.splitlines() if not line.strip().startswith("\\c"))
            cur.execute(cleaned_sql)
            conn.commit()

            # Chạy ANALYZE để cập nhật catalog statistics cho planner
            cur.execute("ANALYZE core.gaming_session; ANALYZE core.customer; ANALYZE core.computer; ANALYZE billing.invoice;")
            conn.commit()

            # 3. Chạy sau khi tạo index
            print("3. Đang chạy EXPLAIN ANALYZE sau khi tối ưu hóa...")
            after_results = {}
            index_forced_results = {}
            for q in QUERIES:
                after_results[q["id"]] = run_explain(cur, q["sql"])
                
                # Chạy với enable_seqscan = off để minh họa cấu trúc Bitmap Index Scan thực sự
                cur.execute("SET enable_seqscan = off;")
                index_forced_results[q["id"]] = run_explain(cur, q["sql"])
                cur.execute("SET enable_seqscan = on;")

    # 4. Ghi kết quả so sánh vào evidence/explain_before_after.txt
    print("4. Đang xuất báo cáo ra evidence/explain_before_after.txt...")
    report_lines = []
    report_lines.append("================================================================================")
    report_lines.append("BÁO CÁO ĐO LƯỜNG VÀ TỐI ƯU HÓA HIỆU NĂNG CSDL (EXPLAIN ANALYZE BEFORE & AFTER)")
    report_lines.append("Môn: Quản trị Cơ sở Dữ liệu (QTDL)")
    report_lines.append("Hệ thống Quản lý Phòng Game/Net (netcafe_db) - PostgreSQL 17")
    report_lines.append("Môi trường: Đo lường thực tế trên máy chủ PostgreSQL 17 cục bộ")
    report_lines.append("================================================================================\n")

    for q in QUERIES:
        qid = q["id"]
        report_lines.append("-" * 80)
        report_lines.append(f"MỤC TIÊU: {q['title']}")
        report_lines.append(f"CÂU LỆNH: {q['sql'].replace('EXPLAIN (ANALYZE, BUFFERS) ', '')}")
        report_lines.append("-" * 80)
        report_lines.append("[1. TRƯỚC KHI TẠO INDEX (BEFORE - CHƯA TỐI ƯU)]:")
        report_lines.append(before_results[qid])
        report_lines.append("\n[2. SAU KHI TẠO INDEX (AFTER - CHIẾN LƯỢC TỐI ƯU CỦA PLANNER)]:")
        report_lines.append(after_results[qid])
        report_lines.append("\n[3. MINH HỌA CÂY THỰC THI TRUY VẤN INDEX (INDEX SCAN PLAN)]:")
        report_lines.append(index_forced_results[qid])
        report_lines.append("\n" + "=" * 80 + "\n")

    output_path = os.path.join("evidence", "explain_before_after.txt")
    os.makedirs("evidence", exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(report_lines))

    print(f"XONG! Đã lưu kết quả đo lường vào {output_path}")

if __name__ == "__main__":
    main()
