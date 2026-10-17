# Trách nhiệm: kiểm thử station trên netcafe_db bằng fixture test_ giữ lại.
# Không reset/truncate dữ liệu; credential chỉ đọc từ .env hoặc environment.

from __future__ import annotations

import os
from pathlib import Path

import psycopg2


def load_env() -> dict[str, str]:
    values: dict[str, str] = {}
    env_path = Path(os.environ.get("NETCAFE_ENV_FILE", ".env"))
    for line in env_path.read_text(encoding="utf-8").splitlines():
        text = line.strip()
        if not text or text.startswith("#") or "=" not in text:
            continue
        key, value = text.split("=", 1)
        values[key.strip()] = value.strip().strip('"').strip("'")
    for key in ("DB_HOST", "DB_PORT", "DB_NAME", "DB_USER", "DB_PASSWORD", "DB_SSL_MODE", "STATION_DB_USER", "STATION_DB_PASSWORD"):
        if os.environ.get(key):
            values[key] = os.environ[key]
    return values


def connect(values: dict[str, str], user_key: str, password_key: str):
    return psycopg2.connect(
        host=values["DB_HOST"],
        port=values.get("DB_PORT", "5432"),
        dbname=values["DB_NAME"],
        user=values[user_key],
        password=values[password_key],
        sslmode=values.get("DB_SSL_MODE", "require").lower(),
    )


def main() -> None:
    values = load_env()
    if values.get("DB_NAME") != "netcafe_db":
        raise RuntimeError("Test main phải chạy đúng netcafe_db.")
    for key in ("DB_HOST", "DB_USER", "DB_PASSWORD", "STATION_DB_USER", "STATION_DB_PASSWORD"):
        if not values.get(key):
            raise RuntimeError(f"Thiếu biến {key}; không ghi credential vào source.")

    username = "test_station_20261017"
    phone = "0930000000"
    email = "test_station_20261017@example.com"
    password = "Test@123"
    session_id: int | None = None
    invoice_id: int | None = None

    owner = connect(values, "DB_USER", "DB_PASSWORD")
    owner.autocommit = True
    try:
        with owner.cursor() as cursor:
            cursor.execute("SELECT employee_id FROM hr.employee WHERE is_active = TRUE ORDER BY employee_id LIMIT 1")
            service_employee_id = cursor.fetchone()[0]
            cursor.execute("SELECT customer_id FROM core.customer WHERE username = %s", (username,))
            existing = cursor.fetchone()
            if existing:
                customer_id = existing[0]
                cursor.execute(
                    "UPDATE core.customer SET balance = GREATEST(balance, 80000), is_active = TRUE, password_hash = crypt(%s, gen_salt('bf')) WHERE customer_id = %s",
                    (password, customer_id),
                )
            else:
                cursor.execute(
                    "INSERT INTO core.customer(full_name, phone, email, username, password_hash, balance) VALUES (%s, %s, %s, %s, crypt(%s, gen_salt('bf')), 80000) RETURNING customer_id",
                    ("TEST Station Customer", phone, email, username, password),
                )
                customer_id = cursor.fetchone()[0]
            cursor.execute("SELECT status FROM core.computer WHERE computer_code = %s", ("PC001",))
            if cursor.fetchone()[0] != "available":
                raise RuntimeError("PC001 không available; dừng test để không chiếm máy đang dùng.")

        station = connect(values, "STATION_DB_USER", "STATION_DB_PASSWORD")
        station.autocommit = True
        try:
            with station.cursor() as cursor:
                cursor.execute("SELECT current_user")
                print("PASS station_user=", cursor.fetchone()[0])
                cursor.execute("SELECT count(*) FROM inventory.fn_customer_menu()")
                print("PASS menu_count=", cursor.fetchone()[0])
                try:
                    cursor.execute("SELECT count(*) FROM core.customer")
                    raise AssertionError("station đọc trực tiếp core.customer được")
                except psycopg2.Error:
                    station.rollback()
                    print("PASS direct_customer_table=DENIED")

                cursor.execute("SELECT * FROM core.sp_customer_login(%s, %s, %s)", (username, password, "PC001"))
                login = cursor.fetchone()
                session_id = login[0]
                print("PASS login_session=", session_id)
                cursor.execute("SELECT * FROM core.sp_station_heartbeat(%s, %s, %s)", (login[0], login[1], login[2]))
                heartbeat = cursor.fetchone()
                if heartbeat[6] != "active":
                    raise AssertionError(f"heartbeat status không active: {heartbeat[6]}")
                print("PASS heartbeat_status=", heartbeat[6])
                cursor.execute("SELECT product_id FROM inventory.fn_customer_menu() WHERE stock_qty > 0 ORDER BY product_id LIMIT 1")
                product_id = cursor.fetchone()[0]
                cursor.execute("SELECT inventory.sp_customer_place_order(%s, %s, %s, %s)", (login[0], login[1], product_id, 1))
                print("PASS order_id=", cursor.fetchone()[0])
                cursor.execute("SELECT core.sp_create_service_request(%s, %s, %s, %s, %s)", (login[0], login[1], login[2], "technical", "TEST station support"))
                print("PASS service_request_id=", cursor.fetchone()[0])
                cursor.execute("SELECT core.sp_station_logout(%s, %s, %s)", (login[0], login[1], login[2]))
                invoice_id = cursor.fetchone()[0]
                print("PASS logout_invoice=", invoice_id)
        finally:
            if session_id is not None and invoice_id is None:
                try:
                    with station.cursor() as cursor:
                        cursor.execute("SELECT core.sp_station_logout(%s, %s, %s)", (session_id, customer_id, 1))
                except Exception:
                    station.rollback()
            station.close()

        with owner.cursor() as cursor:
            cursor.execute("SELECT status FROM core.computer WHERE computer_code = %s", ("PC001",))
            if cursor.fetchone()[0] != "available":
                raise AssertionError("PC001 chưa trả về available sau logout")
            cursor.execute("SELECT balance FROM core.customer WHERE customer_id = %s", (customer_id,))
            if cursor.fetchone()[0] < 0:
                raise AssertionError("balance âm sau logout")
            cursor.execute("SELECT amount FROM billing.payment WHERE invoice_id = %s", (invoice_id,))
            payment_amount = cursor.fetchone()[0]
            if payment_amount <= 0:
                raise AssertionError("payment bằng 0 hoặc không tồn tại")
            print("PASS pc001_after=available")
            print("PASS payment_amount=", payment_amount)
            print("FIXTURE_RETAINED username=", username, "customer_id=", customer_id, "service_employee_id=", service_employee_id)
    finally:
        owner.close()


if __name__ == "__main__":
    main()
