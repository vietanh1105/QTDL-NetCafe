-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 02_functions_triggers.sql
-- Mục đích: Khởi tạo các Function tính toán, Stored Procedure nghiệp vụ và Trigger tự động
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- 1. HÀM TÍNH TOÁN NGHIỆP VỤ (BUSINESS FUNCTIONS)
-- ----------------------------------------------------------------------------

-- 1.1. Hàm tính tiền giờ chơi
-- Quy tắc: Tính theo số phút thực tế, làm tròn đến hàng đơn vị đồng
CREATE OR REPLACE FUNCTION core.fn_calc_session_amount(
    p_start_time TIMESTAMPTZ,
    p_end_time TIMESTAMPTZ,
    p_hourly_rate NUMERIC
) RETURNS NUMERIC AS $$
DECLARE
    v_seconds NUMERIC;
    v_amount NUMERIC;
BEGIN
    IF p_end_time IS NULL OR p_start_time IS NULL OR p_end_time <= p_start_time THEN
        RETURN 0;
    END IF;

    -- Tính số giây chơi thực tế
    v_seconds := EXTRACT(EPOCH FROM (p_end_time - p_start_time));
    
    -- Tiền = (Số giây / 3600) * Đơn giá giờ, làm tròn đến đồng
    v_amount := ROUND((v_seconds / 3600.0) * p_hourly_rate);
    
    RETURN v_amount;
END;
$$ LANGUAGE plpgsql IMMUTABLE;
COMMENT ON FUNCTION core.fn_calc_session_amount IS 'Hàm tính tiền giờ chơi theo giây và đơn giá áp dụng';

-- 1.2. Hàm xác định hạng thành viên theo điểm tích lũy
CREATE OR REPLACE FUNCTION core.fn_membership_tier(p_points INT) 
RETURNS VARCHAR AS $$
BEGIN
    IF p_points >= 2000 THEN
        RETURN 'vip';
    ELSIF p_points >= 500 THEN
        RETURN 'gold';
    ELSIF p_points >= 100 THEN
        RETURN 'silver';
    ELSE
        RETURN 'regular';
    END IF;
END;
$$ LANGUAGE plpgsql IMMUTABLE;
COMMENT ON FUNCTION core.fn_membership_tier IS 'Xác định hạng thành viên theo số điểm tích lũy';

-- ----------------------------------------------------------------------------
-- 2. TRIGGER TỰ ĐỘNG HÓA VẬN HÀNH PHÒNG MÁY (TRIGGERS)
-- ----------------------------------------------------------------------------

-- 2.1. Trigger cập nhật trạng thái máy theo phiên chơi
CREATE OR REPLACE FUNCTION core.fn_trg_sync_computer_session() 
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' AND NEW.status = 'active' THEN
        -- Kiểm tra máy phải đang available
        IF (SELECT status FROM core.computer WHERE computer_id = NEW.computer_id) <> 'available' THEN
            RAISE EXCEPTION 'Máy tính ID % không ở trạng thái trống (available) để mở phiên!', NEW.computer_id;
        END IF;
        UPDATE core.computer 
        SET status = 'in_use' 
        WHERE computer_id = NEW.computer_id;

    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.status = 'active' AND NEW.status IN ('finished', 'cancelled') THEN
            UPDATE core.computer 
            SET status = 'available' 
            WHERE computer_id = NEW.computer_id;
        ELSIF OLD.status <> 'active' AND NEW.status = 'active' THEN
            UPDATE core.computer 
            SET status = 'in_use' 
            WHERE computer_id = NEW.computer_id;
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_session_sync_computer ON core.gaming_session;
CREATE TRIGGER trg_session_sync_computer
    AFTER INSERT OR UPDATE ON core.gaming_session
    FOR EACH ROW
    EXECUTE FUNCTION core.fn_trg_sync_computer_session();

-- 2.2. Trigger tự động trừ tồn kho khi gọi món F&B
-- Ràng buộc stock_qty >= 0 trên bảng product sẽ chặn đứng nếu bán vượt tồn kho!
CREATE OR REPLACE FUNCTION inventory.fn_trg_deduct_stock() 
RETURNS TRIGGER AS $$
BEGIN
    UPDATE inventory.product
    SET stock_qty = stock_qty - NEW.quantity
    WHERE product_id = NEW.product_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_order_detail_deduct_stock ON inventory.food_order_detail;
CREATE TRIGGER trg_order_detail_deduct_stock
    AFTER INSERT ON inventory.food_order_detail
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_trg_deduct_stock();

