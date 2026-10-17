# Trách nhiệm: áp dụng các migration vào database do người vận hành cấu hình.
# Không chứa host, role hoặc credential thật; production/Neon phải được cấp ngoài source.
import os
import sys
try:
    import psycopg
except ImportError:
    import psycopg2 as psycopg

sys.stdout.reconfigure(encoding='utf-8')

# Đọc cấu hình từ .env
HOST = os.getenv("DB_HOST", "localhost")
PORT = int(os.getenv("DB_PORT", "5432"))
DBNAME = os.getenv("DB_NAME", "netcafe_qa")
USER = os.getenv("DB_USER", "postgres")
PASS = os.getenv("DB_PASSWORD", "")
SSL_MODE = os.getenv("DB_SSL_MODE", "disable")

# Đọc từ file .env nếu có
env_file = os.path.join(os.path.dirname(__file__), "..", ".env")
if os.path.exists(env_file):
    with open(env_file, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split("=", 1)
            if len(parts) == 2:
                k, v = parts[0].strip(), parts[1].strip()
                if k == "DB_HOST": HOST = v
                elif k == "DB_PORT": PORT = int(v)
                elif k == "DB_NAME": DBNAME = v
                elif k == "DB_USER": USER = v
                elif k == "DB_PASSWORD": PASS = v
                elif k == "DB_SSL_MODE": SSL_MODE = v

SQL_FILES = [
    "sql/08_station_customer.sql",
]

def clean_sql(content: str) -> str:
    lines = []
    for line in content.splitlines():
        trimmed = line.strip()
        if trimmed.startswith("\\c ") or trimmed == "\\c":
            continue
        lines.append(line)
    return "\n".join(lines)

def main():
    if DBNAME != "netcafe_db":
        raise RuntimeError("Script này chỉ áp dụng migration 08 cho netcafe_db; dùng scripts/run-qa.ps1 cho QA.")
    if os.getenv("NETCAFE_MAIN_MIGRATION") != "08_only":
        raise RuntimeError("Đặt NETCAFE_MAIN_MIGRATION=08_only để xác nhận chỉ chạy migration additive 08.")
    print(f"Connecting to Database {DBNAME} at {HOST}:{PORT} (user: {USER})...")
    conn_str = f"postgresql://{USER}:{PASS}@{HOST}:{PORT}/{DBNAME}?sslmode={SSL_MODE.lower()}"
    if getattr(psycopg, "__name__", "") == "psycopg2":
        conn = psycopg.connect(conn_str)
        conn.autocommit = True
    else:
        conn = psycopg.connect(conn_str, autocommit=True)
    with conn:
        with conn.cursor() as cur:
            cur.execute("SELECT version();")
            print("Connected! Version:", cur.fetchone()[0])
            
            for fpath in SQL_FILES:
                abs_fpath = os.path.join(os.path.dirname(__file__), "..", fpath)
                print(f"--- Running {fpath} ---")
                if not os.path.exists(abs_fpath):
                    print(f"Error: {fpath} not found!")
                    return
                with open(abs_fpath, "r", encoding="utf-8") as f:
                    content = f.read()
                sql = clean_sql(content)
                try:
                    cur.execute(sql)
                    print(f"--> Successfully executed {fpath}!")
                except Exception as e:
                    print(f"--> FAILED on {fpath}: {e}")
                    raise e

    print("=== ALL SQL FILES APPLIED SUCCESSFULLY! ===")

if __name__ == "__main__":
    main()
