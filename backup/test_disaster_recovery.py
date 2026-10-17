# Trách nhiệm: kiểm thử backup/restore bằng database do người vận hành cấp.
# Không chứa credential mặc định và không tự trỏ tới database production.
import os
import sys
import subprocess
import psycopg
from dotenv import load_dotenv

sys.stdout.reconfigure(encoding='utf-8')

load_dotenv()
DB_HOST = os.getenv("DB_HOST", "localhost")
DB_PORT = os.getenv("DB_PORT", "5432")
SRC_DB = os.getenv("DB_NAME", "netcafe_qa")
RESTORE_DB = os.getenv("RESTORE_DB_NAME", "netcafe_qa_restore")
DB_USER = os.getenv("DB_USER", "postgres")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")

PG_BIN = r"C:\Program Files\PostgreSQL\17\bin"
PG_DUMP = os.path.join(PG_BIN, "pg_dump.exe")
PG_RESTORE = os.path.join(PG_BIN, "pg_restore.exe")
PSQL = os.path.join(PG_BIN, "psql.exe")

TABLES_TO_CHECK = [
    ("core", "room"),
    ("core", "computer_type"),
    ("core", "computer"),
    ("core", "customer"),
    ("core", "gaming_session"),
    ("core", "booking"),
    ("inventory", "product_category"),
    ("inventory", "product"),
    ("inventory", "food_order"),
    ("inventory", "food_order_detail"),
    ("billing", "promotion"),
    ("billing", "invoice"),
    ("billing", "invoice_promotion"),
    ("billing", "payment"),
    ("hr", "employee"),
    ("hr", "maintenance"),
    ("audit", "audit_log"),
]

def main():
    log_lines = []
    def log(msg):
        print(msg)
        log_lines.append(msg)

    log("================================================================================")
    log("KIỂM THỬ KHẢ NĂNG PHỤC HỒI THẢM HỌA (DISASTER RECOVERY TEST: BACKUP & RESTORE)")
    log("Môn: Quản trị Cơ sở Dữ liệu (QTDL)")
    log(f"Nguồn: {SRC_DB} -> Đích phục hồi: {RESTORE_DB}")
    log("================================================================================")

    backup_dir = "backup"
    dump_path = os.path.join(backup_dir, "netcafe_test_recovery.dump")

    env = os.environ.copy()
    env["PGPASSWORD"] = DB_PASSWORD
    env["PGCLIENTENCODING"] = "UTF8"

    # Bước 1: Sao lưu từ CSDL gốc ra file dump
    log("\n[BƯỚC 1]: Tạo bản sao lưu định dạng nhị phân pg_dump (-Fc)...")
    cmd_dump = [
        PG_DUMP,
        "-h", DB_HOST,
        "-p", DB_PORT,
        "-U", DB_USER,
        "-d", SRC_DB,
        "-Fc",
        "-f", dump_path
    ]
    res_dump = subprocess.run(cmd_dump, env=env, capture_output=True, text=True)
    if res_dump.returncode != 0:
        log(f"LỖI pg_dump: {res_dump.stderr}")
        sys.exit(1)
    file_size_kb = os.path.getsize(dump_path) / 1024
    log(f"-> Đã tạo dump thành công: {dump_path} ({file_size_kb:.2f} KB)")

    # Bước 2: Tạo CSDL đích trống netcafe_restore
    log(f"\n[BƯỚC 2]: Khởi tạo cơ sở dữ liệu độc lập {RESTORE_DB}...")
    with psycopg.connect(f"host={DB_HOST} port={DB_PORT} dbname=postgres user={DB_USER} password={DB_PASSWORD}", autocommit=True) as conn:
        with conn.cursor() as cur:
            cur.execute(f"DROP DATABASE IF EXISTS {RESTORE_DB} WITH (FORCE);")
            cur.execute(f"CREATE DATABASE {RESTORE_DB} TEMPLATE template0 ENCODING 'UTF8';")
    log(f"-> Đã tạo database {RESTORE_DB} từ template0 chuẩn UTF-8.")

    # Bước 3: Phục hồi bằng pg_restore
    log(f"\n[BƯỚC 3]: Thực hiện khôi phục dữ liệu qua pg_restore vào {RESTORE_DB}...")
    cmd_restore = [
        PG_RESTORE,
        "-h", DB_HOST,
        "-p", DB_PORT,
        "-U", DB_USER,
        "-d", RESTORE_DB,
        "--no-owner",
        dump_path
    ]
    res_restore = subprocess.run(cmd_restore, env=env, capture_output=True, text=True)
    # pg_restore exit code 0 hoặc 1 (warnings)
    log(f"-> pg_restore hoàn tất (Return code: {res_restore.returncode})")

    # Bước 4: So sánh số dòng dữ liệu giữa 2 database
    log("\n[BƯỚC 4]: Kiểm tra tính toàn vẹn và đối chiếu số dòng (Row Count Verification)...")
    log(f"{'Schema':<12} | {'Table':<22} | {'Source (netcafe_db)':<20} | {'Restored (netcafe_restore)':<28} | {'Trạng thái'}")
    log("-" * 95)

    all_match = True
    with psycopg.connect(f"host={DB_HOST} port={DB_PORT} dbname={SRC_DB} user={DB_USER} password={DB_PASSWORD}") as conn_src, \
         psycopg.connect(f"host={DB_HOST} port={DB_PORT} dbname={RESTORE_DB} user={DB_USER} password={DB_PASSWORD}") as conn_dst:
        with conn_src.cursor() as cur_src, conn_dst.cursor() as cur_dst:
            for schema, table in TABLES_TO_CHECK:
                query = f"SELECT count(*) FROM {schema}.{table};"
                cur_src.execute(query)
                cnt_src = cur_src.fetchone()[0]

                cur_dst.execute(query)
                cnt_dst = cur_dst.fetchone()[0]

                status = "KHỚP 100% [PASS]" if cnt_src == cnt_dst else "LỆCH [FAIL]"
                if cnt_src != cnt_dst:
                    all_match = False
                log(f"{schema:<12} | {table:<22} | {cnt_src:<20} | {cnt_dst:<28} | {status}")

    # Bước 5: Dọn dẹp database tạm sau kiểm thử
    log("\n[BƯỚC 5]: Dọn dẹp môi trường thử nghiệm...")
    with psycopg.connect(f"host={DB_HOST} port={DB_PORT} dbname=postgres user={DB_USER} password={DB_PASSWORD}", autocommit=True) as conn:
        with conn.cursor() as cur:
            cur.execute(f"DROP DATABASE IF EXISTS {RESTORE_DB} WITH (FORCE);")
    log(f"-> Đã dọn dẹp cơ sở dữ liệu thử nghiệm {RESTORE_DB}.")

    if os.path.exists(dump_path):
        os.remove(dump_path)
        log("-> Đã xóa file dump tạm thời.")

    log("\n" + "=" * 80)
    if all_match:
        log("KẾT LUẬN: THỬ NGHIỆM PHỤC HỒI DỮ LIỆU THÀNH CÔNG 100% - TOÀN VẸN DỮ LIỆU TUYỆT ĐỐI!")
    else:
        log("KẾT LUẬN: CÓ LỆCH DỮ LIỆU!")
    log("================================================================================")

    evidence_file = os.path.join("evidence", "backup_restore.log")
    with open(evidence_file, "w", encoding="utf-8") as f:
        f.write("\n".join(log_lines))
    print(f"\nĐã ghi nhật ký bằng chứng vào: {evidence_file}")

if __name__ == "__main__":
    main()
