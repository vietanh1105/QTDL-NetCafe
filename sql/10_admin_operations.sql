-- Trách nhiệm: contract PostgreSQL cho F&B, hóa đơn, báo cáo, bảo trì và nhân sự Admin.
-- Liên kết: DbRepository partial, OrdersView, InvoicesView, ReportsView, EmployeesView và MaintenanceView.
-- Bảo vệ: thao tác ghi chỉ được cấp EXECUTE cho role phù hợp; audit trigger vẫn chạy trong database.

CREATE OR REPLACE FUNCTION inventory.sp_receive_stock(
    p_product_id INT,
    p_quantity INT
) RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, inventory, audit, ai
AS $$
DECLARE
    v_stock INT;
BEGIN
    IF p_quantity <= 0 THEN
        RAISE EXCEPTION 'Số lượng nhập kho phải lớn hơn 0.' USING ERRCODE = '22023';
    END IF;

    UPDATE inventory.product
    SET stock_qty = stock_qty + p_quantity
    WHERE product_id = p_product_id
      AND is_active = TRUE
    RETURNING stock_qty INTO v_stock;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Sản phẩm ID % không tồn tại hoặc đã ngừng bán.', p_product_id;
    END IF;

    RETURN v_stock;
END;
$$;

COMMENT ON FUNCTION inventory.sp_receive_stock IS 'Nhập thêm tồn kho qua contract Admin, không cộng tồn kho từ UI trực tiếp.';

CREATE OR REPLACE FUNCTION inventory.sp_update_order_status(
    p_order_id BIGINT,
    p_status VARCHAR(20),
    p_employee_id INT
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, inventory, hr
AS $$
DECLARE
    v_current_status VARCHAR(20);
BEGIN
    IF p_status NOT IN ('served', 'cancelled') THEN
        RAISE EXCEPTION 'Trạng thái bếp không hợp lệ.' USING ERRCODE = '22023';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM hr.employee WHERE employee_id = p_employee_id AND is_active = TRUE) THEN
        RAISE EXCEPTION 'Nhân viên thao tác không tồn tại hoặc đã bị khóa.' USING ERRCODE = '42501';
    END IF;
    SELECT status INTO v_current_status FROM inventory.food_order WHERE order_id = p_order_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Đơn món #% không tồn tại.', p_order_id;
    END IF;
    IF v_current_status <> 'placed' THEN
        RAISE EXCEPTION 'Chỉ đơn placed mới được phục vụ hoặc hủy; trạng thái hiện tại: %.', v_current_status USING ERRCODE = 'P0001';
    END IF;
    UPDATE inventory.food_order
    SET status = p_status, employee_id = p_employee_id
    WHERE order_id = p_order_id;
END;
$$;

COMMENT ON FUNCTION inventory.sp_update_order_status IS 'Chuyển đơn bếp placed sang served/cancelled một lần duy nhất.';

