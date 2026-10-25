-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 01_schema.sql
-- Mục đích: Khởi tạo Extension, Schemas, Bảng, Khóa chính, Khóa ngoại, Ràng buộc
-- NGUYÊN TẮC:
-- 1. Idempotent: Chạy nhiều lần không lỗi (DROP ... CASCADE có kiểm soát).
-- 2. Tách bạch tối ưu: Chỉ tạo PK/UNIQUE/EXCLUDE bắt buộc. KHÔNG tạo index tối ưu ở đây 
--    (index tối ưu nằm ở 05_indexes_partition.sql để phục vụ đo lường trước/sau).
-- 3. Mọi FK dùng ON DELETE RESTRICT để bảo toàn tính toàn vẹn kế toán/lịch sử.
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- 1. KHỞI TẠO CÁC EXTENSION CHUẨN CỦA POSTGRESQL 17
-- ----------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS pgcrypto;      -- Mã hóa mật khẩu, UUID, hàm băm
CREATE EXTENSION IF NOT EXISTS pg_trgm;       -- Tìm kiếm mờ trigram, đo độ tương đồng similarity
CREATE EXTENSION IF NOT EXISTS unaccent;      -- Bỏ dấu tiếng Việt phục vụ tìm kiếm
CREATE EXTENSION IF NOT EXISTS btree_gist;    -- EXCLUDE chống chồng phiên trên cùng máy

-- ----------------------------------------------------------------------------
-- 2. KHỞI TẠO 8 SCHEMAS NGHIỆP VỤ
-- ----------------------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS core;             -- Nghiệp vụ phòng máy, khách hàng, phiên chơi
CREATE SCHEMA IF NOT EXISTS inventory;        -- Dịch vụ F&B, quản lý sản phẩm, đơn gọi món, tồn kho
CREATE SCHEMA IF NOT EXISTS billing;          -- Khuyến mãi, hóa đơn, thanh toán, nạp tiền
CREATE SCHEMA IF NOT EXISTS hr;               -- Nhân sự, tài khoản hệ thống, nhật ký bảo trì
CREATE SCHEMA IF NOT EXISTS audit;            -- Bảng nhật ký kiểm toán phân vùng
CREATE SCHEMA IF NOT EXISTS ai;               -- 5 module tự động hóa thông minh (alert, index, dedup, anomaly, promo)
CREATE SCHEMA IF NOT EXISTS reporting;        -- Views và Materialized Views báo cáo thống kê
CREATE SCHEMA IF NOT EXISTS staging;          -- Bảng thô trung gian phục vụ ETL dữ liệu bẩn

-- ----------------------------------------------------------------------------
-- 3. SCHEMA HR (Nhân sự & Vận hành - Cần tạo trước để bảng core/billing tham chiếu)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS hr.maintenance CASCADE;
DROP TABLE IF EXISTS hr.employee CASCADE;

CREATE TABLE hr.employee (
    employee_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(15) NOT NULL,
    username VARCHAR(50) NOT NULL,
    db_username VARCHAR(50) NULL,
    password_hash VARCHAR(255) NOT NULL,
    emp_role VARCHAR(20) NOT NULL,
    default_shift VARCHAR(20) NOT NULL,
    salary NUMERIC(12,0) NOT NULL DEFAULT 0,
    hired_date DATE NOT NULL DEFAULT CURRENT_DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    -- Ràng buộc duy nhất
    CONSTRAINT uq_employee_username UNIQUE (username),
    CONSTRAINT uq_employee_db_username UNIQUE (db_username),

    -- Ràng buộc kiểm tra CHECK
    CONSTRAINT ck_employee_salary CHECK (salary >= 0),
    CONSTRAINT ck_employee_role CHECK (emp_role IN ('admin', 'manager', 'cashier', 'staff', 'technician')),
    CONSTRAINT ck_employee_shift CHECK (default_shift IN ('morning', 'afternoon', 'night')),
    CONSTRAINT ck_employee_phone CHECK (phone ~ '^0[35789][0-9]{8}$'),
    CONSTRAINT ck_employee_username CHECK (username ~ '^[a-z][a-z0-9_]{3,29}$')
);
COMMENT ON TABLE hr.employee IS 'Bảng nhân viên và tài khoản vận hành, ánh xạ role PostgreSQL qua db_username';
COMMENT ON COLUMN hr.employee.salary IS 'Cột nhạy cảm: Bảo vệ bằng Column-Level Security đối với cashier/staff/technician';