-- 2.3. Trigger đồng bộ trạng thái máy khi bảo trì
CREATE OR REPLACE FUNCTION hr.fn_trg_sync_computer_maintenance() 
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status IN ('reported', 'in_progress') THEN
        UPDATE core.computer 
        SET status = 'maintenance' 
        WHERE computer_id = NEW.computer_id;
    ELSIF NEW.status = 'fixed' THEN
        UPDATE core.computer 
        SET status = 'available' 
        WHERE computer_id = NEW.computer_id;
    ELSIF NEW.status = 'unrepairable' THEN
        UPDATE core.computer 
        SET status = 'broken' 
        WHERE computer_id = NEW.computer_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_maintenance_sync_computer ON hr.maintenance;
CREATE TRIGGER trg_maintenance_sync_computer
    AFTER INSERT OR UPDATE ON hr.maintenance
    FOR EACH ROW
    EXECUTE FUNCTION hr.fn_trg_sync_computer_maintenance();

-- ----------------------------------------------------------------------------
-- 3. STORED FUNCTIONS NGHIỆP VỤ (GỌI TỪ PHẦN MỀM DESKTOP)
-- ----------------------------------------------------------------------------

-- 3.1. Nghiệp vụ: Mở phiên chơi máy cho khách hàng
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
    -- 1. Kiểm tra khách hàng
    SELECT is_active, balance INTO v_cust_active, v_cust_balance
    FROM core.customer 
    WHERE customer_id = p_customer_id;

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
    WHERE c.computer_id = p_computer_id;

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
$$ LANGUAGE plpgsql;
COMMENT ON FUNCTION core.sp_open_session IS 'Nghiệp vụ mở phiên chơi: Kiểm tra số dư, trạng thái máy, chốt đơn giá giờ';

-- 3.2. Nghiệp vụ: Đóng phiên chơi và Thanh toán nguyên tử (Atomic Close & Settle)
CREATE OR REPLACE FUNCTION billing.sp_close_session_and_pay(
    p_session_id BIGINT,
    p_cashier_id INT,
    p_payment_method VARCHAR(20) DEFAULT 'balance',
    p_promo_code VARCHAR(30) DEFAULT NULL
) RETURNS BIGINT AS $$
DECLARE
    v_session RECORD;
    v_customer RECORD;
    v_end_time TIMESTAMPTZ := clock_timestamp();
    v_play_amount NUMERIC := 0;
    v_service_amount NUMERIC := 0;
    v_discount_amount NUMERIC := 0;
    v_total_amount NUMERIC := 0;
    v_invoice_id BIGINT;
    v_promo RECORD;
    v_applied_promo_id INT := NULL;