CREATE OR REPLACE FUNCTION core.sp_update_service_request_status(
    p_request_id BIGINT,
    p_status VARCHAR(20),
    p_employee_id INT
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, hr
AS $$
DECLARE
    v_current_status VARCHAR(20);
BEGIN
    IF p_status NOT IN ('acknowledged', 'resolved', 'cancelled') THEN
        RAISE EXCEPTION 'Trạng thái hỗ trợ không hợp lệ.' USING ERRCODE = '22023';
    END IF;
    IF NOT EXISTS (
        SELECT 1
        FROM hr.employee
        WHERE employee_id = p_employee_id
          AND is_active = TRUE
    ) THEN
        RAISE EXCEPTION 'Nhân viên xử lý không tồn tại hoặc đã bị khóa.' USING ERRCODE = '42501';
    END IF;

    SELECT status
    INTO v_current_status
    FROM core.service_request
    WHERE service_request_id = p_request_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Yêu cầu hỗ trợ #% không tồn tại.', p_request_id;
    END IF;
    IF v_current_status IN ('resolved', 'cancelled') THEN
        RAISE EXCEPTION 'Yêu cầu đã kết thúc, không thể đổi trạng thái lần nữa.' USING ERRCODE = 'P0001';
    END IF;
    IF p_status = 'resolved' AND v_current_status <> 'acknowledged' THEN
        RAISE EXCEPTION 'Chỉ yêu cầu acknowledged mới được hoàn thành.' USING ERRCODE = 'P0001';
    END IF;

    UPDATE core.service_request
    SET status = p_status,
        handled_by = p_employee_id,
        acknowledged_at = CASE
            WHEN p_status = 'acknowledged' THEN COALESCE(acknowledged_at, clock_timestamp())
            ELSE acknowledged_at
        END,
        resolved_at = CASE
            WHEN p_status IN ('resolved', 'cancelled') THEN clock_timestamp()
            ELSE resolved_at
        END
    WHERE service_request_id = p_request_id;
END;
$$;

COMMENT ON FUNCTION core.sp_update_service_request_status IS 'Chuyển yêu cầu hỗ trợ theo luồng new/acknowledged/resolved/cancelled và ghi nhân viên xử lý.';

CREATE OR REPLACE FUNCTION billing.sp_void_invoice(
    p_invoice_id BIGINT,
    p_employee_id INT
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, billing, hr
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM hr.employee
        WHERE employee_id = p_employee_id AND is_active = TRUE
    ) THEN
        RAISE EXCEPTION 'Nhân viên thao tác không tồn tại hoặc đã bị khóa.' USING ERRCODE = '42501';
    END IF;

    UPDATE billing.invoice
    SET status = 'void'
    WHERE invoice_id = p_invoice_id
      AND status = 'paid';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Chỉ hóa đơn paid mới được hủy hoặc hóa đơn không tồn tại.' USING ERRCODE = 'P0001';
    END IF;
END;
$$;

COMMENT ON FUNCTION billing.sp_void_invoice IS 'Hủy hóa đơn paid; trigger audit/AI ghi nhận rủi ro và phát ai_security_alert.';

CREATE OR REPLACE FUNCTION reporting.sp_refresh_materialized_views()
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, reporting
AS $$
BEGIN
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;
    REFRESH MATERIALIZED VIEW reporting.mv_monthly_revenue;
END;
$$;

COMMENT ON FUNCTION reporting.sp_refresh_materialized_views IS 'Refresh snapshot báo cáo theo yêu cầu Admin; không chạy tự động khi mở màn hình.';

CREATE OR REPLACE FUNCTION hr.sp_create_maintenance(
    p_computer_id INT,
    p_technician_id INT,
    p_issue_description TEXT
) RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr, core
AS $$
DECLARE
    v_maintenance_id INT;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM core.computer WHERE computer_id = p_computer_id) THEN
        RAISE EXCEPTION 'Máy ID % không tồn tại.', p_computer_id;
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM hr.employee
        WHERE employee_id = p_technician_id
          AND is_active = TRUE
          AND emp_role = 'technician'
    ) THEN
        RAISE EXCEPTION 'Nhân viên phụ trách phải là technician đang hoạt động.' USING ERRCODE = '23514';
    END IF;
    IF NULLIF(trim(p_issue_description), '') IS NULL THEN
        RAISE EXCEPTION 'Lý do bảo trì không được để trống.' USING ERRCODE = '22023';
    END IF;

    INSERT INTO hr.maintenance (computer_id, technician_id, issue_description, status)
    VALUES (p_computer_id, p_technician_id, trim(p_issue_description), 'reported')
    RETURNING maintenance_id INTO v_maintenance_id;
    RETURN v_maintenance_id;
END;
$$;