-- ----------------------------------------------------------------------------
-- 4. SCHEMA CORE (Nghiệp vụ phòng máy cốt lõi)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS core.gaming_session CASCADE;
DROP TABLE IF EXISTS core.computer CASCADE;
DROP TABLE IF EXISTS core.computer_type CASCADE;
DROP TABLE IF EXISTS core.room CASCADE;
DROP TABLE IF EXISTS core.customer CASCADE;

-- 4.1. Phòng/Khu vực
CREATE TABLE core.room (
    room_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    room_name VARCHAR(50) NOT NULL,
    description TEXT NULL,
    CONSTRAINT uq_room_name UNIQUE (room_name)
);
COMMENT ON TABLE core.room IS 'Danh mục khu vực / phòng máy (Hút thuốc, VIP, Stream, Thi đấu)';

-- 4.2. Loại máy và đơn giá thuê theo giờ (Chuẩn hóa 3NF)
CREATE TABLE core.computer_type (
    type_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    type_name VARCHAR(50) NOT NULL,
    hourly_rate NUMERIC(12,0) NOT NULL,
    CONSTRAINT uq_computer_type_name UNIQUE (type_name),
    CONSTRAINT ck_computer_type_hourly_rate CHECK (hourly_rate >= 0)
);
COMMENT ON TABLE core.computer_type IS 'Loại cấu hình máy và đơn giá giờ chơi (Đạt 3NF, tránh dị thường cập nhật giá)';

-- 4.3. Máy trạm
CREATE TABLE core.computer (
    computer_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    computer_code VARCHAR(20) NOT NULL,
    computer_name VARCHAR(50) NOT NULL,
    room_id INT NOT NULL REFERENCES core.room(room_id) ON DELETE RESTRICT,
    type_id INT NOT NULL REFERENCES core.computer_type(type_id) ON DELETE RESTRICT,
    specs JSONB NOT NULL DEFAULT '{}'::jsonb,
    ip_address INET NOT NULL,
    mac_address MACADDR NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'available',
    purchased_at DATE NOT NULL DEFAULT CURRENT_DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_computer_code UNIQUE (computer_code),
    CONSTRAINT uq_computer_ip UNIQUE (ip_address),
    CONSTRAINT uq_computer_mac UNIQUE (mac_address),
    CONSTRAINT ck_computer_code_format CHECK (computer_code ~ '^PC[0-9]{3,4}$'),
    CONSTRAINT ck_computer_status CHECK (status IN ('available', 'in_use', 'maintenance', 'broken'))
);
COMMENT ON TABLE core.computer IS 'Danh mục máy trạm, tích hợp kiểm tra IP (INET), MAC (MACADDR) và cấu hình linh hoạt (JSONB)';

