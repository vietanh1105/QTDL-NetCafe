-- Kiểm tra nghiệp vụ bằng role thu ngân thật; mọi dữ liệu thử đều ROLLBACK.
\set ON_ERROR_STOP on
BEGIN;

DO $$
BEGIN
    IF current_database() <> 'netcafe_qa' THEN
        RAISE EXCEPTION 'verify_defense_roles.sql chỉ chạy trên netcafe_qa';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE (n.nspname, p.proname) IN (
            ('core','sp_open_session'), ('billing','sp_top_up'), ('inventory','sp_place_order')
        ) AND NOT p.prosecdef
    ) THEN
        RAISE EXCEPTION 'Nghiệp vụ cần SECURITY DEFINER chưa được cập nhật';
    END IF;
END;
$$;

INSERT INTO core.customer(full_name,phone,username,password_hash,balance)
VALUES ('TEST Defense Role','0930000099','test_defense_role',
        public.crypt('Test@123',public.gen_salt('bf')),80000);

SET LOCAL ROLE nv_thao;
SELECT billing.sp_top_up(
    (SELECT customer_id FROM core.customer WHERE username='test_defense_role'),
    hr.fn_get_current_employee_id(), 10000::numeric, 'cash'::varchar, 'TEST_ROLE'::varchar
);

DO $$
BEGIN
    BEGIN
        PERFORM billing.sp_top_up(
            (SELECT customer_id FROM core.customer WHERE username='test_defense_role'),
            hr.fn_get_current_employee_id() + 1000000,
            10000::numeric, 'cash'::varchar, NULL::varchar
        );
        RAISE EXCEPTION 'Không được mạo danh nhân viên khác';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'PASS cashier_actor_spoof=DENIED';
    END;
END;
$$;

SELECT core.sp_open_session(
    (SELECT customer_id FROM core.customer WHERE username='test_defense_role'),
    (SELECT computer_id FROM core.computer WHERE status='available' ORDER BY computer_id LIMIT 1),
    hr.fn_get_current_employee_id()
);

SELECT inventory.sp_place_order(
    (SELECT s.session_id FROM core.gaming_session s
     JOIN core.customer c ON c.customer_id=s.customer_id
     WHERE c.username='test_defense_role' AND s.status='active'),
    hr.fn_get_current_employee_id(),
    (SELECT product_id FROM inventory.product WHERE stock_qty>0 AND is_active ORDER BY product_id LIMIT 1),
    1
);

SELECT billing.sp_close_session_and_pay(
    (SELECT s.session_id FROM core.gaming_session s
     JOIN core.customer c ON c.customer_id=s.customer_id
     WHERE c.username='test_defense_role' AND s.status='active'),
    hr.fn_get_current_employee_id(), 'balance'::varchar, NULL::varchar
);

ROLLBACK;
SELECT 'PASS cashier topup/open/order/close and actor validation' AS result;
