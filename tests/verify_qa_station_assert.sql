-- Trách nhiệm: xác nhận trạng thái dữ liệu sau station login/heartbeat/order/logout.
-- Liên kết: verify_qa_station.sql; chỉ đọc trên netcafe_qa.

\set ON_ERROR_STOP on

DO $$
DECLARE
    v_status TEXT;
    v_payment NUMERIC;
BEGIN
    SELECT c.status INTO v_status
    FROM core.gaming_session s
    JOIN core.customer u ON u.customer_id = s.customer_id
    JOIN core.computer c ON c.computer_id = s.computer_id
    WHERE u.username = 'test_station_qa'
    ORDER BY s.session_id DESC
    LIMIT 1;
    IF v_status <> 'available' THEN
        RAISE EXCEPTION 'Máy station chưa về available sau logout: %', v_status;
    END IF;

    SELECT p.amount INTO v_payment
    FROM billing.payment p
    JOIN billing.invoice i ON i.invoice_id = p.invoice_id
    JOIN core.gaming_session s ON s.session_id = i.session_id
    JOIN core.customer u ON u.customer_id = s.customer_id
    WHERE u.username = 'test_station_qa'
    ORDER BY p.payment_id DESC
    LIMIT 1;
    IF v_payment IS NULL OR v_payment <= 0 THEN
        RAISE EXCEPTION 'Logout không tạo payment dương.';
    END IF;

    RAISE NOTICE 'PASS computer_after_logout=available';
    RAISE NOTICE 'PASS payment_amount=%', v_payment;
END $$;

SELECT 'PASS qa_station_contract' AS result;