-- 4.4. Khách hàng / Hội viên
CREATE TABLE core.customer (
    customer_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    phone VARCHAR(15) NOT NULL,
    email VARCHAR(100) NULL,
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    balance NUMERIC(12,0) NOT NULL DEFAULT 0,
    loyalty_points INT NOT NULL DEFAULT 0,
    membership_tier VARCHAR(20) NOT NULL DEFAULT 'regular',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_customer_phone UNIQUE (phone),
    CONSTRAINT uq_customer_email UNIQUE (email),
    CONSTRAINT uq_customer_username UNIQUE (username),
    CONSTRAINT ck_customer_balance CHECK (balance >= 0),
    CONSTRAINT ck_customer_loyalty_points CHECK (loyalty_points >= 0),
    CONSTRAINT ck_customer_tier CHECK (membership_tier IN ('regular', 'silver', 'gold', 'vip')),
    CONSTRAINT ck_customer_phone CHECK (phone ~ '^0[35789][0-9]{8}$'),
    CONSTRAINT ck_customer_email CHECK (email IS NULL OR email ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'),
    CONSTRAINT ck_customer_username CHECK (username ~ '^[a-z][a-z0-9_]{3,29}$')
);
COMMENT ON TABLE core.customer IS 'Tài khoản hội viên phòng net: số dư không âm, hạng thẻ, tích điểm';

-- 4.5. Phiên chơi máy (Bảng trung tâm của toàn hệ thống)
CREATE TABLE core.gaming_session (
    session_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INT NOT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    computer_id INT NOT NULL REFERENCES core.computer(computer_id) ON DELETE RESTRICT,
    opened_by INT NOT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    start_time TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMPTZ NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    hourly_rate_applied NUMERIC(12,0) NOT NULL,
    play_amount NUMERIC(12,0) NULL,
    game_info JSONB NOT NULL DEFAULT '{}'::jsonb,

    CONSTRAINT ck_session_status CHECK (status IN ('active', 'finished', 'cancelled')),
    CONSTRAINT ck_session_hourly_rate CHECK (hourly_rate_applied >= 0),
    CONSTRAINT ck_session_play_amount CHECK (play_amount IS NULL OR play_amount >= 0),
    CONSTRAINT ck_session_time_order CHECK (end_time IS NULL OR end_time > start_time),
    CONSTRAINT ck_session_status_endtime CHECK (
        (status = 'active' AND end_time IS NULL) OR 
        (status <> 'active' AND end_time IS NOT NULL)
    )
);
COMMENT ON TABLE core.gaming_session IS 'Phiên chơi máy: hourly_rate_applied bảo lưu giá tại thời điểm mở phiên';

ALTER TABLE core.gaming_session
    ADD CONSTRAINT ex_gaming_session_computer_time
    EXCLUDE USING gist (
        computer_id WITH =,
        tstzrange(
            start_time,
            COALESCE(end_time, 'infinity'::timestamptz),
            '[)'
        ) WITH &&
    )
    WHERE (status IN ('active', 'finished'));

-- ----------------------------------------------------------------------------
-- 5. SCHEMA INVENTORY (Quản lý F&B, Tồn kho & Đơn gọi món)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS inventory.food_order_detail CASCADE;
DROP TABLE IF EXISTS inventory.food_order CASCADE;
DROP TABLE IF EXISTS inventory.product CASCADE;
DROP TABLE IF EXISTS inventory.product_category CASCADE;

-- 5.1. Danh mục sản phẩm
CREATE TABLE inventory.product_category (
    category_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    category_name VARCHAR(50) NOT NULL,
    CONSTRAINT uq_product_category_name UNIQUE (category_name)
);
COMMENT ON TABLE inventory.product_category IS 'Danh mục sản phẩm (Đồ ăn, Đồ uống, Thẻ nạp, Snack)';

-- 5.2. Sản phẩm F&B
CREATE TABLE inventory.product (
    product_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sku VARCHAR(30) NOT NULL,
    product_name VARCHAR(100) NOT NULL,
    category_id INT NOT NULL REFERENCES inventory.product_category(category_id) ON DELETE RESTRICT,
    unit_price NUMERIC(12,0) NOT NULL,
    stock_qty INT NOT NULL DEFAULT 0,
    attributes JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT uq_product_sku UNIQUE (sku),
    CONSTRAINT ck_product_unit_price CHECK (unit_price >= 0),
    CONSTRAINT ck_product_stock_qty CHECK (stock_qty >= 0)
);
COMMENT ON TABLE inventory.product IS 'Danh mục sản phẩm bán tại quán net: stock_qty >= 0 chặn bán lố kho';

-- 5.3. Đơn gọi món
CREATE TABLE inventory.food_order (
    order_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    session_id BIGINT NOT NULL REFERENCES core.gaming_session(session_id) ON DELETE RESTRICT,
    employee_id INT NOT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    ordered_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'placed',

    CONSTRAINT ck_food_order_status CHECK (status IN ('placed', 'served', 'cancelled'))
);
COMMENT ON TABLE inventory.food_order IS 'Đơn đặt đồ ăn uống gắn với phiên chơi đang hoạt động';

-- 5.4. Chi tiết đơn gọi món (Quan hệ N-N giữa food_order và product)
CREATE TABLE inventory.food_order_detail (
    order_id BIGINT NOT NULL REFERENCES inventory.food_order(order_id) ON DELETE RESTRICT,
    product_id INT NOT NULL REFERENCES inventory.product(product_id) ON DELETE RESTRICT,
    quantity INT NOT NULL,
    unit_price NUMERIC(12,0) NOT NULL,

    PRIMARY KEY (order_id, product_id),
    CONSTRAINT ck_order_detail_quantity CHECK (quantity > 0),
    CONSTRAINT ck_order_detail_price CHECK (unit_price >= 0)
);
COMMENT ON TABLE inventory.food_order_detail IS 'Chi tiết đơn đặt món: Chốt đơn giá unit_price tại thời điểm gọi món';

-- ----------------------------------------------------------------------------
-- 6. SCHEMA BILLING (Khuyến mãi, Hóa đơn, Thanh toán)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS billing.payment CASCADE;
DROP TABLE IF EXISTS billing.invoice_promotion CASCADE;
DROP TABLE IF EXISTS billing.invoice CASCADE;
DROP TABLE IF EXISTS billing.promotion CASCADE;

-- 6.1. Khuyến mãi
CREATE TABLE billing.promotion (
    promotion_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code VARCHAR(30) NOT NULL,
    promotion_name VARCHAR(100) NOT NULL,
    promo_type VARCHAR(20) NOT NULL,
    value NUMERIC(12,2) NOT NULL,
    valid_from TIMESTAMPTZ NOT NULL,
    valid_to TIMESTAMPTZ NOT NULL,
    conditions JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT uq_promotion_code UNIQUE (code),
    CONSTRAINT ck_promotion_type CHECK (promo_type IN ('percent', 'fixed', 'free_hours', 'happy_hour')),
    CONSTRAINT ck_promotion_value CHECK (value >= 0),
    CONSTRAINT ck_promotion_dates CHECK (valid_to >= valid_from)
);
COMMENT ON TABLE billing.promotion IS 'Chương trình ưu đãi: conditions JSONB lưu các điều kiện động linh hoạt';

-- 6.2. Hóa đơn (Quan hệ 1-1 với phiên chơi)
CREATE TABLE billing.invoice (
    invoice_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    session_id BIGINT NOT NULL,
    customer_id INT NOT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    cashier_id INT NOT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    play_amount NUMERIC(12,0) NOT NULL DEFAULT 0,
    service_amount NUMERIC(12,0) NOT NULL DEFAULT 0,
    surcharge NUMERIC(12,0) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(12,0) NOT NULL DEFAULT 0,
    total_amount NUMERIC(12,0) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'unpaid',
    issued_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    paid_at TIMESTAMPTZ NULL,

    CONSTRAINT uq_invoice_session UNIQUE (session_id),
    CONSTRAINT fk_invoice_session FOREIGN KEY (session_id) 
        REFERENCES core.gaming_session(session_id) ON DELETE RESTRICT,
    CONSTRAINT ck_invoice_status CHECK (status IN ('unpaid', 'paid', 'void')),
    CONSTRAINT ck_invoice_play_amount CHECK (play_amount >= 0),
    CONSTRAINT ck_invoice_service_amount CHECK (service_amount >= 0),
    CONSTRAINT ck_invoice_surcharge CHECK (surcharge >= 0),
    CONSTRAINT ck_invoice_discount_amount CHECK (discount_amount >= 0),
    CONSTRAINT ck_invoice_total_amount CHECK (total_amount >= 0),
    CONSTRAINT ck_invoice_formula CHECK (
        total_amount = (play_amount + service_amount + surcharge - discount_amount)
    )
);
COMMENT ON TABLE billing.invoice IS 'Hóa đơn tổng kết phiên: 1-1 với session_id, công thức tổng tiền bất biến';

-- 6.3. Chi tiết mã khuyến mãi áp dụng trên hóa đơn (Quan hệ N-N)
CREATE TABLE billing.invoice_promotion (
    invoice_id BIGINT NOT NULL REFERENCES billing.invoice(invoice_id) ON DELETE RESTRICT,
    promotion_id INT NOT NULL REFERENCES billing.promotion(promotion_id) ON DELETE RESTRICT,
    discount_amount NUMERIC(12,0) NOT NULL DEFAULT 0,

    PRIMARY KEY (invoice_id, promotion_id),
    CONSTRAINT ck_invoice_promo_discount CHECK (discount_amount >= 0)
);
COMMENT ON TABLE billing.invoice_promotion IS 'Liên kết N-N: một hóa đơn có thể áp dụng đồng thời nhiều khuyến mãi';

-- 6.4. Giao dịch thanh toán (Thanh toán hóa đơn hoặc Nạp tiền tài khoản)
CREATE TABLE billing.payment (
    payment_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    kind VARCHAR(20) NOT NULL,
    invoice_id BIGINT NULL REFERENCES billing.invoice(invoice_id) ON DELETE RESTRICT,
    customer_id INT NOT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    method VARCHAR(20) NOT NULL,
    amount NUMERIC(12,0) NOT NULL,
    employee_id INT NOT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    paid_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reference VARCHAR(100) NULL,

    CONSTRAINT ck_payment_kind CHECK (kind IN ('invoice', 'topup')),
    CONSTRAINT ck_payment_method CHECK (method IN ('balance', 'cash', 'bank_transfer', 'card')),
    CONSTRAINT ck_payment_amount CHECK (amount > 0),
    CONSTRAINT ck_payment_kind_invoice_match CHECK (
        (kind = 'invoice' AND invoice_id IS NOT NULL) OR
        (kind = 'topup' AND invoice_id IS NULL)
    )
);
COMMENT ON TABLE billing.payment IS 'Sổ quỹ thanh toán: Quản lý dòng tiền vào (hóa đơn hoặc nạp thẻ hội viên)';

-- ----------------------------------------------------------------------------
-- 7. SCHEMA HR (Bảo trì máy trạm)
-- ----------------------------------------------------------------------------

CREATE TABLE hr.maintenance (
    maintenance_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    computer_id INT NOT NULL REFERENCES core.computer(computer_id) ON DELETE RESTRICT,
    technician_id INT NOT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    issue_description TEXT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    finished_at TIMESTAMPTZ NULL,
    cost NUMERIC(12,0) NOT NULL DEFAULT 0,
    parts_replaced JSONB NOT NULL DEFAULT '[]'::jsonb,
    status VARCHAR(20) NOT NULL DEFAULT 'reported',

    CONSTRAINT ck_maint_cost CHECK (cost >= 0),
    CONSTRAINT ck_maint_status CHECK (status IN ('reported', 'in_progress', 'fixed', 'unrepairable')),
    CONSTRAINT ck_maint_time_order CHECK (finished_at IS NULL OR finished_at >= started_at)
);
COMMENT ON TABLE hr.maintenance IS 'Lịch sử bảo trì phần cứng: Lưu trữ linh kiện thay thế qua JSONB';

-- ----------------------------------------------------------------------------
-- 8. SCHEMA AUDIT (Nhật ký kiểm toán an ninh - Partition theo tháng)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS audit.audit_log CASCADE;

CREATE TABLE audit.audit_log (
    audit_id BIGINT GENERATED ALWAYS AS IDENTITY,
    logged_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),
    actor VARCHAR(50) NOT NULL,
    table_name VARCHAR(50) NOT NULL,
    action VARCHAR(10) NOT NULL,
    row_pk VARCHAR(50) NOT NULL,
    old_data JSONB NULL,
    new_data JSONB NULL,

    PRIMARY KEY (audit_id, logged_at),
    CONSTRAINT ck_audit_action CHECK (action IN ('INSERT', 'UPDATE', 'DELETE'))
) PARTITION BY RANGE (logged_at);

