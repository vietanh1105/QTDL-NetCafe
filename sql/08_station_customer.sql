-- Vai trò: mở rộng contract cho NetCafe.Client và queue hỗ trợ.
-- Liên kết: sql/01_schema.sql, sql/02_functions_triggers.sql, app/NetCafe.Data.
-- Bảo mật: station chỉ EXECUTE function SECURITY DEFINER, không đọc bảng trực tiếp.

ALTER TABLE core.gaming_session
    ADD COLUMN IF NOT EXISTS last_heartbeat_at TIMESTAMPTZ;

ALTER TABLE core.gaming_session
    ADD COLUMN IF NOT EXISTS close_reason VARCHAR(40);

UPDATE core.gaming_session
SET last_heartbeat_at = COALESCE(last_heartbeat_at, start_time)
WHERE last_heartbeat_at IS NULL;

-- View mở rộng sau migration để Admin đọc đủ tiền session, số dư và heartbeat.
CREATE OR REPLACE VIEW reporting.vw_computer_grid AS
SELECT
    c.computer_id,
    c.computer_code,
    c.computer_name,
    r.room_name,
    ct.type_name,
    ct.hourly_rate,
    c.status AS computer_status,
    c.ip_address,
    s.session_id AS current_session_id,
    cust.customer_id AS current_customer_id,
    cust.full_name AS current_customer_name,
    cust.username AS current_username,
    s.start_time AS session_start_time,
    ROUND(EXTRACT(EPOCH FROM (clock_timestamp() - s.start_time)) / 60.0) AS played_minutes,
    c.specs,
    -- Giữ specs ở vị trí cũ để CREATE OR REPLACE VIEW không đổi tên cột hiện hữu.
    COALESCE(s.play_amount, 0) AS current_play_amount,
    COALESCE((
        SELECT SUM(fod.quantity * fod.unit_price)
        FROM inventory.food_order AS fo
        JOIN inventory.food_order_detail AS fod ON fod.order_id = fo.order_id
        WHERE fo.session_id = s.session_id
          AND fo.status <> 'cancelled'
    ), 0) AS current_service_amount,
    COALESCE(cust.balance, 0) AS current_balance,
    s.last_heartbeat_at
FROM core.computer AS c
JOIN core.room AS r ON c.room_id = r.room_id
JOIN core.computer_type AS ct ON c.type_id = ct.type_id
LEFT JOIN core.gaming_session AS s
    ON c.computer_id = s.computer_id AND s.status = 'active'
LEFT JOIN core.customer AS cust ON s.customer_id = cust.customer_id
ORDER BY c.computer_code;

CREATE INDEX IF NOT EXISTS idx_session_heartbeat_active
    ON core.gaming_session (last_heartbeat_at)
    WHERE status = 'active';

CREATE UNIQUE INDEX IF NOT EXISTS uq_active_session_customer
    ON core.gaming_session (customer_id)
    WHERE status = 'active';

CREATE UNIQUE INDEX IF NOT EXISTS uq_active_session_computer
    ON core.gaming_session (computer_id)
    WHERE status = 'active';

CREATE TABLE IF NOT EXISTS core.service_request (
    service_request_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    session_id BIGINT NULL REFERENCES core.gaming_session(session_id) ON DELETE RESTRICT,
    customer_id INT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    computer_id INT NOT NULL REFERENCES core.computer(computer_id) ON DELETE RESTRICT,
    request_type VARCHAR(30) NOT NULL DEFAULT 'general',
    message TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'new',
    created_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    acknowledged_at TIMESTAMPTZ NULL,
    resolved_at TIMESTAMPTZ NULL,
    handled_by INT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    CONSTRAINT ck_service_request_type CHECK (request_type IN ('general', 'technical', 'food', 'payment')),
    CONSTRAINT ck_service_request_status CHECK (status IN ('new', 'acknowledged', 'resolved', 'cancelled')),
    CONSTRAINT ck_service_request_message CHECK (length(trim(message)) BETWEEN 1 AND 1000)
);