CREATE OR REPLACE FUNCTION hr.sp_complete_maintenance(
    p_maintenance_id INT,
    p_status VARCHAR(20),
    p_cost NUMERIC
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr, core
AS $$
BEGIN
    IF p_status NOT IN ('fixed', 'unrepairable') THEN
        RAISE EXCEPTION 'Trạng thái hoàn tất phải là fixed hoặc unrepairable.' USING ERRCODE = '22023';
    END IF;
    IF p_cost < 0 THEN
        RAISE EXCEPTION 'Chi phí bảo trì không được âm.' USING ERRCODE = '22023';
    END IF;

    UPDATE hr.maintenance
    SET status = p_status,
        cost = p_cost,
        finished_at = clock_timestamp()
    WHERE maintenance_id = p_maintenance_id
      AND finished_at IS NULL;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiếu bảo trì không tồn tại hoặc đã hoàn tất.';
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION hr.sp_create_employee(
    p_full_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_username VARCHAR(30),
    p_db_username VARCHAR(50),
    p_password TEXT,
    p_emp_role VARCHAR(20),
    p_default_shift VARCHAR(20),
    p_salary NUMERIC
) RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr, public
AS $$
DECLARE
    v_employee_id INT;
BEGIN
    INSERT INTO hr.employee (
        full_name, phone, username, db_username, password_hash,
        emp_role, default_shift, salary
    ) VALUES (
        trim(p_full_name), trim(p_phone), lower(trim(p_username)),
        NULLIF(lower(trim(p_db_username)), ''),
        public.crypt(p_password, public.gen_salt('bf')),
        p_emp_role, p_default_shift, p_salary
    ) RETURNING employee_id INTO v_employee_id;
    RETURN v_employee_id;
END;
$$;

CREATE OR REPLACE FUNCTION hr.sp_update_employee(
    p_employee_id INT,
    p_full_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_db_username VARCHAR(50),
    p_emp_role VARCHAR(20),
    p_default_shift VARCHAR(20),
    p_salary NUMERIC
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr
AS $$
BEGIN
    UPDATE hr.employee
    SET full_name = trim(p_full_name),
        phone = trim(p_phone),
        db_username = NULLIF(lower(trim(p_db_username)), ''),
        emp_role = p_emp_role,
        default_shift = p_default_shift,
        salary = p_salary
    WHERE employee_id = p_employee_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Nhân viên ID % không tồn tại.', p_employee_id;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION hr.sp_set_employee_active(
    p_employee_id INT,
    p_is_active BOOLEAN
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr
AS $$
BEGIN
    UPDATE hr.employee SET is_active = p_is_active WHERE employee_id = p_employee_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Nhân viên ID % không tồn tại.', p_employee_id;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION hr.sp_reset_employee_password(
    p_employee_id INT,
    p_password TEXT
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr, public
AS $$
BEGIN
    IF NULLIF(trim(p_password), '') IS NULL THEN
        RAISE EXCEPTION 'Mật khẩu mới không được để trống.' USING ERRCODE = '22023';
    END IF;
    UPDATE hr.employee
    SET password_hash = public.crypt(p_password, public.gen_salt('bf'))
    WHERE employee_id = p_employee_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Nhân viên ID % không tồn tại.', p_employee_id;
    END IF;
END;
$$;

GRANT EXECUTE ON FUNCTION inventory.sp_receive_stock(INT, INT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION inventory.sp_update_order_status(BIGINT, VARCHAR, INT) TO role_admin, role_manager, role_cashier, role_staff;
GRANT EXECUTE ON FUNCTION core.sp_update_service_request_status(BIGINT, VARCHAR, INT) TO role_admin, role_manager, role_cashier, role_staff;
GRANT EXECUTE ON FUNCTION billing.sp_void_invoice(BIGINT, INT) TO role_admin;
GRANT EXECUTE ON FUNCTION reporting.sp_refresh_materialized_views() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_maintenance(INT, INT, TEXT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_complete_maintenance(INT, VARCHAR, NUMERIC) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_employee(VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, VARCHAR, VARCHAR, NUMERIC) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_update_employee(INT, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, NUMERIC) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_set_employee_active(INT, BOOLEAN) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_reset_employee_password(INT, TEXT) TO role_admin;
