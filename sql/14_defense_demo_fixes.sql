-- Sửa quyền các nghiệp vụ Admin trên database đã tồn tại, không reset dữ liệu.
-- Các định nghĩa đồng bộ với sql/02_functions_triggers.sql.
-- Chạy bằng DBA, sau khi sao lưu và kiểm tra database.
BEGIN;


CREATE OR REPLACE FUNCTION hr.fn_assert_employee_actor(p_employee_id INT)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, hr
AS $$
DECLARE
    v_effective_user TEXT := COALESCE(NULLIF(current_setting('role', true), 'none'), session_user);
    v_employee_id INT;
    v_is_active BOOLEAN;
BEGIN
    SELECT employee_id, is_active INTO v_employee_id, v_is_active
    FROM hr.employee
    WHERE db_username = v_effective_user;

    IF v_employee_id IS NOT NULL THEN
        IF v_is_active AND p_employee_id = v_employee_id THEN
            RETURN;
        END IF;
        RAISE EXCEPTION 'Nhân viên thao tác không khớp tài khoản hoặc đã bị khóa.'
            USING ERRCODE = '42501';
    END IF;

    -- DBA được thao tác trên dữ liệu demo nhưng vẫn phải dùng một nhân viên active.
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_effective_user AND rolsuper)
       OR v_effective_user = (
           SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = current_database()
       ) THEN
        IF EXISTS (
            SELECT 1 FROM hr.employee WHERE employee_id = p_employee_id AND is_active
        ) THEN
            RETURN;
        END IF;
    END IF;

    RAISE EXCEPTION 'Tài khoản chưa được ánh xạ với nhân viên được phép thao tác.'
        USING ERRCODE = '42501';
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_open_session(
    p_customer_id INT,
    p_computer_id INT,
    p_opened_by INT
) RETURNS BIGINT AS $$
DECLARE
    v_cust_active BOOLEAN;
    v_cust_balance NUMERIC;
    v_active_sess_count INT;
    v_comp_status VARCHAR(20);
    v_hourly_rate NUMERIC;
    v_session_id BIGINT;
BEGIN
    PERFORM hr.fn_assert_employee_actor(p_opened_by);
    -- 1. Kiểm tra khách hàng
    SELECT is_active, balance INTO v_cust_active, v_cust_balance
    FROM core.customer
    WHERE customer_id = p_customer_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Khách hàng ID % không tồn tại!', p_customer_id;
    END IF;

    IF NOT v_cust_active THEN
        RAISE EXCEPTION 'Tài khoản khách hàng ID % đang bị khóa!', p_customer_id;
    END IF;

    IF v_cust_balance <= 0 THEN
        RAISE EXCEPTION 'Số dư tài khoản không đủ để mở máy (Số dư hiện tại: % VNĐ)!', v_cust_balance;
    END IF;

    -- Kiểm tra khách hàng đang có phiên hoạt động nào không (Mỗi khách tối đa 1 phiên active)
    SELECT COUNT(*) INTO v_active_sess_count
    FROM core.gaming_session
    WHERE customer_id = p_customer_id AND status = 'active';

    IF v_active_sess_count > 0 THEN
        RAISE EXCEPTION 'Khách hàng ID % đang có phiên chơi hoạt động trên máy khác!', p_customer_id;
    END IF;

    -- 2. Kiểm tra máy tính
    SELECT c.status, ct.hourly_rate
    INTO v_comp_status, v_hourly_rate
    FROM core.computer c
    JOIN core.computer_type ct ON c.type_id = ct.type_id
    WHERE c.computer_id = p_computer_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Máy tính ID % không tồn tại!', p_computer_id;
    END IF;

    IF v_comp_status <> 'available' THEN
        RAISE EXCEPTION 'Máy tính ID % không ở trạng thái sẵn sàng (Trạng thái hiện tại: %)!', p_computer_id, v_comp_status;
    END IF;

    -- 3. Mở phiên chơi mới
    INSERT INTO core.gaming_session (
        customer_id, computer_id, opened_by, start_time, status, hourly_rate_applied
    ) VALUES (
        p_customer_id, p_computer_id, p_opened_by, clock_timestamp(), 'active', v_hourly_rate
    ) RETURNING session_id INTO v_session_id;

    -- Trigger trg_session_sync_computer sẽ tự động cập nhật computer.status = 'in_use'
    RETURN v_session_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai;

CREATE OR REPLACE FUNCTION billing.sp_top_up(
    p_customer_id INT,
    p_employee_id INT,
    p_amount NUMERIC,
    p_method VARCHAR(20) DEFAULT 'cash',
    p_reference VARCHAR(100) DEFAULT NULL
) RETURNS NUMERIC AS $$
DECLARE
    v_new_balance NUMERIC;
BEGIN
    PERFORM hr.fn_assert_employee_actor(p_employee_id);
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Số tiền nạp phải lớn hơn 0 (Số tiền nhập: %)!', p_amount;
    END IF;

    -- Cập nhật số dư tài khoản
    UPDATE core.customer
    SET balance = balance + p_amount
    WHERE customer_id = p_customer_id
    RETURNING balance INTO v_new_balance;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Khách hàng ID % không tồn tại!', p_customer_id;
    END IF;

    -- Ghi sổ quỹ giao dịch nạp tiền
    INSERT INTO billing.payment (
        kind, invoice_id, customer_id, method, amount, employee_id, paid_at, reference
    ) VALUES (
        'topup', NULL, p_customer_id, p_method, p_amount, p_employee_id, clock_timestamp(),
        COALESCE(p_reference, 'Nạp tiền tài khoản hội viên')
    );

    RETURN v_new_balance;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai;

CREATE OR REPLACE FUNCTION inventory.sp_place_order(
    p_session_id BIGINT,
    p_employee_id INT,
    p_product_id INT,
    p_quantity INT
) RETURNS BIGINT AS $$
DECLARE
    v_sess_status VARCHAR(20);
    v_unit_price NUMERIC;
    v_stock_qty INT;
    v_order_id BIGINT;
BEGIN
    PERFORM hr.fn_assert_employee_actor(p_employee_id);
    -- Kiểm tra phiên chơi
    SELECT status INTO v_sess_status
    FROM core.gaming_session
    WHERE session_id = p_session_id;

    IF NOT FOUND OR v_sess_status <> 'active' THEN
        RAISE EXCEPTION 'Phiên chơi ID % không hoạt động để gọi món!', p_session_id;
    END IF;

    -- Kiểm tra sản phẩm và tồn kho
    SELECT unit_price, stock_qty INTO v_unit_price, v_stock_qty
    FROM inventory.product
    WHERE product_id = p_product_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Sản phẩm ID % không tồn tại!', p_product_id;
    END IF;

    IF v_stock_qty < p_quantity THEN
        RAISE EXCEPTION 'Không đủ tồn kho để bán (Tồn kho hiện có: %, Yêu cầu: %)!', v_stock_qty, p_quantity;
    END IF;

    -- Tạo đơn gọi món mới
    INSERT INTO inventory.food_order (session_id, employee_id, status)
    VALUES (p_session_id, p_employee_id, 'placed')
    RETURNING order_id INTO v_order_id;

    -- Thêm chi tiết món (Trigger trg_order_detail_deduct_stock sẽ tự trừ tồn kho)
    INSERT INTO inventory.food_order_detail (order_id, product_id, quantity, unit_price)
    VALUES (v_order_id, p_product_id, p_quantity, v_unit_price);

    RETURN v_order_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai;

REVOKE ALL ON FUNCTION hr.fn_assert_employee_actor(INT) FROM PUBLIC;

COMMIT;
