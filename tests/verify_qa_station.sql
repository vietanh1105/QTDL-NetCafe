-- Trách nhiệm: chạy station contract bằng role station_runtime trên netcafe_qa.
-- Liên kết: sql/08_station_customer.sql và verify_qa_station_prepare.sql.

\set ON_ERROR_STOP on

DO $$
DECLARE
    v_customer_id INT;
    v_computer_id INT;
    v_computer_code TEXT;
    v_product_id INT;
    v_session_id BIGINT;
    v_order_id BIGINT;
    v_invoice_id BIGINT;
    v_status TEXT;
    v_login RECORD;
BEGIN
    BEGIN
        PERFORM count(*) FROM core.customer;
        RAISE EXCEPTION 'station_runtime đọc trực tiếp core.customer được';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'PASS direct_customer_table=DENIED';
    END;

    SELECT * INTO v_login
    FROM core.sp_customer_login('test_station_qa', 'Test@123', 'PC001');
    v_session_id := v_login.session_id;
    v_customer_id := v_login.customer_id;
    v_computer_id := v_login.computer_id;
    RAISE NOTICE 'PASS login_session=%', v_session_id;

    SELECT session_status INTO v_status
    FROM core.sp_station_heartbeat(v_session_id, v_customer_id, v_computer_id);
    IF v_status <> 'active' THEN
        RAISE EXCEPTION 'Heartbeat không active: %', v_status;
    END IF;
    RAISE NOTICE 'PASS heartbeat_status=%', v_status;

    SELECT product_id INTO v_product_id
    FROM inventory.fn_customer_menu()
    WHERE stock_qty > 0
    ORDER BY product_id
    LIMIT 1;
    SELECT inventory.sp_customer_place_order(v_session_id, v_customer_id, v_product_id, 1)
    INTO v_order_id;
    IF NOT EXISTS (
        SELECT 1 FROM inventory.fn_customer_order_status(v_session_id, v_customer_id)
        WHERE order_id = v_order_id
    ) THEN
        RAISE EXCEPTION 'Không đọc lại được trạng thái đơn station.';
    END IF;
    RAISE NOTICE 'PASS order_status=%', v_order_id;

    PERFORM core.sp_create_service_request(
        v_session_id,
        v_customer_id,
        v_computer_id,
        'technical',
        'TEST station QA support'
    );
    RAISE NOTICE 'PASS service_request';

    SELECT core.sp_station_logout(v_session_id, v_customer_id, v_computer_id)
    INTO v_invoice_id;
    RAISE NOTICE 'PASS logout_invoice=%', v_invoice_id;
END $$;
