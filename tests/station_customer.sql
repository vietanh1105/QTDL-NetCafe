-- Trách nhiệm: kiểm tra contract station trên database QA sau reset.
-- Script chỉ đọc metadata/quyền; không DROP, TRUNCATE hoặc sửa dữ liệu.

\set ON_ERROR_STOP on

DO $$
DECLARE
    v_database TEXT;
    v_function_count INT;
BEGIN
    SELECT current_database() INTO v_database;
    IF v_database <> 'netcafe_qa' THEN
        RAISE EXCEPTION 'station_customer.sql chỉ được chạy trên netcafe_qa, hiện tại là %', v_database;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'core' AND table_name = 'gaming_session'
          AND column_name = 'last_heartbeat_at'
    ) THEN
        RAISE EXCEPTION 'Thiếu core.gaming_session.last_heartbeat_at';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'core' AND table_name = 'gaming_session'
          AND column_name = 'close_reason'
    ) THEN
        RAISE EXCEPTION 'Thiếu core.gaming_session.close_reason';
    END IF;

    SELECT COUNT(*) INTO v_function_count
    FROM pg_proc p
    JOIN pg_namespace n ON n.oid = p.pronamespace
    WHERE (n.nspname, p.proname) IN (
        ('core', 'sp_customer_login'),
        ('core', 'sp_station_heartbeat'),
        ('core', 'sp_station_logout'),
        ('core', 'sp_reconcile_stale_sessions'),
        ('inventory', 'sp_customer_place_order'),
        ('inventory', 'fn_customer_order_status'),
        ('core', 'sp_create_service_request'),
        ('billing', 'sp_close_session_and_pay_at'),
        ('ai', 'fn_recommend_indexes'),
        ('ai', 'fn_detect_duplicates'),
        ('ai', 'fn_detect_anomalies'),
        ('ai', 'fn_suggest_promotions')
    );
    IF v_function_count < 12 THEN
        RAISE EXCEPTION 'Thiếu function station/AI: tìm thấy %, cần ít nhất 12', v_function_count;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'role_station')
       OR NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'station_runtime') THEN
        RAISE EXCEPTION 'Thiếu role_station hoặc station_runtime';
    END IF;

    IF has_table_privilege('station_runtime', 'core.customer', 'SELECT')
       OR has_table_privilege('station_runtime', 'core.gaming_session', 'SELECT') THEN
        RAISE EXCEPTION 'station_runtime đang có quyền đọc bảng nhạy cảm';
    END IF;

    IF NOT has_function_privilege(
        'station_runtime',
        'core.sp_customer_login(character varying,text,character varying)',
        'EXECUTE'
    ) THEN
        RAISE EXCEPTION 'station_runtime thiếu EXECUTE sp_customer_login';
    END IF;
END $$;

SELECT current_database() AS qa_database,
       EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
               WHERE n.nspname = 'core' AND p.proname = 'sp_reconcile_stale_sessions') AS stale_reconcile_ready,
       EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'role_station') AS station_role_ready,
       EXISTS (SELECT 1 FROM core.service_request) AS service_request_seeded;