CREATE INDEX IF NOT EXISTS idx_service_request_status_created
    ON core.service_request (status, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_service_request_session
    ON core.service_request (session_id, created_at DESC);

CREATE TABLE IF NOT EXISTS core.station_runtime_config (
    config_key VARCHAR(50) PRIMARY KEY,
    config_value TEXT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

COMMENT ON TABLE core.station_runtime_config IS 'Cấu hình station không chứa mật khẩu; service_employee_id phải trỏ nhân viên active.';

CREATE OR REPLACE FUNCTION core.fn_station_service_employee_id()
RETURNS INT
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = core, hr, pg_catalog
AS $$
DECLARE
    v_employee_id INT;
BEGIN
    SELECT e.employee_id INTO v_employee_id
    FROM hr.employee e
    JOIN core.station_runtime_config c ON c.config_key = 'service_employee_id'
    WHERE e.employee_id = c.config_value::INT AND e.is_active = TRUE;
    IF v_employee_id IS NULL THEN
        RAISE EXCEPTION 'STATION_SERVICE_EMPLOYEE_ID chưa được cấu hình hoặc nhân viên không active.'
            USING ERRCODE = '23514';
    END IF;
    RETURN v_employee_id;
END;
$$;

CREATE OR REPLACE FUNCTION billing.sp_close_session_and_pay_at(
    p_session_id BIGINT,
    p_cashier_id INT,
    p_payment_method VARCHAR(20) DEFAULT 'balance',
    p_promo_code VARCHAR(30) DEFAULT NULL,
    p_end_time TIMESTAMPTZ DEFAULT clock_timestamp(),
    p_close_reason VARCHAR(40) DEFAULT 'admin_close'
) RETURNS BIGINT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_session RECORD;
    v_customer RECORD;
    v_end_time TIMESTAMPTZ := COALESCE(p_end_time, clock_timestamp());
    v_play_amount NUMERIC := 0;
    v_service_amount NUMERIC := 0;
    v_discount_amount NUMERIC := 0;
    v_total_amount NUMERIC := 0;
    v_invoice_id BIGINT;
    v_promo RECORD;
    v_applied_promo_id INT := NULL;
    v_affordable_play NUMERIC;
BEGIN
    SELECT * INTO v_session
    FROM core.gaming_session
    WHERE session_id = p_session_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiên chơi ID % không tồn tại!', p_session_id;
    END IF;
    IF v_session.status <> 'active' THEN
        RAISE EXCEPTION 'Phiên chơi ID % đã đóng (trạng thái: %)!', p_session_id, v_session.status;
    END IF;
    IF v_end_time <= v_session.start_time THEN
        v_end_time := v_session.start_time + INTERVAL '1 second';
    END IF;

    SELECT * INTO v_customer
    FROM core.customer
    WHERE customer_id = v_session.customer_id
    FOR UPDATE;

    v_play_amount := core.fn_calc_session_amount(
        v_session.start_time, v_end_time, v_session.hourly_rate_applied
    );
    IF v_play_amount = 0 THEN
        v_play_amount := ROUND((15.0 / 60.0) * v_session.hourly_rate_applied);
    END IF;

    SELECT COALESCE(SUM(fod.quantity * fod.unit_price), 0)
    INTO v_service_amount
    FROM inventory.food_order fo
    JOIN inventory.food_order_detail fod ON fod.order_id = fo.order_id
    WHERE fo.session_id = p_session_id AND fo.status <> 'cancelled';

    IF p_promo_code IS NOT NULL AND p_promo_code <> '' THEN
        SELECT * INTO v_promo
        FROM billing.promotion
        WHERE code = p_promo_code
          AND is_active = TRUE
          AND v_end_time BETWEEN valid_from AND valid_to;
        IF FOUND THEN
            v_applied_promo_id := v_promo.promotion_id;
            IF v_promo.promo_type IN ('percent', 'happy_hour') THEN
                v_discount_amount := ROUND((v_play_amount * v_promo.value) / 100.0);
            ELSIF v_promo.promo_type = 'fixed' THEN
                v_discount_amount := LEAST(v_promo.value, v_play_amount);
            END IF;
        END IF;
    END IF;

    v_total_amount := GREATEST(0, v_play_amount + v_service_amount - v_discount_amount);

    IF p_payment_method = 'balance' AND v_customer.balance < v_total_amount THEN
        IF p_close_reason IN ('balance_exhausted', 'stale_timeout', 'station_auto')
           AND v_customer.balance >= v_service_amount THEN
            v_affordable_play := GREATEST(0, v_customer.balance - v_service_amount);
            v_play_amount := LEAST(v_play_amount, v_affordable_play);
            IF v_session.hourly_rate_applied > 0 THEN
                v_end_time := v_session.start_time + make_interval(
                    secs => (v_play_amount / v_session.hourly_rate_applied * 3600)::DOUBLE PRECISION
                );
            END IF;
            IF v_end_time <= v_session.start_time THEN
                v_end_time := v_session.start_time + INTERVAL '1 second';
            END IF;
            v_discount_amount := 0;
            v_total_amount := v_play_amount + v_service_amount;
        ELSE
            RAISE EXCEPTION 'Số dư không đủ: còn %, cần %!', v_customer.balance, v_total_amount;
        END IF;
    END IF;

    IF p_payment_method = 'balance' THEN
        UPDATE core.customer
        SET balance = balance - v_total_amount,
            loyalty_points = loyalty_points + FLOOR(v_total_amount / 10000),
            membership_tier = core.fn_membership_tier((loyalty_points + FLOOR(v_total_amount / 10000))::INT)
        WHERE customer_id = v_session.customer_id;
    ELSE
        UPDATE core.customer
        SET loyalty_points = loyalty_points + FLOOR(v_total_amount / 10000),
            membership_tier = core.fn_membership_tier((loyalty_points + FLOOR(v_total_amount / 10000))::INT)
        WHERE customer_id = v_session.customer_id;
    END IF;

    UPDATE core.gaming_session
    SET end_time = v_end_time,
        status = 'finished',
        play_amount = v_play_amount,
        last_heartbeat_at = clock_timestamp(),
        close_reason = p_close_reason
    WHERE session_id = p_session_id;

    INSERT INTO billing.invoice (
        session_id, customer_id, cashier_id, play_amount, service_amount,
        surcharge, discount_amount, total_amount, status, issued_at, paid_at
    ) VALUES (
        p_session_id, v_session.customer_id, p_cashier_id, v_play_amount, v_service_amount,
        0, v_discount_amount, v_total_amount, 'paid', v_end_time, v_end_time
    ) RETURNING invoice_id INTO v_invoice_id;

    IF v_discount_amount > 0 AND v_applied_promo_id IS NOT NULL THEN
        INSERT INTO billing.invoice_promotion (invoice_id, promotion_id, discount_amount)
        VALUES (v_invoice_id, v_applied_promo_id, v_discount_amount);
    END IF;

    INSERT INTO billing.payment (
        kind, invoice_id, customer_id, method, amount, employee_id, paid_at, reference
    ) VALUES (
        'invoice', v_invoice_id, v_session.customer_id, p_payment_method,
        v_total_amount, p_cashier_id, v_end_time,
        'Thanh toán phiên #' || p_session_id || ' (' || p_close_reason || ')'
    );

    RETURN v_invoice_id;
END;
$$;

CREATE OR REPLACE FUNCTION billing.sp_close_session_and_pay(
    p_session_id BIGINT,
    p_cashier_id INT,
    p_payment_method VARCHAR(20) DEFAULT 'balance',
    p_promo_code VARCHAR(30) DEFAULT NULL
) RETURNS BIGINT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
BEGIN
    RETURN billing.sp_close_session_and_pay_at(
        p_session_id, p_cashier_id, p_payment_method, p_promo_code,
        clock_timestamp(), 'admin_close'
    );
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_customer_login(
    p_username VARCHAR(50),
    p_password TEXT,
    p_computer_code VARCHAR(20)
) RETURNS TABLE (
    session_id BIGINT,
    customer_id INT,
    computer_id INT,
    computer_code VARCHAR(20),
    customer_name VARCHAR(100),
    balance NUMERIC,
    hourly_rate NUMERIC,
    start_time TIMESTAMPTZ
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_customer core.customer%ROWTYPE;
    v_computer core.computer%ROWTYPE;
    v_rate NUMERIC;
    v_session_id BIGINT;
    v_service_employee INT;
BEGIN
    SELECT * INTO v_customer
    FROM core.customer AS c
    WHERE c.username = p_username
      AND c.is_active = TRUE
      AND c.password_hash = public.crypt(p_password, c.password_hash::TEXT)
    FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Sai tài khoản hoặc mật khẩu!';
    END IF;
    IF v_customer.balance <= 0 THEN
        RAISE EXCEPTION 'Tài khoản không đủ số dư để mở phiên!';
    END IF;
    IF EXISTS (
        SELECT 1 FROM core.gaming_session AS s
        WHERE s.customer_id = v_customer.customer_id AND s.status = 'active'
    ) THEN
        RAISE EXCEPTION 'Khách hàng đang có phiên hoạt động!';
    END IF;

    SELECT * INTO v_computer
    FROM core.computer c
    WHERE c.computer_code = p_computer_code
    FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Máy % không tồn tại!', p_computer_code;
    END IF;
    SELECT ct.hourly_rate INTO v_rate
    FROM core.computer_type AS ct
    WHERE ct.type_id = v_computer.type_id;
    IF NOT FOUND OR v_computer.status <> 'available' THEN
        RAISE EXCEPTION 'Máy % không sẵn sàng!', p_computer_code;
    END IF;

    v_service_employee := core.fn_station_service_employee_id();
    INSERT INTO core.gaming_session (
        customer_id, computer_id, opened_by, start_time, status,
        hourly_rate_applied, last_heartbeat_at, close_reason
    ) VALUES (
        v_customer.customer_id, v_computer.computer_id, v_service_employee,
        clock_timestamp(), 'active', v_rate, clock_timestamp(), NULL
    ) RETURNING core.gaming_session.session_id INTO v_session_id;

    RETURN QUERY
    SELECT v_session_id, v_customer.customer_id, v_computer.computer_id,
           v_computer.computer_code, v_customer.full_name, v_customer.balance,
           v_rate, clock_timestamp();
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_station_heartbeat(
    p_session_id BIGINT,
    p_customer_id INT,
    p_computer_id INT
) RETURNS TABLE (
    session_id BIGINT,
    balance_remaining NUMERIC,
    projected_play_amount NUMERIC,
    projected_service_amount NUMERIC,
    projected_total_amount NUMERIC,
    last_heartbeat_at TIMESTAMPTZ,
    session_status VARCHAR(20)
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_session core.gaming_session%ROWTYPE;
    v_balance NUMERIC;
    v_play NUMERIC;
    v_service NUMERIC;
    v_total NUMERIC;
    v_heartbeat TIMESTAMPTZ := clock_timestamp();
    v_status VARCHAR(20) := 'active';
BEGIN
    SELECT * INTO v_session
    FROM core.gaming_session AS s
    WHERE s.session_id = p_session_id
      AND s.customer_id = p_customer_id
      AND s.computer_id = p_computer_id
      AND s.status = 'active'
    FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiên station không hợp lệ hoặc đã đóng!';
    END IF;

    UPDATE core.gaming_session AS gs
    SET last_heartbeat_at = v_heartbeat
    WHERE gs.session_id = p_session_id;

    SELECT c.balance INTO v_balance FROM core.customer AS c WHERE c.customer_id = p_customer_id;
    v_play := core.fn_calc_session_amount(v_session.start_time, v_heartbeat, v_session.hourly_rate_applied);
    SELECT COALESCE(SUM(d.quantity * d.unit_price), 0) INTO v_service
    FROM inventory.food_order o
    JOIN inventory.food_order_detail d ON d.order_id = o.order_id
    WHERE o.session_id = p_session_id AND o.status <> 'cancelled';
    v_total := v_play + v_service;

    IF v_balance <= v_total AND v_total > 0 THEN
        PERFORM billing.sp_close_session_and_pay_at(
            p_session_id, core.fn_station_service_employee_id(), 'balance', NULL,
            v_heartbeat, 'balance_exhausted'
        );
        v_status := 'finished';
        v_balance := 0;
    END IF;

    RETURN QUERY SELECT p_session_id, v_balance, v_play, v_service, v_total, v_heartbeat, v_status;
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_station_logout(
    p_session_id BIGINT,
    p_customer_id INT,
    p_computer_id INT
) RETURNS BIGINT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_valid BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 FROM core.gaming_session AS s
        WHERE s.session_id = p_session_id AND s.customer_id = p_customer_id
          AND s.computer_id = p_computer_id AND s.status = 'active'
    ) INTO v_valid;
    IF NOT v_valid THEN
        RAISE EXCEPTION 'Phiên station không hợp lệ hoặc đã đóng!';
    END IF;
    RETURN billing.sp_close_session_and_pay_at(
        p_session_id, core.fn_station_service_employee_id(), 'balance', NULL,
        clock_timestamp(), 'station_logout'
    );
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_reconcile_stale_sessions(
    p_timeout_seconds INT DEFAULT 90
) RETURNS TABLE (session_id BIGINT, invoice_id BIGINT, close_reason VARCHAR(40))
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_session RECORD;
    v_invoice BIGINT;
BEGIN
    FOR v_session IN
        SELECT s.session_id
        FROM core.gaming_session AS s
        WHERE s.status = 'active'
          AND s.last_heartbeat_at < clock_timestamp() - make_interval(secs => GREATEST(1, p_timeout_seconds))
        FOR UPDATE SKIP LOCKED
    LOOP
        v_invoice := billing.sp_close_session_and_pay_at(
            v_session.session_id, core.fn_station_service_employee_id(), 'balance', NULL,
            clock_timestamp(), 'stale_timeout'
        );
        session_id := v_session.session_id;
        invoice_id := v_invoice;
        close_reason := 'stale_timeout';
        RETURN NEXT;
    END LOOP;
END;
$$;

CREATE OR REPLACE FUNCTION inventory.sp_customer_place_order(
    p_session_id BIGINT,
    p_customer_id INT,
    p_product_id INT,
    p_quantity INT
) RETURNS BIGINT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_session core.gaming_session%ROWTYPE;
    v_product inventory.product%ROWTYPE;
    v_balance NUMERIC;
    v_play NUMERIC;
    v_service NUMERIC;
    v_order_id BIGINT;
    v_employee INT := core.fn_station_service_employee_id();
BEGIN
    IF p_quantity <= 0 THEN
        RAISE EXCEPTION 'Số lượng món phải lớn hơn 0!';
    END IF;
    SELECT * INTO v_session
    FROM core.gaming_session AS s
    WHERE s.session_id = p_session_id AND s.customer_id = p_customer_id AND s.status = 'active'
    FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiên station không hợp lệ!';
    END IF;
    SELECT * INTO v_product FROM inventory.product AS p WHERE p.product_id = p_product_id AND p.is_active = TRUE FOR UPDATE;
    IF NOT FOUND OR v_product.stock_qty < p_quantity THEN
        RAISE EXCEPTION 'Sản phẩm không tồn tại hoặc không đủ tồn kho!';
    END IF;
    SELECT c.balance INTO v_balance FROM core.customer AS c WHERE c.customer_id = p_customer_id FOR UPDATE;
    v_play := core.fn_calc_session_amount(v_session.start_time, clock_timestamp(), v_session.hourly_rate_applied);
    SELECT COALESCE(SUM(d.quantity * d.unit_price), 0) INTO v_service
    FROM inventory.food_order o
    JOIN inventory.food_order_detail d ON d.order_id = o.order_id
    WHERE o.session_id = p_session_id AND o.status <> 'cancelled';
    IF v_balance < v_play + v_service + (v_product.unit_price * p_quantity) THEN
        RAISE EXCEPTION 'Đơn hàng vượt số dư khả dụng!';
    END IF;

    INSERT INTO inventory.food_order (session_id, employee_id, status)
    VALUES (p_session_id, v_employee, 'placed')
    RETURNING order_id INTO v_order_id;
    INSERT INTO inventory.food_order_detail (order_id, product_id, quantity, unit_price)
    VALUES (v_order_id, p_product_id, p_quantity, v_product.unit_price);
    RETURN v_order_id;
END;
$$;

CREATE OR REPLACE FUNCTION inventory.fn_customer_order_status(
    p_session_id BIGINT,
    p_customer_id INT
) RETURNS TABLE (
    order_id BIGINT,
    status VARCHAR(20),
    ordered_at TIMESTAMPTZ,
    total_amount NUMERIC
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory
AS $$
    SELECT o.order_id, o.status, o.ordered_at,
           COALESCE(SUM(d.quantity * d.unit_price), 0) AS total_amount
    FROM inventory.food_order o
    JOIN inventory.food_order_detail d ON d.order_id = o.order_id
    JOIN core.gaming_session s ON s.session_id = o.session_id
    WHERE o.session_id = p_session_id AND s.customer_id = p_customer_id
    GROUP BY o.order_id, o.status, o.ordered_at
    ORDER BY o.order_id DESC;
$$;

CREATE OR REPLACE FUNCTION inventory.fn_customer_menu()
RETURNS TABLE (
    product_id INT,
    product_name VARCHAR(100),
    unit_price NUMERIC,
    stock_qty INT
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, inventory
AS $$
    SELECT p.product_id, p.product_name, p.unit_price, p.stock_qty
    FROM inventory.product p
    WHERE p.is_active = TRUE
    ORDER BY p.product_name;
$$;

-- Trả SKU để Station chọn đúng asset ảnh; function cũ vẫn giữ nguyên API tương thích.
CREATE OR REPLACE FUNCTION inventory.fn_customer_menu_v2()
RETURNS TABLE (
    product_id INT,
    sku VARCHAR(30),
    product_name VARCHAR(100),
    unit_price NUMERIC,
    stock_qty INT
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, inventory
AS $$
    SELECT p.product_id, p.sku, p.product_name, p.unit_price, p.stock_qty
    FROM inventory.product AS p
    WHERE p.is_active = TRUE
    ORDER BY p.product_id;
$$;

CREATE OR REPLACE FUNCTION core.sp_create_service_request(
    p_session_id BIGINT,
    p_customer_id INT,
    p_computer_id INT,
    p_type VARCHAR(30),
    p_message TEXT
) RETURNS BIGINT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, audit, ai
AS $$
DECLARE
    v_id BIGINT;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM core.gaming_session AS s
        WHERE s.session_id = p_session_id AND s.customer_id = p_customer_id
          AND s.computer_id = p_computer_id AND s.status = 'active'
    ) THEN
        RAISE EXCEPTION 'Phiên station không hợp lệ!';
    END IF;
    INSERT INTO core.service_request (session_id, customer_id, computer_id, request_type, message)
    VALUES (p_session_id, p_customer_id, p_computer_id, p_type, trim(p_message))
    RETURNING service_request_id INTO v_id;
    RETURN v_id;
END;
$$;

CREATE OR REPLACE FUNCTION core.fn_notify_computer_state()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core
AS $$
BEGIN
    PERFORM pg_notify('computer_state_changed', COALESCE(NEW.computer_id, OLD.computer_id)::TEXT);
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_notify_computer_state ON core.gaming_session;
CREATE TRIGGER trg_notify_computer_state
    AFTER INSERT OR UPDATE ON core.gaming_session
    FOR EACH ROW
    EXECUTE FUNCTION core.fn_notify_computer_state();

CREATE OR REPLACE FUNCTION core.fn_notify_support_queue()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core
AS $$
BEGIN
    PERFORM pg_notify('support_queue_changed', COALESCE(NEW.service_request_id, OLD.service_request_id)::TEXT);
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_notify_support_queue ON core.service_request;
CREATE TRIGGER trg_notify_support_queue
    AFTER INSERT OR UPDATE ON core.service_request
    FOR EACH ROW
    EXECUTE FUNCTION core.fn_notify_support_queue();

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'role_station') THEN
        CREATE ROLE role_station NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'station_runtime') THEN
        CREATE ROLE station_runtime LOGIN IN ROLE role_station;
    END IF;
END $$;

GRANT USAGE ON SCHEMA core, inventory, billing TO role_station;
REVOKE ALL ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai, reporting, staging FROM role_station;
GRANT EXECUTE ON FUNCTION core.sp_customer_login(VARCHAR, TEXT, VARCHAR) TO role_station;
GRANT EXECUTE ON FUNCTION core.sp_station_heartbeat(BIGINT, INT, INT) TO role_station;
GRANT EXECUTE ON FUNCTION core.sp_station_logout(BIGINT, INT, INT) TO role_station;
GRANT EXECUTE ON FUNCTION inventory.sp_customer_place_order(BIGINT, INT, INT, INT) TO role_station;
GRANT EXECUTE ON FUNCTION inventory.fn_customer_order_status(BIGINT, INT) TO role_station;
GRANT EXECUTE ON FUNCTION inventory.fn_customer_menu() TO role_station;
GRANT EXECUTE ON FUNCTION inventory.fn_customer_menu_v2() TO role_station;
GRANT EXECUTE ON FUNCTION core.sp_create_service_request(BIGINT, INT, INT, VARCHAR, TEXT) TO role_station;

GRANT SELECT, INSERT, UPDATE ON core.service_request TO role_admin, role_manager;
GRANT USAGE, SELECT ON SEQUENCE core.service_request_service_request_id_seq TO role_admin, role_manager;

GRANT EXECUTE ON FUNCTION billing.sp_close_session_and_pay_at(BIGINT, INT, VARCHAR, VARCHAR, TIMESTAMPTZ, VARCHAR)
    TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_reconcile_stale_sessions(INT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION core.fn_station_service_employee_id() TO role_admin, role_manager, role_station;

COMMENT ON TABLE core.service_request IS 'Queue hỗ trợ từ NetCafe.Client; station chỉ ghi qua SECURITY DEFINER function.';
COMMENT ON COLUMN core.gaming_session.last_heartbeat_at IS 'Heartbeat cuối của station hoặc watcher.';
COMMENT ON COLUMN core.gaming_session.close_reason IS 'Lý do đóng phiên: admin_close, station_logout, balance_exhausted, stale_timeout.';
