-- Bảo vệ role và function cho bản cập nhật chỉ còn admin/manager/cashier.
-- Role station là role kỹ thuật nội bộ, không xuất hiện trong Admin role selector.

\c netcafe_db;

-- Hợp nhất dữ liệu legacy trước khi siết CHECK role.
UPDATE hr.employee SET emp_role = 'cashier' WHERE emp_role = 'staff';
UPDATE hr.employee SET emp_role = 'manager' WHERE emp_role = 'technician';
ALTER TABLE hr.employee DROP CONSTRAINT IF EXISTS ck_employee_role;
ALTER TABLE hr.employee
    ADD CONSTRAINT ck_employee_role CHECK (emp_role IN ('admin', 'manager', 'cashier'));

-- Legacy login/group không còn được dùng bởi app.
DO $$
DECLARE
    v_role TEXT;
BEGIN
    FOR v_role IN
        SELECT rolname
        FROM pg_roles
        WHERE rolname IN ('role_staff', 'role_technician', 'role_report_readonly', 'nv_tuan', 'nv_hung')
    LOOP
        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai, reporting, staging FROM %I',
            v_role);
        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA core, inventory, billing, hr, audit, ai, reporting, staging FROM %I',
            v_role);
        EXECUTE format('ALTER ROLE %I NOLOGIN', v_role);
    END LOOP;
END $$;

-- Không để function mới tự cấp EXECUTE cho PUBLIC.
ALTER DEFAULT PRIVILEGES IN SCHEMA core, inventory, billing, hr, audit, ai, reporting
    REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
REVOKE ALL ON ALL FUNCTIONS IN SCHEMA core, inventory, billing, hr, audit, ai, reporting FROM PUBLIC;

-- Không cho manager/cashier ghi thẳng bảng để bypass function nghiệp vụ.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE
    ON ALL TABLES IN SCHEMA core, inventory, billing, hr, reporting, ai
    FROM role_manager, role_cashier;

GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting, ai TO role_admin, role_manager;
GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting TO role_cashier;

GRANT SELECT ON ALL TABLES IN SCHEMA core, inventory, billing, reporting, ai TO role_manager;
GRANT SELECT ON hr.maintenance TO role_manager;
REVOKE ALL ON hr.employee FROM role_manager;
GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_manager;
GRANT SELECT ON hr.vw_employee_public TO role_manager;

GRANT SELECT ON core.room, core.computer_type, core.computer, core.customer, core.gaming_session
    TO role_cashier;
GRANT SELECT ON core.service_request TO role_cashier;
GRANT SELECT ON inventory.food_order, inventory.food_order_detail,
    inventory.product_category, inventory.product TO role_cashier;
GRANT SELECT ON billing.invoice, billing.invoice_promotion, billing.payment, billing.promotion
    TO role_cashier;
GRANT SELECT ON ALL TABLES IN SCHEMA reporting TO role_cashier;
GRANT SELECT ON reporting.mv_revenue_daily TO role_admin, role_manager, role_cashier;
REVOKE ALL ON hr.employee FROM role_cashier;
GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_cashier;
GRANT SELECT ON hr.vw_employee_public TO role_cashier;

-- Core session/payment/order contracts.
GRANT EXECUTE ON FUNCTION core.fn_calc_session_amount(TIMESTAMPTZ, TIMESTAMPTZ, NUMERIC)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.fn_membership_tier(INT)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_open_session(INT, INT, INT)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION billing.sp_close_session_and_pay(BIGINT, INT, VARCHAR, VARCHAR)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION billing.sp_top_up(INT, INT, NUMERIC, VARCHAR, VARCHAR)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION inventory.sp_place_order(BIGINT, INT, INT, INT)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION inventory.sp_update_order_status(BIGINT, VARCHAR, INT)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_update_service_request_status(BIGINT, VARCHAR, INT)
    TO role_admin, role_manager, role_cashier;

-- Customer CRUD contract; UI không ghi core.customer trực tiếp nữa.
GRANT EXECUTE ON FUNCTION core.sp_create_customer(VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_update_customer(INT, VARCHAR, VARCHAR, VARCHAR)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_set_customer_active(INT, BOOLEAN)
    TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION core.sp_reset_customer_password(INT, TEXT)
    TO role_admin, role_manager;

-- Manager operational contracts.
GRANT EXECUTE ON FUNCTION inventory.sp_receive_stock(INT, INT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION reporting.sp_refresh_materialized_views() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_maintenance(INT, INT, TEXT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_complete_maintenance(INT, VARCHAR, NUMERIC) TO role_admin, role_manager;

-- Admin-only contracts.
GRANT EXECUTE ON FUNCTION billing.sp_void_invoice(BIGINT, INT) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_create_employee(VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, VARCHAR, VARCHAR, NUMERIC)
    TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_update_employee(INT, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, NUMERIC)
    TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_set_employee_active(INT, BOOLEAN) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_reset_employee_password(INT, TEXT) TO role_admin;
GRANT MAINTAIN ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai TO role_admin;
-- GRANT pg_read_all_stats bo qua tren Cloud Database

-- AI chỉ dành cho admin/manager.
GRANT EXECUTE ON FUNCTION ai.fn_recommend_indexes() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_auto_optimize_indexes() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_detect_duplicates(REAL) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_detect_anomalies(REAL) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_suggest_promotions() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_security_guard() TO role_admin, role_manager;

-- RLS helper chỉ cần cho ba role người dùng.
GRANT EXECUTE ON FUNCTION hr.fn_get_current_employee_id()
    TO role_admin, role_manager, role_cashier;