COMMENT ON TABLE audit.audit_log IS 'Bảng kiểm toán bảo mật: PARTITION BY RANGE theo logged_at';

-- Tạo các partition mặc định và partition thời gian
CREATE TABLE audit.audit_log_default PARTITION OF audit.audit_log DEFAULT;

CREATE TABLE audit.audit_log_2026_09 PARTITION OF audit.audit_log
    FOR VALUES FROM ('2026-09-01 00:00:00+00') TO ('2026-10-01 00:00:00+00');

CREATE TABLE audit.audit_log_2026_10 PARTITION OF audit.audit_log
    FOR VALUES FROM ('2026-10-01 00:00:00+00') TO ('2026-11-01 00:00:00+00');

CREATE TABLE audit.audit_log_2026_11 PARTITION OF audit.audit_log
    FOR VALUES FROM ('2026-11-01 00:00:00+00') TO ('2026-12-01 00:00:00+00');

CREATE TABLE audit.audit_log_2026_12 PARTITION OF audit.audit_log
    FOR VALUES FROM ('2026-12-01 00:00:00+00') TO ('2027-01-01 00:00:00+00');

-- ----------------------------------------------------------------------------
-- 9. SCHEMA AI (5 Module Tự động hóa thông minh trong PostgreSQL)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS ai.promotion_suggestion CASCADE;
DROP TABLE IF EXISTS ai.anomaly_flag CASCADE;
DROP TABLE IF EXISTS ai.duplicate_candidate CASCADE;
DROP TABLE IF EXISTS ai.index_recommendation CASCADE;
DROP TABLE IF EXISTS ai.ai_alert CASCADE;