BEGIN
    -- 1. Khóa và lấy thông tin phiên chơi (SELECT FOR UPDATE)
    SELECT * INTO v_session
    FROM core.gaming_session
    WHERE session_id = p_session_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiên chơi ID % không tồn tại!', p_session_id;
    END IF;

    IF v_session.status <> 'active' THEN
        RAISE EXCEPTION 'Phiên chơi ID % đã được đóng trước đó (Trạng thái: %)!', p_session_id, v_session.status;
    END IF;

    -- 2. Khóa và kiểm tra tài khoản khách hàng
    SELECT * INTO v_customer
    FROM core.customer
    WHERE customer_id = v_session.customer_id
    FOR UPDATE;

    -- 3. Tính tiền giờ chơi
    v_play_amount := core.fn_calc_session_amount(v_session.start_time, v_end_time, v_session.hourly_rate_applied);
    
    -- Tối thiểu tính 15 phút nếu vừa mở máy
    IF v_play_amount = 0 THEN
        v_play_amount := ROUND((15.0 / 60.0) * v_session.hourly_rate_applied);
    END IF;

    -- 4. Tính tổng tiền dịch vụ đồ ăn uống (F&B) chưa hủy
    SELECT COALESCE(SUM(fod.quantity * fod.unit_price), 0) INTO v_service_amount
    FROM inventory.food_order fo
    JOIN inventory.food_order_detail fod ON fo.order_id = fod.order_id
    WHERE fo.session_id = p_session_id AND fo.status <> 'cancelled';

    -- 5. Xử lý khuyến mãi nếu có
    IF p_promo_code IS NOT NULL AND p_promo_code <> '' THEN
        SELECT * INTO v_promo
        FROM billing.promotion
        WHERE code = p_promo_code AND is_active = TRUE
          AND v_end_time BETWEEN valid_from AND valid_to;

        IF FOUND THEN
            v_applied_promo_id := v_promo.promotion_id;
            IF v_promo.promo_type = 'percent' THEN
                v_discount_amount := ROUND((v_play_amount * v_promo.value) / 100.0);
            ELSIF v_promo.promo_type = 'fixed' THEN
                v_discount_amount := LEAST(v_promo.value, v_play_amount);
            ELSIF v_promo.promo_type = 'happy_hour' THEN
                v_discount_amount := ROUND((v_play_amount * v_promo.value) / 100.0);
            END IF;
        END IF;
    END IF;

    -- 6. Tính tổng tiền thanh toán theo công thức bắt buộc
    v_total_amount := v_play_amount + v_service_amount + 0 - v_discount_amount;
    IF v_total_amount < 0 THEN
        v_total_amount := 0;
    END IF;

    -- 7. Xử lý trừ tiền
    IF p_payment_method = 'balance' THEN
        IF v_customer.balance < v_total_amount THEN
            RAISE EXCEPTION 'Số dư tài khoản hội viên không đủ (Số dư: % VNĐ, Cần thanh toán: % VNĐ)! Vui lòng nạp thêm tiền hoặc chọn tiền mặt.', 
                            v_customer.balance, v_total_amount;
        END IF;

        -- Trừ số dư và cộng điểm tích lũy (10.000đ = 1 điểm)
        UPDATE core.customer
        SET balance = balance - v_total_amount,
            loyalty_points = loyalty_points + FLOOR(v_total_amount / 10000)
        WHERE customer_id = v_session.customer_id;
    ELSE
        -- Thanh toán tiền mặt/chuyển khoản: chỉ cộng điểm tích lũy
        UPDATE core.customer
        SET loyalty_points = loyalty_points + FLOOR(v_total_amount / 10000)
        WHERE customer_id = v_session.customer_id;
    END IF;

    -- Cập nhật lại hạng thành viên nếu đủ điểm
    UPDATE core.customer
    SET membership_tier = core.fn_membership_tier(loyalty_points)
    WHERE customer_id = v_session.customer_id;

    -- 8. Cập nhật phiên chơi kết thúc
    UPDATE core.gaming_session
    SET end_time = v_end_time,
        status = 'finished',
        play_amount = v_play_amount
    WHERE session_id = p_session_id;

    -- 9. Tạo hóa đơn thanh toán (Quan hệ 1-1 với phiên chơi)
    INSERT INTO billing.invoice (
        session_id, customer_id, cashier_id, play_amount, service_amount,
        surcharge, discount_amount, total_amount, status, issued_at, paid_at
    ) VALUES (
        p_session_id, v_session.customer_id, p_cashier_id, v_play_amount, v_service_amount,
        0, v_discount_amount, v_total_amount, 'paid', v_end_time, v_end_time
    ) RETURNING invoice_id INTO v_invoice_id;

    -- Ghi nhận khuyến mãi nếu có
    IF v_discount_amount > 0 AND v_applied_promo_id IS NOT NULL THEN
        INSERT INTO billing.invoice_promotion (invoice_id, promotion_id, discount_amount)
        VALUES (v_invoice_id, v_applied_promo_id, v_discount_amount);
    END IF;

    -- 10. Ghi nhận giao dịch sổ quỹ
    INSERT INTO billing.payment (
        kind, invoice_id, customer_id, method, amount, employee_id, paid_at, reference
    ) VALUES (
        'invoice', v_invoice_id, v_session.customer_id, p_payment_method, v_total_amount, p_cashier_id, v_end_time, 'Thanh toán phiên chơi #' || p_session_id
    );

    -- Trigger trg_session_sync_computer sẽ tự động trả computer.status = 'available'
    RETURN v_invoice_id;
END;
$$ LANGUAGE plpgsql;
COMMENT ON FUNCTION billing.sp_close_session_and_pay IS 'Nghiệp vụ đóng phiên và thanh toán trọn gói trong 1 transaction nguyên tử';

-- 3.3. Nghiệp vụ: Nạp tiền vào tài khoản hội viên
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
$$ LANGUAGE plpgsql;
COMMENT ON FUNCTION billing.sp_top_up IS 'Nghiệp vụ nạp tiền tài khoản: Cộng số dư và ghi sổ quỹ';

-- 3.4. Nghiệp vụ: Gọi món F&B cho phiên chơi
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
$$ LANGUAGE plpgsql;
COMMENT ON FUNCTION inventory.sp_place_order IS 'Nghiệp vụ gọi món F&B: Kiểm tra tồn kho, tạo đơn và trừ kho';