-- 9.1. Module 1: Cảnh báo bảo mật bất thường (Audit risk scoring)
CREATE TABLE ai.ai_alert (
    alert_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    audit_id BIGINT NOT NULL,
    audit_logged_at TIMESTAMPTZ NOT NULL,
    risk_score INT NOT NULL,
    risk_level VARCHAR(10) NOT NULL,
    reason TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'new',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT ck_alert_score CHECK (risk_score BETWEEN 0 AND 100),
    CONSTRAINT ck_alert_level CHECK (risk_level IN ('low', 'medium', 'high')),
    CONSTRAINT ck_alert_status CHECK (status IN ('new', 'reviewed'))
);
COMMENT ON TABLE ai.ai_alert IS 'Module AI 1: Điểm rủi ro và cảnh báo bảo mật sinh ra từ Audit trigger';

-- 9.2. Module 2: Tư vấn index tự động
CREATE TABLE ai.index_recommendation (
    rec_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    column_list VARCHAR(255) NOT NULL,
    reason TEXT NOT NULL,
    suggested_sql TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
COMMENT ON TABLE ai.index_recommendation IS 'Module AI 2: Gợi ý tạo chỉ mục từ phân tích thống kê pg_stat_user_tables';

-- 9.3. Module 3: Tự chuẩn hóa & Phát hiện khách hàng trùng lặp
CREATE TABLE ai.duplicate_candidate (
    cand_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id_a INT NOT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    customer_id_b INT NOT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    similarity NUMERIC(4,3) NOT NULL,
    reason TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT ck_dup_cand_order CHECK (customer_id_a < customer_id_b),
    CONSTRAINT ck_dup_cand_sim CHECK (similarity >= 0 AND similarity <= 1),
    CONSTRAINT ck_dup_cand_status CHECK (status IN ('pending', 'merged', 'ignored'))
);
COMMENT ON TABLE ai.duplicate_candidate IS 'Module AI 3: Cặp tài khoản khách nghi vấn trùng lặp phát hiện qua pg_trgm';

-- 9.4. Module 4: Phát hiện bất thường thống kê (Z-score / IQR)
CREATE TABLE ai.anomaly_flag (
    flag_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    anomaly_type VARCHAR(50) NOT NULL,
    subject_table VARCHAR(50) NOT NULL,
    subject_pk VARCHAR(50) NOT NULL,
    metric_value NUMERIC(15,2) NOT NULL,
    threshold_info JSONB NOT NULL,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'open',

    CONSTRAINT ck_anom_status CHECK (status IN ('open', 'resolved'))
);
COMMENT ON TABLE ai.anomaly_flag IS 'Module AI 4: Gắn cờ các phiên/giao dịch bất thường theo phương sai thống kê';

-- 9.5. Module 5: Gợi ý khuyến mãi & Dự báo giờ cao điểm
CREATE TABLE ai.promotion_suggestion (
    sug_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    suggestion_type VARCHAR(50) NOT NULL,
    target VARCHAR(100) NOT NULL,
    details JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
COMMENT ON TABLE ai.promotion_suggestion IS 'Module AI 5: Đề xuất chiến dịch ưu đãi Happy Hour và phân khúc RFM';

-- ----------------------------------------------------------------------------
-- 10. SCHEMA STAGING (Bảng thô phục vụ ETL dữ liệu bẩn)
-- ----------------------------------------------------------------------------

DROP TABLE IF EXISTS staging.rejected_rows CASCADE;
DROP TABLE IF EXISTS staging.stg_customers CASCADE;

CREATE TABLE staging.stg_customers (
    raw_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    raw_full_name TEXT NULL,
    raw_phone TEXT NULL,
    raw_email TEXT NULL,
    raw_username TEXT NULL,
    raw_balance TEXT NULL,
    raw_points TEXT NULL,
    raw_tier TEXT NULL,
    source_batch VARCHAR(50) NULL,
    loaded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
COMMENT ON TABLE staging.stg_customers IS 'Bảng chứa dữ liệu khách hàng thô chưa chuẩn hóa từ Python generator';

CREATE TABLE staging.rejected_rows (
    rejected_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_table VARCHAR(50) NOT NULL,
    raw_record JSONB NOT NULL,
    error_reason TEXT NOT NULL,
    rejected_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
COMMENT ON TABLE staging.rejected_rows IS 'Bảng lưu các dòng dữ liệu bẩn không thể cứu vớt trong quá trình ETL làm sạch';
