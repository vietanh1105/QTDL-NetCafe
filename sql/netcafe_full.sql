-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: netcafe_full.sql (TẬP LỆNH TỔNG HỢP TOÀN BỘ HỆ THỐNG)
-- Mô tả: File duy nhất cho phép giáo viên tái tạo toàn bộ hệ thống từ một máy chủ trống
--        Bao gồm: Database -> Schemas -> Tables -> Contraints -> Functions/Triggers 
--                 -> Views -> Security (RBAC/RLS) -> Indexes & Partition -> Seed Data -> 5 AI Modules
-- Liên kết: các file SQL nguồn trong SQL_FILES.
-- Đây là script TÁI TẠO DỮ LIỆU DEMO, không phải migration giữ dữ liệu.
-- Chỉ chạy trên database demo riêng; bản full có DROP DATABASE và lệnh psql.
-- ============================================================================



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/00_create_database.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 00_create_database.sql
-- Mục đích: Khởi tạo Database độc lập netcafe_db không phụ thuộc locale HĐH
-- Lưu ý: Chạy file này khi đang kết nối tới Database 'postgres' bằng user postgres (Superuser)
-- ============================================================================

-- 1. Đóng kết nối hiện có tới netcafe_db nếu đang tồn tại
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'netcafe_db' AND pid <> pg_backend_pid();

-- 2. Xóa database cũ nếu đã tồn tại để đảm bảo tính tái lập (idempotent)
DROP DATABASE IF EXISTS netcafe_db;

-- 3. Tạo database mới chuẩn hóa UTF-8 từ template0
-- Giải thích thiết kế:
-- - TEMPLATE template0 + ENCODING 'UTF8': Đảm bảo CSDL hoàn toàn độc lập với Locale
--   và bảng mã của máy chủ cài đặt (đặc biệt khi chấm trên môi trường Windows hoặc Linux khác nhau).
-- - LC_COLLATE = 'C', LC_CTYPE = 'C': Tránh phụ thuộc vào collation tiếng Việt của OS,
--   việc tìm kiếm không dấu và so khớp chuỗi sẽ được giải quyết triệt để thông qua extension 'unaccent' và 'pg_trgm'.
CREATE DATABASE netcafe_db
    WITH
    TEMPLATE = template0
    ENCODING = 'UTF8'
    LC_COLLATE = 'C'
    LC_CTYPE = 'C'
    CONNECTION LIMIT = -1;

COMMENT ON DATABASE netcafe_db IS 'Database quản lý phòng game/net - Đề tài môn Quản trị dữ liệu';



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/01_schema.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

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
    CONSTRAINT ck_employee_role CHECK (emp_role IN ('admin', 'manager', 'cashier')),
    CONSTRAINT ck_employee_shift CHECK (default_shift IN ('morning', 'afternoon', 'night')),
    CONSTRAINT ck_employee_phone CHECK (phone ~ '^0[35789][0-9]{8}$'),
    CONSTRAINT ck_employee_username CHECK (username ~ '^[a-z][a-z0-9_]{3,29}$')
);
COMMENT ON TABLE hr.employee IS 'Bảng nhân viên và tài khoản vận hành, ánh xạ role PostgreSQL qua db_username';
COMMENT ON COLUMN hr.employee.salary IS 'Cột nhạy cảm: Bảo vệ bằng Column-Level Security đối với manager/cashier';

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
    last_heartbeat_at TIMESTAMPTZ NULL,
    close_reason VARCHAR(40) NULL,

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

CREATE TABLE IF NOT EXISTS core.service_request (
    service_request_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    session_id BIGINT NULL REFERENCES core.gaming_session(session_id) ON DELETE RESTRICT,
    customer_id INT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    computer_id INT NOT NULL REFERENCES core.computer(computer_id) ON DELETE RESTRICT,
    request_type VARCHAR(30) NOT NULL DEFAULT 'general',
    message TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'new',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    acknowledged_at TIMESTAMPTZ NULL,
    resolved_at TIMESTAMPTZ NULL,
    handled_by INT NULL REFERENCES hr.employee(employee_id) ON DELETE RESTRICT,
    CONSTRAINT ck_service_request_type CHECK (request_type IN ('general', 'technical', 'food', 'payment')),
    CONSTRAINT ck_service_request_status CHECK (status IN ('new', 'acknowledged', 'resolved', 'cancelled')),
    CONSTRAINT ck_service_request_message CHECK (length(trim(message)) BETWEEN 1 AND 1000)
);
COMMENT ON TABLE core.service_request IS 'Queue hỗ trợ từ NetCafe.Client; station chỉ ghi qua SECURITY DEFINER function.';

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



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/02_functions_triggers.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

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

-- Kiểm tra tác nhân trước khi nâng quyền ghi; SET ROLE vẫn nhận diện đúng login nghiệp vụ.
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
REVOKE ALL ON FUNCTION hr.fn_assert_employee_actor(INT) FROM PUBLIC;

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
COMMENT ON FUNCTION inventory.sp_place_order IS 'Nghiệp vụ gọi món F&B: Kiểm tra tồn kho, tạo đơn và trừ kho';



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/03_views.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 03_views.sql
-- Mục đích: Tạo các View phục vụ hiển thị ứng dụng Desktop và Materialized View báo cáo
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- 1. VIEW PHỤC VỤ GIAO DIỆN PHẦN MỀM DESKTOP (WINFORMS)
-- ----------------------------------------------------------------------------

-- 1.1. Sơ đồ lưới máy trạm theo thời gian thực (Lõi của màn hình chính)
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
    c.specs
FROM core.computer c
JOIN core.room r ON c.room_id = r.room_id
JOIN core.computer_type ct ON c.type_id = ct.type_id
LEFT JOIN core.gaming_session s ON c.computer_id = s.computer_id AND s.status = 'active'
LEFT JOIN core.customer cust ON s.customer_id = cust.customer_id
ORDER BY c.computer_code;

COMMENT ON VIEW reporting.vw_computer_grid IS 'View sơ đồ phòng máy hiển thị trạng thái, người đang ngồi, thời gian chơi thực tế';

-- 1.2. View chi tiết phiên chơi và thời lượng (Không lưu thừa cột duration)
CREATE OR REPLACE VIEW reporting.vw_session_detail AS
SELECT
    s.session_id,
    c.computer_code,
    c.computer_name,
    cust.customer_id,
    cust.full_name AS customer_name,
    cust.username,
    emp.full_name AS opened_by_name,
    s.start_time,
    s.end_time,
    ROUND(EXTRACT(EPOCH FROM (COALESCE(s.end_time, clock_timestamp()) - s.start_time)) / 60.0) AS duration_minutes,
    s.hourly_rate_applied,
    s.status AS session_status,
    s.play_amount,
    COALESCE(inv.total_amount, 0) AS invoice_total
FROM core.gaming_session s
JOIN core.computer c ON s.computer_id = c.computer_id
JOIN core.customer cust ON s.customer_id = cust.customer_id
JOIN hr.employee emp ON s.opened_by = emp.employee_id
LEFT JOIN billing.invoice inv ON s.session_id = inv.session_id;

COMMENT ON VIEW reporting.vw_session_detail IS 'View chi tiết phiên chơi tính thời lượng tự động theo phút';

-- 1.3. Báo cáo doanh thu theo ngày
CREATE OR REPLACE VIEW reporting.vw_daily_revenue AS
SELECT
    date_trunc('day', i.paid_at)::date AS revenue_date,
    COUNT(i.invoice_id) AS total_invoices,
    SUM(i.play_amount) AS total_play_amount,
    SUM(i.service_amount) AS total_service_amount,
    SUM(i.discount_amount) AS total_discount_amount,
    SUM(i.total_amount) AS net_revenue
FROM billing.invoice i
WHERE i.status = 'paid' AND i.paid_at IS NOT NULL
GROUP BY date_trunc('day', i.paid_at)::date
ORDER BY revenue_date DESC;

-- 1.4. Báo cáo hiệu suất máy trạm
CREATE OR REPLACE VIEW reporting.vw_computer_usage AS
SELECT
    c.computer_id,
    c.computer_code,
    c.computer_name,
    r.room_name,
    COUNT(s.session_id) AS total_sessions,
    COALESCE(SUM(EXTRACT(EPOCH FROM (s.end_time - s.start_time)) / 3600.0), 0) AS total_hours_played,
    COALESCE(SUM(s.play_amount), 0) AS total_play_revenue
FROM core.computer c
JOIN core.room r ON c.room_id = r.room_id
LEFT JOIN core.gaming_session s ON c.computer_id = s.computer_id AND s.status = 'finished'
GROUP BY c.computer_id, c.computer_code, c.computer_name, r.room_name
ORDER BY total_hours_played DESC;

-- ----------------------------------------------------------------------------
-- 2. MATERIALIZED VIEWS (BÁO CÁO NẶNG TỐI ƯU VỚI UNIQUE INDEX)
-- ----------------------------------------------------------------------------

-- 2.1. Doanh thu theo ngày (hợp đồng báo cáo và lọc khoảng ngày trên Admin)
DROP MATERIALIZED VIEW IF EXISTS reporting.mv_revenue_daily CASCADE;
CREATE MATERIALIZED VIEW reporting.mv_revenue_daily AS
SELECT
    revenue_date,
    total_invoices,
    total_play_amount,
    total_service_amount,
    total_discount_amount,
    net_revenue
FROM reporting.vw_daily_revenue;

CREATE UNIQUE INDEX idx_mv_revenue_daily_date ON reporting.mv_revenue_daily (revenue_date);

-- 2.2. Doanh thu theo tháng (Cần UNIQUE INDEX để REFRESH CONCURRENTLY)
DROP MATERIALIZED VIEW IF EXISTS reporting.mv_monthly_revenue CASCADE;
CREATE MATERIALIZED VIEW reporting.mv_monthly_revenue AS
SELECT
    date_trunc('month', i.paid_at)::date AS revenue_month,
    COUNT(i.invoice_id) AS total_invoices,
    SUM(i.play_amount) AS total_play_amount,
    SUM(i.service_amount) AS total_service_amount,
    SUM(i.discount_amount) AS total_discount_amount,
    SUM(i.total_amount) AS net_revenue
FROM billing.invoice i
WHERE i.status = 'paid' AND i.paid_at IS NOT NULL
GROUP BY date_trunc('month', i.paid_at)::date
ORDER BY revenue_month DESC;

CREATE UNIQUE INDEX idx_mv_monthly_revenue_month ON reporting.mv_monthly_revenue (revenue_month);

-- 2.3. Top khách hàng thân thiết và chi tiêu cao nhất (kèm lần chơi gần nhất cho RFM)
DROP MATERIALIZED VIEW IF EXISTS reporting.mv_top_customers CASCADE;
CREATE MATERIALIZED VIEW reporting.mv_top_customers AS
SELECT
    c.customer_id,
    c.full_name,
    c.phone,
    c.membership_tier,
    c.loyalty_points,
    COUNT(DISTINCT s.session_id) AS total_sessions,
    COALESCE(SUM(i.total_amount), 0) AS total_spent,
    MAX(s.start_time) AS last_played_at
FROM core.customer c
LEFT JOIN core.gaming_session s ON c.customer_id = s.customer_id
LEFT JOIN billing.invoice i ON s.session_id = i.session_id AND i.status = 'paid'
GROUP BY c.customer_id, c.full_name, c.phone, c.membership_tier, c.loyalty_points
ORDER BY total_spent DESC;

CREATE UNIQUE INDEX idx_mv_top_customers_id ON reporting.mv_top_customers (customer_id);

-- 2.4. Thống kê tần suất chơi theo giờ trong tuần (Phục vụ Module AI 5: Dự báo Happy Hour)
DROP MATERIALIZED VIEW IF EXISTS reporting.mv_hourly_usage CASCADE;
CREATE MATERIALIZED VIEW reporting.mv_hourly_usage AS
SELECT
    EXTRACT(ISODOW FROM s.start_time)::INT AS day_of_week,     -- 1: Thứ 2 ... 7: Chủ Nhật
    EXTRACT(HOUR FROM s.start_time)::INT AS hour_of_day,       -- 0..23h
    COUNT(s.session_id) AS session_count,
    ROUND(AVG(EXTRACT(EPOCH FROM (COALESCE(s.end_time, s.start_time + INTERVAL '1 hour') - s.start_time)) / 3600.0), 2) AS avg_duration_hours
FROM core.gaming_session s
GROUP BY EXTRACT(ISODOW FROM s.start_time)::INT, EXTRACT(HOUR FROM s.start_time)::INT
ORDER BY day_of_week, hour_of_day;

CREATE UNIQUE INDEX idx_mv_hourly_usage_dow_hour ON reporting.mv_hourly_usage (day_of_week, hour_of_day);



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/04_security.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 04_security.sql
-- Mục đích: Thiết lập Bảo mật CSDL toàn diện:
--           - RBAC (Role-Based Access Control) theo nguyên tắc đặc quyền tối thiểu
--           - Column-Level Security (Bảo vệ cột lương salary và password_hash)
--           - Row-Level Security (RLS) với ENABLE + FORCE ROW LEVEL SECURITY
--           - Ánh xạ tài khoản nhân viên qua db_username
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- 1. THU HỒI ĐẶC QUYỀN MẶC ĐỊNH TỪ PUBLIC
-- ----------------------------------------------------------------------------
REVOKE ALL ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL FUNCTIONS IN SCHEMA public FROM PUBLIC;

-- ----------------------------------------------------------------------------
-- 2. TẠO CÁC ROLE NHÓM VÀ USER ĐĂNG NHẬP (CHUẨN IDEMPOTENT)
-- ----------------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_admin') THEN
        CREATE ROLE role_admin NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_manager') THEN
        CREATE ROLE role_manager NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_cashier') THEN
        CREATE ROLE role_cashier NOLOGIN;
    END IF;
END $$;

-- Login role được tạo và cấp password riêng bằng scripts/provision-app-roles.ps1.

-- ----------------------------------------------------------------------------
-- 4. PHÂN QUYỀN TRUY CẬP SCHEMAS (USAGE)
-- ----------------------------------------------------------------------------
GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting, audit, ai TO role_admin, role_manager;
GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting TO role_cashier;

-- ----------------------------------------------------------------------------
-- 5. CẤP QUYỀN TRÊN BẢNG VÀ CỘT (TABLE & COLUMN-LEVEL PRIVILEGES)
-- ----------------------------------------------------------------------------

-- 5.1. Các role vận hành chỉ đọc bảng; thao tác ghi đi qua function có kiểm tra nghiệp vụ.
GRANT SELECT ON core.room, core.computer_type, core.computer, core.customer, core.gaming_session TO role_cashier;
GRANT SELECT ON core.service_request TO role_cashier;

-- 5.2. Cashier đọc menu và hàng đợi; function đặt món chịu trách nhiệm ghi.
GRANT SELECT ON inventory.food_order, inventory.food_order_detail,
    inventory.product_category, inventory.product TO role_cashier;

-- 5.3. Cashier đọc chứng từ; function thanh toán/nạp ví ghi dữ liệu.
GRANT SELECT ON billing.invoice, billing.invoice_promotion,
    billing.payment, billing.promotion TO role_cashier;

-- 5.4. Quyền trên schema reporting
GRANT SELECT ON ALL TABLES IN SCHEMA reporting TO role_admin, role_manager, role_cashier;

-- 5.5. Quyền trên schema ai (Chỉ Admin và Manager được đọc và xử lý)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA ai TO role_admin, role_manager;

-- 5.6. Admin toàn quyền
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA core, inventory, billing, hr, reporting, audit, ai TO role_admin;
GRANT SELECT ON ALL TABLES IN SCHEMA core, inventory, billing, hr, reporting, ai TO role_manager;
GRANT MAINTAIN ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai TO role_admin;
-- GRANT pg_read_all_stats bo qua tren Cloud Database

-- Manager chỉ đọc nhân sự công khai; thay đổi tài khoản chỉ do admin thực hiện.
REVOKE ALL ON hr.employee FROM role_manager;
GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_manager;

-- 5.7. Column-level security: manager/cashier không đọc salary/password_hash.
REVOKE ALL ON hr.employee FROM role_cashier;
GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_cashier;

-- Cung cấp View an toàn công khai thông tin nhân viên
CREATE OR REPLACE VIEW hr.vw_employee_public AS
SELECT employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active
FROM hr.employee;
GRANT SELECT ON hr.vw_employee_public TO role_cashier;
GRANT SELECT ON hr.vw_employee_public TO role_manager;

GRANT SELECT ON hr.maintenance TO role_admin, role_manager;

-- Không cấp sequence cho role vận hành vì mọi thao tác ghi dùng SECURITY DEFINER function.

-- 5.8. Cấp quyền thực thi Stored Functions (EXECUTE)
GRANT EXECUTE ON FUNCTION core.fn_calc_session_amount TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION core.fn_membership_tier TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION core.sp_open_session TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION billing.sp_close_session_and_pay TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION billing.sp_top_up TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION inventory.sp_place_order TO role_cashier, role_manager, role_admin;

-- ----------------------------------------------------------------------------
-- 6. HÀM HỖ TRỢ RLS (SECURITY DEFINER, SEARCH_PATH CỐ ĐỊNH)
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION hr.fn_get_current_employee_id()
RETURNS INT AS $$
DECLARE
    v_user TEXT;
    v_id INT;
BEGIN
    -- Nhận diện user dù đăng nhập trực tiếp hay qua SET ROLE trong phiên demo
    v_user := COALESCE(NULLIF(current_setting('role', true), 'none'), session_user);
    SELECT employee_id INTO v_id
    FROM hr.employee
    WHERE db_username = v_user;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER
SET search_path = hr, pg_temp;

COMMENT ON FUNCTION hr.fn_get_current_employee_id IS 'Hàm helper lấy employee_id của user PostgreSQL đang đăng nhập';
GRANT EXECUTE ON FUNCTION hr.fn_get_current_employee_id() TO role_cashier, role_manager, role_admin;

-- ----------------------------------------------------------------------------
-- 7. CHÍNH SÁCH BẢO MẬT CẤP DÒNG (ROW-LEVEL SECURITY - RLS)
-- ----------------------------------------------------------------------------

-- 7.1. RLS trên bảng billing.invoice: Thu ngân chỉ thấy hóa đơn do chính mình lập
ALTER TABLE billing.invoice ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.invoice FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_invoice_admin_manager ON billing.invoice;
CREATE POLICY p_invoice_admin_manager ON billing.invoice
    FOR ALL
    TO role_admin, role_manager
    USING (TRUE)
    WITH CHECK (TRUE);

DROP POLICY IF EXISTS p_invoice_cashier ON billing.invoice;
CREATE POLICY p_invoice_cashier ON billing.invoice
    FOR ALL
    TO role_cashier
    USING (cashier_id = hr.fn_get_current_employee_id())
    WITH CHECK (cashier_id = hr.fn_get_current_employee_id());

-- 7.2. RLS trên bảng billing.payment: Thu ngân chỉ thấy giao dịch của mình
ALTER TABLE billing.payment ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing.payment FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_payment_admin_manager ON billing.payment;
CREATE POLICY p_payment_admin_manager ON billing.payment
    FOR ALL
    TO role_admin, role_manager
    USING (TRUE)
    WITH CHECK (TRUE);

DROP POLICY IF EXISTS p_payment_cashier ON billing.payment;
CREATE POLICY p_payment_cashier ON billing.payment
    FOR ALL
    TO role_cashier
    USING (employee_id = hr.fn_get_current_employee_id())
    WITH CHECK (employee_id = hr.fn_get_current_employee_id());

-- 7.3. RLS trên bảng hr.employee: Nhân viên cấp thấp chỉ thấy dòng của chính mình
-- Lưu ý: Không dùng FORCE trên hr.employee để function SECURITY DEFINER (owner postgres)
-- tra cứu được ID mà không bị đệ quy chính sách.
ALTER TABLE hr.employee ENABLE ROW LEVEL SECURITY;
ALTER TABLE hr.employee NO FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_employee_admin_manager ON hr.employee;
CREATE POLICY p_employee_admin_manager ON hr.employee
    FOR ALL
    TO role_admin, role_manager
    USING (TRUE)
    WITH CHECK (TRUE);

DROP POLICY IF EXISTS p_employee_self ON hr.employee;
CREATE POLICY p_employee_self ON hr.employee
    FOR SELECT
    TO role_cashier
    USING (employee_id = hr.fn_get_current_employee_id());

-- 7.4. RLS trên bảng hr.maintenance: Admin/Manager quản lý toàn bộ phiếu
ALTER TABLE hr.maintenance ENABLE ROW LEVEL SECURITY;
ALTER TABLE hr.maintenance FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_maint_admin_manager ON hr.maintenance;
CREATE POLICY p_maint_admin_manager ON hr.maintenance
    FOR ALL
    TO role_admin, role_manager
    USING (TRUE)
    WITH CHECK (TRUE);



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/05_indexes_partition.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 05_indexes_partition.sql
-- Mục đích: Tối ưu hóa hiệu năng CSDL toàn diện:
--           1. B-Tree Indexes cho khóa ngoại và cột lọc thường xuyên
--           2. Partial Indexes cho dữ liệu biến động cao (phiên active, máy available)
--           3. GIN Trigram & JSONB Indexes (tìm kiếm mờ, lọc cấu hình phần cứng)
--           4. BRIN Index cho bảng nhật ký thời gian lớn (audit_log)
--           5. Thiết lập Fillfactor tối ưu Heap-Only Tuples (HOT) & Autovacuum
--           6. Thủ tục tự động tạo Partition bảng nhật ký kiểm toán theo tháng
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- 1. B-TREE INDEXES CHO KHÓA NGOẠI VÀ TRUY VẤN TẦN SUẤT CAO
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Mặc định PostgreSQL không tự tạo index trên cột Foreign Key.
-- Index trên FK chống Table Scan khi JOIN và tránh Row Exclusive Lock lan tỏa khi DELETE/UPDATE bảng cha.

CREATE INDEX IF NOT EXISTS idx_session_customer_id
    ON core.gaming_session (customer_id);

CREATE INDEX IF NOT EXISTS idx_session_computer_id
    ON core.gaming_session (computer_id);

CREATE INDEX IF NOT EXISTS idx_session_start_time
    ON core.gaming_session (start_time DESC);

CREATE INDEX IF NOT EXISTS idx_order_session_id
    ON inventory.food_order (session_id);

CREATE INDEX IF NOT EXISTS idx_invoice_customer_id
    ON billing.invoice (customer_id);

CREATE INDEX IF NOT EXISTS idx_invoice_cashier_id
    ON billing.invoice (cashier_id);

CREATE INDEX IF NOT EXISTS idx_invoice_issued_at
    ON billing.invoice (issued_at DESC);

CREATE INDEX IF NOT EXISTS idx_payment_customer_id
    ON billing.payment (customer_id);

CREATE INDEX IF NOT EXISTS idx_payment_invoice_id
    ON billing.payment (invoice_id) WHERE invoice_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_maint_computer_id
    ON hr.maintenance (computer_id);

CREATE INDEX IF NOT EXISTS idx_maint_technician_id
    ON hr.maintenance (technician_id);

-- ----------------------------------------------------------------------------
-- 2. PARTIAL INDEXES (INDEX CÓ ĐIỀU KIỆN)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Chỉ đánh chỉ mục trên tập con dòng thỏa mãn WHERE.
-- Dung lượng index cực nhỏ, cache hoàn toàn trong RAM, tối ưu vượt trội cho các truy vấn thời gian thực.

-- 2.1. Tra cứu các phiên chơi đang hoạt động (chỉ vài chục dòng trong hàng triệu dòng lịch sử)
CREATE INDEX IF NOT EXISTS idx_session_active
    ON core.gaming_session (customer_id, computer_id)
    WHERE status = 'active';

-- 2.2. Tra cứu máy tính còn trống theo phòng
CREATE INDEX IF NOT EXISTS idx_computer_available
    ON core.computer (room_id, type_id)
    WHERE status = 'available';

-- 2.3. Tra cứu hóa đơn chưa thanh toán
CREATE INDEX IF NOT EXISTS idx_invoice_unpaid
    ON billing.invoice (invoice_id, customer_id)
    WHERE status = 'unpaid';

-- ----------------------------------------------------------------------------
-- 3. GIN INDEXES (GENERALIZED INVERTED INDEX)
-- ----------------------------------------------------------------------------
-- 3.1. GIN Trigram cho tìm kiếm mờ họ tên khách hàng không dấu và có dấu (ILIKE / LIKE '%...%')
-- Giải thích DBA: B-Tree không dùng được khi tìm kiếm có ký tự đại diện ở đầu (%abc%).
-- GIN pg_trgm tách chuỗi thành các cụm 3 ký tự, cho phép tăng tốc tìm kiếm chuỗi con tức thì.
CREATE INDEX IF NOT EXISTS idx_customer_fullname_trgm
    ON core.customer USING gin (full_name gin_trgm_ops);

-- 3.2. GIN jsonb_path_ops cho cấu hình phần cứng máy trạm (core.computer.specs)
-- Giải thích DBA: jsonb_path_ops băm cả path+value thành 1 hash integer, nhỏ gọn hơn GIN mặc định và tăng tốc toán tử @>.
CREATE INDEX IF NOT EXISTS idx_computer_specs_gin
    ON core.computer USING gin (specs jsonb_path_ops);

-- 3.3. GIN jsonb_path_ops cho thuộc tính sản phẩm F&B
CREATE INDEX IF NOT EXISTS idx_product_attr_gin
    ON inventory.product USING gin (attributes jsonb_path_ops);

-- ----------------------------------------------------------------------------
-- 4. BRIN INDEX (BLOCK RANGE INDEX) CHO BẢNG NHẬT KÝ THỜI GIAN LỚN
-- ----------------------------------------------------------------------------
-- Giải thích DBA: BRIN lưu tóm tắt min/max của từng cụm trang đĩa vật lý (block range).
-- Chiếm dưới 1% dung lượng so với B-Tree, hoàn hảo cho dữ liệu audit_log ghi theo thứ tự thời gian tăng dần liên tục.
CREATE INDEX IF NOT EXISTS idx_audit_log_logged_at_brin
    ON audit.audit_log USING brin (logged_at);

-- ----------------------------------------------------------------------------
-- 5. TỐI ƯU HÓA HEAP-ONLY TUPLES (HOT) & AUTOVACUUM
-- ----------------------------------------------------------------------------
-- Giải thích DBA:
-- - Fillfactor < 100 chừa khoảng trống (free space) trên mỗi page dữ liệu để khi UPDATE,
--   tuple mới được ghi ngay trên cùng page (Heap-Only Tuple).
--   Khi đó không cần cập nhật lại các B-Tree index, giảm triệt để I/O write và chống index bloat.
-- - autovacuum_vacuum_scale_factor thấp hơn giúp dọn dẹp tuple chết sớm hơn trên các bảng cập nhật nhiều.

ALTER TABLE core.gaming_session SET (
    fillfactor = 90,
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_vacuum_cost_limit = 500
);

ALTER TABLE core.computer SET (
    fillfactor = 85,
    autovacuum_vacuum_scale_factor = 0.05
);

ALTER TABLE inventory.product SET (
    fillfactor = 90
);

-- ----------------------------------------------------------------------------
-- 6. THỦ TỤC TỰ ĐỘNG TẠO PARTITION THEO THÁNG CHO AUDIT_LOG
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Tự động hóa vận hành định kỳ để DBA không phải tạo phân vùng thủ công mỗi tháng.
CREATE OR REPLACE FUNCTION audit.fn_create_monthly_partitions(p_months_ahead INT DEFAULT 3)
RETURNS TABLE(partition_name TEXT, start_date DATE, end_date DATE, action_taken TEXT) AS $$
DECLARE
    v_target_date DATE;
    v_next_date DATE;
    v_part_name TEXT;
    v_sql TEXT;
    i INT;
BEGIN
    FOR i IN 0..p_months_ahead LOOP
        v_target_date := date_trunc('month', CURRENT_DATE + (i || ' month')::interval)::DATE;
        v_next_date   := date_trunc('month', v_target_date + '1 month'::interval)::DATE;
        v_part_name   := 'audit_log_' || to_char(v_target_date, 'YYYY_MM');

        IF NOT EXISTS (
            SELECT 1 FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'audit' AND c.relname = v_part_name
        ) THEN
            v_sql := format(
                'CREATE TABLE audit.%I PARTITION OF audit.audit_log FOR VALUES FROM (%L) TO (%L);',
                v_part_name,
                to_char(v_target_date, 'YYYY-MM-01 00:00:00+00'),
                to_char(v_next_date, 'YYYY-MM-01 00:00:00+00')
            );
            EXECUTE v_sql;

            partition_name := v_part_name;
            start_date := v_target_date;
            end_date := v_next_date;
            action_taken := 'CREATED';
            RETURN NEXT;
        ELSE
            partition_name := v_part_name;
            start_date := v_target_date;
            end_date := v_next_date;
            action_taken := 'ALREADY_EXISTS';
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION audit.fn_create_monthly_partitions IS 'Tự động tạo partition audit_log cho N tháng tiếp theo';



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/06_seed_base.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 06_seed_base.sql
-- Mục đích: Dữ liệu mồi nhỏ thuần SQL (20 máy, phòng, menu F&B, nhân viên, khách hàng)
--           Phục vụ demo trực tiếp trên pgAdmin và phần mềm desktop không phụ thuộc Python
-- ============================================================================

\c netcafe_db;

-- 1. XÓA DỮ LIỆU CŨ VÀ RESET IDENTITY SEQUENCES
TRUNCATE hr.maintenance, billing.payment, billing.invoice_promotion, billing.invoice,
         inventory.food_order_detail, inventory.food_order,
         core.gaming_session, inventory.product, inventory.product_category,
         core.customer, core.computer, core.computer_type, core.room,
         hr.employee, billing.promotion RESTART IDENTITY CASCADE;

-- 2. NHÂN SỰ VÀ TÀI KHOẢN HỆ THỐNG (3 VAI TRÒ ỨNG DỤNG)
INSERT INTO hr.employee (full_name, phone, username, db_username, password_hash, emp_role, default_shift, salary, hired_date)
VALUES
    ('Nguyễn Văn An', '0981234567', 'an_nguyen', 'nv_admin', crypt(gen_random_uuid()::text, gen_salt('bf')), 'admin', 'morning', 20000000, '2024-01-15'),
    ('Lê Quang Minh', '0972345678', 'minh_le', 'nv_manager', crypt(gen_random_uuid()::text, gen_salt('bf')), 'manager', 'morning', 15000000, '2024-03-01'),
    ('Phạm Thu Thảo', '0913456789', 'thao_pham', 'nv_thao', crypt(gen_random_uuid()::text, gen_salt('bf')), 'cashier', 'morning', 8500000, '2024-06-10'),
    ('Trần Đức Nam', '0934567890', 'nam_tran', 'nv_nam', crypt(gen_random_uuid()::text, gen_salt('bf')), 'cashier', 'afternoon', 8500000, '2024-06-15');

-- 3. KHU VỰC VÀ PHÒNG MÁY
INSERT INTO core.room (room_name, description)
VALUES
    ('Khu Máy Lạnh', 'Không gian thoáng mát cấm hút thuốc, ghế công thái học'),
    ('Khu Hút Thuốc', 'Khu vực có hệ thống hút mùi riêng biệt'),
    ('Phòng VIP Thi Đấu', 'Khu máy cấu hình cực cao, màn hình 240Hz, tai nghe chuyên game');

-- 4. LOẠI MÁY VÀ GIÁ THUÊ/GIỜ (CHUẨN HÓA 3NF)
INSERT INTO core.computer_type (type_name, hourly_rate)
VALUES
    ('Tiêu Chuẩn', 8000),      -- Core i5, GTX 1660 Super
    ('Nâng Cao', 12000),       -- Core i5 13th, RTX 3060
    ('VIP Thi Đấu', 18000);    -- Core i7 13th, RTX 4070

-- 5. DANH SÁCH 20 MÁY TRẠM (PC001 -> PC020)
-- 8 máy Tiêu chuẩn (Khu hút thuốc)
INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address, specs, status)
SELECT
    v.code, v.name, r.room_id, ct.type_id, v.ip::inet, v.mac::macaddr, v.specs::jsonb, 'available'
FROM (VALUES
    ('PC001', 'Máy 01', '192.168.1.101', '00:1A:2B:3C:4D:01', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC002', 'Máy 02', '192.168.1.102', '00:1A:2B:3C:4D:02', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC003', 'Máy 03', '192.168.1.103', '00:1A:2B:3C:4D:03', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC004', 'Máy 04', '192.168.1.104', '00:1A:2B:3C:4D:04', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC005', 'Máy 05', '192.168.1.105', '00:1A:2B:3C:4D:05', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC006', 'Máy 06', '192.168.1.106', '00:1A:2B:3C:4D:06', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC007', 'Máy 07', '192.168.1.107', '00:1A:2B:3C:4D:07', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC008', 'Máy 08', '192.168.1.108', '00:1A:2B:3C:4D:08', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}')
) AS v(code, name, ip, mac, specs)
CROSS JOIN (SELECT room_id FROM core.room WHERE room_name = 'Khu Hút Thuốc') r
CROSS JOIN (SELECT type_id FROM core.computer_type WHERE type_name = 'Tiêu Chuẩn') ct;

-- 7 máy Nâng cao (Khu máy lạnh)
INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address, specs, status)
SELECT
    v.code, v.name, r.room_id, ct.type_id, v.ip::inet, v.mac::macaddr, v.specs::jsonb, 'available'
FROM (VALUES
    ('PC009', 'Máy 09', '192.168.1.109', '00:1A:2B:3C:4D:09', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC010', 'Máy 10', '192.168.1.110', '00:1A:2B:3C:4D:10', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC011', 'Máy 11', '192.168.1.111', '00:1A:2B:3C:4D:11', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC012', 'Máy 12', '192.168.1.112', '00:1A:2B:3C:4D:12', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC013', 'Máy 13', '192.168.1.113', '00:1A:2B:3C:4D:13', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC014', 'Máy 14', '192.168.1.114', '00:1A:2B:3C:4D:14', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC015', 'Máy 15', '192.168.1.115', '00:1A:2B:3C:4D:15', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}')
) AS v(code, name, ip, mac, specs)
CROSS JOIN (SELECT room_id FROM core.room WHERE room_name = 'Khu Máy Lạnh') r
CROSS JOIN (SELECT type_id FROM core.computer_type WHERE type_name = 'Nâng Cao') ct;

-- 5 máy VIP Thi đấu (Phòng VIP)
INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address, specs, status)
SELECT
    v.code, v.name, r.room_id, ct.type_id, v.ip::inet, v.mac::macaddr, v.specs::jsonb, 'available'
FROM (VALUES
    ('PC016', 'Máy VIP 16', '192.168.1.116', '00:1A:2B:3C:4D:16', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC017', 'Máy VIP 17', '192.168.1.117', '00:1A:2B:3C:4D:17', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC018', 'Máy VIP 18', '192.168.1.118', '00:1A:2B:3C:4D:18', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC019', 'Máy VIP 19', '192.168.1.119', '00:1A:2B:3C:4D:19', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC020', 'Máy VIP 20', '192.168.1.120', '00:1A:2B:3C:4D:20', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}')
) AS v(code, name, ip, mac, specs)
CROSS JOIN (SELECT room_id FROM core.room WHERE room_name = 'Phòng VIP Thi Đấu') r
CROSS JOIN (SELECT type_id FROM core.computer_type WHERE type_name = 'VIP Thi Đấu') ct;

-- 6. DANH MỤC VÀ SẢN PHẨM F&B
INSERT INTO inventory.product_category (category_name)
VALUES
    ('Đồ Uống Pha Chế'),
    ('Nước Ngọt Đóng Lon'),
    ('Đồ Ăn Nhanh'),
    ('Snack & Ăn Vặt');

-- Thêm sản phẩm qua danh mục
INSERT INTO inventory.product (sku, product_name, category_id, unit_price, stock_qty, attributes)
SELECT v.sku, v.name, cat.category_id, v.price, v.qty, v.attr::jsonb
FROM (VALUES
    ('STING-RED', 'Sting Dâu Đỏ 330ml', 'Nước Ngọt Đóng Lon', 12000, 100, '{"size":"lon","cold":true}'),
    ('REDBULL', 'Bò Húc RedBull Thái', 'Nước Ngọt Đóng Lon', 18000, 80, '{"size":"lon","cold":true}'),
    ('COCA-ZERO', 'Coca Cola Zero Calo', 'Nước Ngọt Đóng Lon', 12000, 60, '{"size":"lon","cold":true}'),
    ('AQUA-500', 'Nước Khoáng Aquafina 500ml', 'Nước Ngọt Đóng Lon', 8000, 120, '{"size":"chai"}'),
    ('CF-SUA-DA', 'Cà Phê Sữa Đá', 'Đồ Uống Pha Chế', 18000, 200, '{"type":"pha_che"}'),
    ('TRA-DAO', 'Trà Đào Cam Sả', 'Đồ Uống Pha Chế', 22000, 150, '{"type":"pha_che"}'),
    ('MI-TRUNG', 'Mì Tôm Trứng Xúc Xích', 'Đồ Ăn Nhanh', 25000, 75, '{"type":"do_an_nong"}'),
    ('COM-XAO-BO', 'Cơm Rang Dưa Bò', 'Đồ Ăn Nhanh', 40000, 40, '{"type":"do_an_nong"}'),
    ('BANH-MI-PATE', 'Bánh Mì Pate Chả Lụa', 'Đồ Ăn Nhanh', 20000, 50, '{"type":"do_an_nhe"}'),
    ('BIM-BIM-KHOAI', 'Snack Khoai Tây Lays', 'Snack & Ăn Vặt', 15000, 85, '{"flavor":"rong_bien"}')
) AS v(sku, name, cat_name, price, qty, attr)
JOIN inventory.product_category cat ON v.cat_name = cat.category_name;

-- 7. CHƯƠNG TRÌNH KHUYẾN MÃI
INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
VALUES
    ('HAPPY22', 'Khung Giờ Vàng Đêm (22h - 6h)', 'happy_hour', 20.0, '2026-01-01 00:00:00+07', '2027-01-01 00:00:00+07', '{"time_window":"22:00-06:00","discount_percent":20}'::jsonb, TRUE),
    ('GOLD10', 'Ưu Đãi Hội Viên Hạng Gold', 'percent', 10.0, '2026-01-01 00:00:00+07', '2027-01-01 00:00:00+07', '{"required_tier":"gold"}'::jsonb, TRUE),
    ('CHAOHAY26', 'Mừng Năm Mới 2026', 'fixed', 15000.0, '2026-01-01 00:00:00+07', '2027-01-01 00:00:00+07', '{"min_bill":50000}'::jsonb, TRUE);

-- 8. DANH SÁCH KHÁCH HÀNG MẪU (12 KHÁCH CÓ SỐ DƯ TÀI KHOẢN)
INSERT INTO core.customer (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier)
VALUES
    ('Trần Văn Bình', '0912345678', 'binh.tran@gmail.com', 'binh_tran', crypt(gen_random_uuid()::text, gen_salt('bf')), 80000, 120, 'silver'),
    ('Hoàng Đức Anh', '0983456789', 'anh.hoang@gmail.com', 'anh_hoang', crypt(gen_random_uuid()::text, gen_salt('bf')), 150000, 650, 'gold'),
    ('Đặng Thùy Dung', '0974567890', 'dung.dang@gmail.com', 'dung_dang', crypt(gen_random_uuid()::text, gen_salt('bf')), 250000, 2200, 'vip'),
    ('Nguyễn Hải Đăng', '0935678901', 'dang.hai@gmail.com', 'dang_hai', crypt(gen_random_uuid()::text, gen_salt('bf')), 50000, 45, 'regular'),
    ('Lê Bảo Châu', '0966789012', 'chau.le@gmail.com', 'chau_le', crypt(gen_random_uuid()::text, gen_salt('bf')), 120000, 520, 'gold'),
    ('Phan Thanh Tùng', '0987890123', 'tung.phan@gmail.com', 'tung_phan', crypt(gen_random_uuid()::text, gen_salt('bf')), 30000, 20, 'regular'),
    ('Võ Quốc Huy', '0918901234', 'huy.vo@gmail.com', 'huy_vo', crypt(gen_random_uuid()::text, gen_salt('bf')), 90000, 180, 'silver'),
    ('Bùi Minh Quân', '0939012345', 'quan.bui@gmail.com', 'quan_bui', crypt(gen_random_uuid()::text, gen_salt('bf')), 300000, 2800, 'vip'),
    ('Đỗ Gia Hân', '0970123456', 'han.do@gmail.com', 'han_do', crypt(gen_random_uuid()::text, gen_salt('bf')), 60000, 80, 'regular'),
    ('Dương Tuấn Kiệt', '0981122334', 'kiet.duong@gmail.com', 'kiet_duong', crypt(gen_random_uuid()::text, gen_salt('bf')), 110000, 310, 'silver'),
    ('Tran Van Binh', '0912345679', 'binh.duplicate@gmail.com', 'binh_dup', crypt(gen_random_uuid()::text, gen_salt('bf')), 50000, 10, 'regular'),
    ('Nguyễn Việt Anh', '0327469105', 'vietanh.nguyen@gmail.com', 'vietanh_vip', crypt('Pass@123', gen_salt('bf')), 500000, 3500, 'vip');

-- 9. LỊCH SỬ BẢO TRÌ MẪU (CHO DEMO RLS VÀ MODULE BẢO TRÌ)
INSERT INTO hr.maintenance (computer_id, technician_id, issue_description, parts_replaced, cost, status, started_at, finished_at)
VALUES
    (1, 2, 'Thay quạt tản nhiệt CPU kêu to', '{"fan":"CoolerMaster 120mm"}'::jsonb, 150000, 'fixed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    (2, 1, 'Kiểm tra nguồn máy tính sập nguồn', '{"psu":"Corsair 650W"}'::jsonb, 500000, 'in_progress', NOW() - INTERVAL '3 hours', NULL);

-- 10. PHIÊN CHƠI VÀ HÓA ĐƠN MẪU (PHỤC VỤ DEMO RLS VÀ BÁO CÁO DOANH THU)
INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied, play_amount, game_info)
VALUES
    (1, 1, 3, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '1 hour', 'finished', 8000, 16000, '{"game":"League of Legends"}'::jsonb),
    (2, 2, 3, NOW() - INTERVAL '4 hours', NOW() - INTERVAL '2 hours', 'finished', 8000, 16000, '{"game":"Valorant"}'::jsonb),
    (3, 3, 3, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '3 hours', 'finished', 8000, 16000, '{"game":"CS2"}'::jsonb),
    (4, 9, 4, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '1 hour', 'finished', 12000, 24000, '{"game":"Dota 2"}'::jsonb),
    (5, 10, 4, NOW() - INTERVAL '4 hours', NOW() - INTERVAL '2 hours', 'finished', 12000, 24000, '{"game":"PUBG"}'::jsonb),
    (6, 11, 4, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '3 hours', 'finished', 12000, 24000, '{"game":"Genshin Impact"}'::jsonb),
    (3, 16, 3, NOW() - INTERVAL '5 days' - INTERVAL '4 hours', NOW() - INTERVAL '5 days', 'finished', 18000, 72000, '{"game":"Cyberpunk 2077"}'::jsonb),
    (8, 17, 3, NOW() - INTERVAL '36 hours', NOW() - INTERVAL '1 hour', 'finished', 18000, 630000, '{"game":"Marathon Stream"}'::jsonb),
    ((SELECT customer_id FROM core.customer WHERE username = 'vietanh_vip'),
     18, 3, NOW() - INTERVAL '4 days' - INTERVAL '5 hours', NOW() - INTERVAL '4 days',
     'finished', 18000, 90000, '{"game":"CS2 Tournament"}'::jsonb);

INSERT INTO billing.invoice (session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount, status, issued_at, paid_at)
VALUES
    (1, 1, 3, 16000, 0, 0, 0, 16000, 'paid', NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour'),
    (2, 2, 3, 16000, 0, 0, 0, 16000, 'paid', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours'),
    (3, 3, 3, 16000, 0, 0, 0, 16000, 'paid', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours'),
    (4, 4, 4, 24000, 0, 0, 0, 24000, 'paid', NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour'),
    (5, 5, 4, 24000, 0, 0, 0, 24000, 'paid', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours'),
    (6, 6, 4, 24000, 0, 0, 0, 24000, 'paid', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours'),
    (7, 3, 3, 72000, 0, 0, 0, 72000, 'paid', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    (8, 8, 3, 630000, 0, 0, 0, 630000, 'paid', NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour'),
    (9, (SELECT customer_id FROM core.customer WHERE username = 'vietanh_vip'),
     3, 90000, 0, 0, 0, 90000, 'paid', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days');

INSERT INTO billing.payment (kind, invoice_id, customer_id, method, amount, employee_id, paid_at, reference)
VALUES
    ('invoice', 1, 1, 'cash', 16000, 3, NOW() - INTERVAL '1 hour', 'HD-001'),
    ('invoice', 2, 2, 'cash', 16000, 3, NOW() - INTERVAL '2 hours', 'HD-002'),
    ('invoice', 3, 3, 'cash', 16000, 3, NOW() - INTERVAL '3 hours', 'HD-003'),
    ('invoice', 4, 4, 'cash', 24000, 4, NOW() - INTERVAL '1 hour', 'HD-004'),
    ('invoice', 5, 5, 'cash', 24000, 4, NOW() - INTERVAL '2 hours', 'HD-005'),
    ('invoice', 6, 6, 'cash', 24000, 4, NOW() - INTERVAL '3 hours', 'HD-006'),
    ('invoice', 7, 3, 'cash', 72000, 3, NOW() - INTERVAL '5 days', 'HD-007'),
    ('invoice', 8, 8, 'bank_transfer', 630000, 3, NOW() - INTERVAL '1 hour', 'HD-008'),
    ('invoice', 9, (SELECT customer_id FROM core.customer WHERE username = 'vietanh_vip'),
     'bank_transfer', 90000, 3, NOW() - INTERVAL '4 days', 'HD-009'),
    ('topup', NULL, 8, 'bank_transfer', 10000000, 1, NOW() - INTERVAL '1 day', 'DEP-001'),
    ('topup', NULL, (SELECT customer_id FROM core.customer WHERE username = 'vietanh_vip'),
     'bank_transfer', 1000000, 1, NOW() - INTERVAL '5 days', 'DEP-002');

-- 11. LÀM MỚI MATERIALIZED VIEWS
REFRESH MATERIALIZED VIEW reporting.mv_revenue_daily;
REFRESH MATERIALIZED VIEW reporting.mv_monthly_revenue;
REFRESH MATERIALIZED VIEW reporting.mv_top_customers;
REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/07_ai_modules.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 07_ai_modules.sql
-- Mục đích: Triển khai 5 Module "AI" (Tự động hóa thông minh trong PostgreSQL):
--           1. Cảnh báo an ninh thông minh (Security Alert & Risk Scoring)
--           2. Tự động khuyến nghị chỉ mục (Index Recommendation Engine)
--           3. Phát hiện khách hàng trùng lặp mờ (Duplicate Customer Detection via pg_trgm)
--           4. Phát hiện bất thường thống kê Z-Score (Statistical Anomaly Detection)
--           5. Gợi ý chương trình khuyến mãi thông minh (Smart Promotion Suggestion)
--           Toàn bộ thuật toán chạy 100% bằng SQL/PLpgSQL nội bộ CSDL, không phụ thuộc API ngoài.
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- MODULE 1: CẢNH BÁO AN NINH THÔNG MINH (SECURITY ALERT & RISK SCORING)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Khi có thay đổi trên các bảng nhạy cảm (nhân sự, hóa đơn, thanh toán),
-- Trigger tự động đánh giá mức độ rủi ro (Risk Score từ 0 đến 100) dựa trên quy tắc an ninh
-- và thời gian phát sinh; nếu vượt ngưỡng cảnh báo (>= 40) thì tự động ghi vào ai_alert
-- và phát tín hiệu NOTIFY thời gian thực tới ứng dụng quản trị.

CREATE OR REPLACE FUNCTION audit.fn_audit_security_alert_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_audit_id BIGINT;
    v_logged_at TIMESTAMPTZ := clock_timestamp();
    v_actor VARCHAR(50);
    v_action VARCHAR(10) := TG_OP;
    v_pk_val VARCHAR(50);
    v_old_json JSONB := NULL;
    v_new_json JSONB := NULL;
    v_risk_score INT := 10;
    v_risk_level VARCHAR(10) := 'low';
    v_reason TEXT := '';
    v_alert_id INT;
BEGIN
    -- Nhận diện actor đăng nhập
    v_actor := COALESCE(NULLIF(current_setting('role', true), 'none'), session_user);

    -- Trích xuất Primary Key an toàn theo từng bảng
    IF TG_TABLE_NAME = 'employee' THEN
        v_pk_val := CASE WHEN TG_OP = 'DELETE' THEN OLD.employee_id::text ELSE NEW.employee_id::text END;
    ELSIF TG_TABLE_NAME = 'payment' THEN
        v_pk_val := CASE WHEN TG_OP = 'DELETE' THEN OLD.payment_id::text ELSE NEW.payment_id::text END;
    ELSIF TG_TABLE_NAME = 'invoice' THEN
        v_pk_val := CASE WHEN TG_OP = 'DELETE' THEN OLD.invoice_id::text ELSE NEW.invoice_id::text END;
    ELSE
        v_pk_val := 'unknown';
    END IF;

    -- Trích xuất JSON và kiểm tra rủi ro
    IF (TG_OP = 'DELETE') THEN
        v_old_json := to_jsonb(OLD) - 'password_hash' - 'salary';
        v_risk_score := v_risk_score + 30;
        v_reason := v_reason || '[Hành động xóa dữ liệu nhạy cảm] ';
    ELSIF (TG_OP = 'UPDATE') THEN
        v_old_json := to_jsonb(OLD) - 'password_hash' - 'salary';
        v_new_json := to_jsonb(NEW) - 'password_hash' - 'salary';

        -- Kiểm tra thay đổi lương nhân viên
        IF TG_TABLE_NAME = 'employee' THEN
            IF (OLD.salary IS DISTINCT FROM NEW.salary) THEN
                v_risk_score := v_risk_score + 45;
                v_reason := v_reason || format('[Điều chỉnh lương: %s -> %s] ', OLD.salary, NEW.salary);
            END IF;
        ELSIF TG_TABLE_NAME = 'invoice' THEN
            IF NEW.status = 'void' AND OLD.status <> 'void' THEN
                v_risk_score := v_risk_score + 50;
                v_reason := v_reason || '[Hủy hóa đơn đã xuất (VOID)] ';
            END IF;
        END IF;
    ELSIF (TG_OP = 'INSERT') THEN
        v_new_json := to_jsonb(NEW) - 'password_hash' - 'salary';

        -- Kiểm tra giao dịch nạp tiền hoặc thanh toán lớn bất thường (>= 5.000.000 VNĐ)
        IF TG_TABLE_NAME = 'payment' THEN
            IF NEW.amount >= 5000000 THEN
                v_risk_score := v_risk_score + 40;
                v_reason := v_reason || format('[Giao dịch tài chính lớn: %s VNĐ] ', NEW.amount);
            END IF;
        END IF;
    END IF;

    -- Kiểm tra phát sinh ngoài giờ làm việc (từ 23h đêm đến 6h sáng)
    IF EXTRACT(HOUR FROM v_logged_at) < 6 OR EXTRACT(HOUR FROM v_logged_at) >= 23 THEN
        v_risk_score := v_risk_score + 25;
        v_reason := v_reason || '[Thực hiện ngoài giờ làm việc thông thường] ';
    END IF;

    -- Giới hạn điểm rủi ro tối đa 100
    IF v_risk_score > 100 THEN v_risk_score := 100; END IF;

    -- 1. Ghi vào sổ nhật ký kiểm toán phân vùng audit_log
    INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
    VALUES (v_logged_at, v_actor, TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME, v_action, v_pk_val, v_old_json, v_new_json)
    RETURNING audit_id INTO v_audit_id;

    -- 2. Nếu vượt ngưỡng rủi ro (>= 40), sinh cảnh báo AI và phát NOTIFY
    IF v_risk_score >= 40 THEN
        IF v_risk_score >= 70 THEN
            v_risk_level := 'high';
        ELSE
            v_risk_level := 'medium';
        END IF;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (v_audit_id, v_logged_at, v_risk_score, v_risk_level, TRIM(v_reason), 'new')
        RETURNING alert_id INTO v_alert_id;

        -- Gửi thông điệp thời gian thực cho Desktop App qua LISTEN/NOTIFY
        PERFORM pg_notify(
            'ai_security_alert',
            json_build_object(
                'alert_id', v_alert_id,
                'risk_score', v_risk_score,
                'risk_level', v_risk_level,
                'table', TG_TABLE_NAME,
                'actor', v_actor,
                'reason', v_reason
            )::text
        );
    END IF;

    IF (TG_OP = 'DELETE') THEN
        RETURN OLD;
    ELSE
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Gắn trigger an ninh vào các bảng trọng yếu
DROP TRIGGER IF EXISTS trg_audit_security_employee ON hr.employee;
CREATE TRIGGER trg_audit_security_employee
    AFTER INSERT OR UPDATE OR DELETE ON hr.employee
    FOR EACH ROW EXECUTE FUNCTION audit.fn_audit_security_alert_trigger();

DROP TRIGGER IF EXISTS trg_audit_security_payment ON billing.payment;
CREATE TRIGGER trg_audit_security_payment
    AFTER INSERT OR UPDATE OR DELETE ON billing.payment
    FOR EACH ROW EXECUTE FUNCTION audit.fn_audit_security_alert_trigger();

DROP TRIGGER IF EXISTS trg_audit_security_invoice ON billing.invoice;
CREATE TRIGGER trg_audit_security_invoice
    AFTER INSERT OR UPDATE OR DELETE ON billing.invoice
    FOR EACH ROW EXECUTE FUNCTION audit.fn_audit_security_alert_trigger();

COMMENT ON FUNCTION audit.fn_audit_security_alert_trigger IS 'Module AI 1: Trigger kiểm toán và chấm điểm rủi ro an ninh tự động';

-- ----------------------------------------------------------------------------
-- MODULE 2: TỰ ĐỘNG KHUYẾN NGHỊ CHỈ MỤC (INDEX RECOMMENDATION ENGINE)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Quét qua catalog pg_stat_user_tables và các ràng buộc khóa ngoại (Foreign Key)
-- để nhận diện các bảng có tỷ lệ quét tuần tự cao hoặc các FK chưa được đánh chỉ mục.
CREATE OR REPLACE FUNCTION ai.fn_recommend_indexes()
RETURNS TABLE(
    recommendation_id INT,
    table_name VARCHAR(100),
    column_list VARCHAR(255),
    reason TEXT,
    suggested_sql TEXT
) AS $$
DECLARE
    r RECORD;
    v_rec_id INT;
    v_tbl TEXT;
    v_col TEXT;
    v_sql TEXT;
    v_reason TEXT;
BEGIN
    -- 1. Tìm các bảng có tỷ lệ Sequential Scan cao trên dữ liệu đáng kể
    FOR r IN
        SELECT
            schemaname || '.' || relname AS full_table,
            relname,
            seq_scan,
            idx_scan,
            n_live_tup
        FROM pg_stat_user_tables
        WHERE schemaname IN ('core', 'inventory', 'billing', 'hr')
          AND seq_scan > 5
          AND (idx_scan IS NULL OR idx_scan < seq_scan)
    LOOP
        v_tbl := r.full_table;
        SELECT a.attname
        INTO v_col
        FROM pg_attribute a
        WHERE a.attrelid = to_regclass(v_tbl)
          AND a.attnum > 0
          AND NOT a.attisdropped
          AND a.attname IN ('created_at', 'started_at', 'logged_at', 'order_date', 'invoice_date', 'status')
        ORDER BY CASE a.attname
            WHEN 'created_at' THEN 1
            WHEN 'started_at' THEN 2
            WHEN 'logged_at' THEN 3
            WHEN 'order_date' THEN 4
            WHEN 'invoice_date' THEN 5
            WHEN 'status' THEN 6
            ELSE 99
        END
        LIMIT 1;

        IF v_col IS NULL THEN
            CONTINUE;
        END IF;

        v_reason := format('Bảng %s bị quét tuần tự %s lần nhưng chỉ có %s lượt quét chỉ mục -> (Giải thích: CSDL đang phải đọc duyệt từng dòng để tìm kiếm. Cần tạo B-Tree Index trên cột [%s] để nhảy thẳng đến dữ liệu cần tìm, tránh quét toàn bộ bảng).',
                           v_tbl, r.seq_scan, COALESCE(r.idx_scan, 0), v_col);
        v_sql := format('CREATE INDEX idx_%s_auto_opt ON %s (%I);', r.relname, v_tbl, v_col);

        SELECT rec_id INTO v_rec_id
        FROM ai.index_recommendation
        WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql
        LIMIT 1;

        IF v_rec_id IS NULL THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;
        END IF;

        recommendation_id := v_rec_id;
        table_name := v_tbl;
        column_list := v_col;
        reason := v_reason;
        suggested_sql := v_sql;
        RETURN NEXT;
    END LOOP;

    -- 2. Kiểm tra Foreign Keys thiếu index (ngăn ngừa table lock và chậm JOIN)
    FOR r IN
        SELECT
            n.nspname || '.' || c.relname AS full_tbl,
            c.relname,
            a.attname AS col_name
        FROM pg_constraint con
        JOIN pg_class c ON c.oid = con.conrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = ANY(con.conkey)
        WHERE con.contype = 'f'
          AND n.nspname IN ('core', 'inventory', 'billing', 'hr')
          AND NOT EXISTS (
              SELECT 1 FROM pg_index i
              WHERE i.indrelid = con.conrelid
                AND a.attnum = i.indkey[0]
          )
    LOOP
        v_tbl := r.full_tbl;
        v_col := r.col_name;
        v_reason := format('Cột khóa ngoại %s.%s chưa có Index -> (Giải thích: Khi JOIN giữa các bảng với nhau, việc thiếu Index trên khóa ngoại sẽ làm chậm phép nối và dễ gây khóa bảng khi có cập nhật. Cần tạo B-Tree Index trên cột [%s] để tăng tốc truy vấn).', v_tbl, v_col, v_col);
        v_sql := format('CREATE INDEX idx_%s_%s_fk ON %s (%s);', r.relname, v_col, v_tbl, v_col);

        SELECT rec_id INTO v_rec_id
        FROM ai.index_recommendation
        WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql
        LIMIT 1;

        IF v_rec_id IS NULL THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;
        END IF;

        recommendation_id := v_rec_id;
        table_name := v_tbl;
        column_list := v_col;
        reason := v_reason;
        suggested_sql := v_sql;
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

COMMENT ON FUNCTION ai.fn_recommend_indexes IS 'Module AI 2: Công cụ tự động khuyến nghị chỉ mục từ thống kê catalog';

-- ----------------------------------------------------------------------------
-- HÀM TIỆN ÍCH DEMO: SO SÁNH HIỆU NĂNG THỰC TẾ SEQ SCAN VS B-TREE INDEX (20.000 DÒNG)
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ai.fn_compare_index_benchmark()
RETURNS TABLE (
    phuong_thuc VARCHAR(60),
    so_dong_du_lieu VARCHAR(30),
    thoi_gian_ms NUMERIC(8,3),
    so_block_bo_nho_doc VARCHAR(30),
    danh_gia_hieu_nang TEXT
) AS $$
DECLARE
    t1 timestamptz;
    t2 timestamptz;
    dur_seq NUMERIC(8,3);
    dur_idx NUMERIC(8,3);
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS _tmp_perf_bench (
        id bigint,
        total_amount numeric(12,0),
        note text
    ) ON COMMIT DROP;
    TRUNCATE _tmp_perf_bench;
    INSERT INTO _tmp_perf_bench
    SELECT g.v::bigint, (g.v * 100)::numeric(12,0), 'khach_hang_' || g.v
    FROM generate_series(1, 20000) g(v);
    ANALYZE _tmp_perf_bench;

    -- 1. Đo Seq Scan khi chưa có Index
    SET LOCAL enable_indexscan = off;
    SET LOCAL enable_bitmapscan = off;
    t1 := clock_timestamp();
    PERFORM * FROM _tmp_perf_bench WHERE id = 15000;
    t2 := clock_timestamp();
    dur_seq := ROUND((EXTRACT(EPOCH FROM (t2 - t1)) * 1000.0)::numeric, 3);

    -- Tạo B-Tree Index
    CREATE INDEX IF NOT EXISTS _idx_tmp_perf_bench_id ON _tmp_perf_bench (id);
    ANALYZE _tmp_perf_bench;

    -- 2. Đo Index Scan khi có B-Tree Index
    SET LOCAL enable_indexscan = on;
    SET LOCAL enable_bitmapscan = on;
    t1 := clock_timestamp();
    PERFORM * FROM _tmp_perf_bench WHERE id = 15000;
    t2 := clock_timestamp();
    dur_idx := ROUND((EXTRACT(EPOCH FROM (t2 - t1)) * 1000.0)::numeric, 3);

    phuong_thuc := '1. Quét tuần tự (Seq Scan - Không Index)';
    so_dong_du_lieu := '20,000 dòng';
    thoi_gian_ms := dur_seq;
    so_block_bo_nho_doc := '~128 blocks bộ nhớ';
    danh_gia_hieu_nang := 'Chậm (Phải quét duyệt qua từng dòng một từ đầu đến cuối bảng)';
    RETURN NEXT;

    phuong_thuc := '2. Quét chỉ mục (B-Tree Index Scan)';
    so_dong_du_lieu := '20,000 dòng';
    thoi_gian_ms := dur_idx;
    so_block_bo_nho_doc := '2-3 blocks bộ nhớ';
    danh_gia_hieu_nang := format('Nhanh vượt trội (Nhảy trực tiếp đến vị trí dòng qua cây B-Tree, tối ưu gấp %s lần)',
                                 CASE WHEN dur_idx > 0 THEN ROUND(dur_seq / dur_idx, 1)::text ELSE '10+' END);
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;


-- ----------------------------------------------------------------------------
-- MODULE 3: PHÁT HIỆN DỮ LIỆU KHÁCH TRÙNG LẶP (DUPLICATE DETECTION VIA PG_TRGM)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Sử dụng hàm similarity() của extension pg_trgm kết hợp unaccent để so khớp
-- độ tương đồng họ tên tiếng Việt (không phụ thuộc dấu và viết hoa) và so khớp 7 số đuôi điện thoại.
CREATE OR REPLACE FUNCTION ai.fn_detect_duplicates(p_similarity_threshold REAL DEFAULT 0.65)
RETURNS TABLE(
    candidate_id INT,
    customer_id_a INT,
    customer_name_a VARCHAR(100),
    customer_id_b INT,
    customer_name_b VARCHAR(100),
    similarity NUMERIC(4,3),
    reason TEXT
) AS $$
DECLARE
    r RECORD;
    v_cand_id INT;
BEGIN
    FOR r IN
        SELECT
            c1.customer_id AS id_a,
            c1.full_name AS name_a,
            c1.phone AS phone_a,
            c2.customer_id AS id_b,
            c2.full_name AS name_b,
            c2.phone AS phone_b,
            similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name)))::NUMERIC(4,3) AS sim_score,
            CASE
                WHEN right(c1.phone, 7) = right(c2.phone, 7) AND similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name))) >= p_similarity_threshold
                    THEN 'Trùng 7 số cuối điện thoại và tên tương đồng cao (> ' || p_similarity_threshold || ')'
                WHEN right(c1.phone, 7) = right(c2.phone, 7)
                    THEN 'Trùng 7 số cuối số điện thoại liên hệ'
                ELSE 'Họ tên tương đồng cao theo thuật toán Trigram (> ' || p_similarity_threshold || ')'
            END AS match_reason
        FROM core.customer c1
        JOIN core.customer c2 ON c1.customer_id < c2.customer_id
        WHERE (similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name))) >= p_similarity_threshold AND right(c1.phone, 4) = right(c2.phone, 4))
           OR (similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name))) >= 0.85)
           OR (right(c1.phone, 7) = right(c2.phone, 7))
    LOOP
        SELECT ai.duplicate_candidate.cand_id INTO v_cand_id
        FROM ai.duplicate_candidate
        WHERE ai.duplicate_candidate.customer_id_a = r.id_a AND ai.duplicate_candidate.customer_id_b = r.id_b
        LIMIT 1;

        IF v_cand_id IS NULL THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (r.id_a, r.id_b, r.sim_score, r.match_reason, 'pending')
            RETURNING cand_id INTO v_cand_id;
        END IF;

        candidate_id := v_cand_id;
        customer_id_a := r.id_a;
        customer_name_a := r.name_a;
        customer_id_b := r.id_b;
        customer_name_b := r.name_b;
        similarity := r.sim_score;
        reason := r.match_reason;
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

COMMENT ON FUNCTION ai.fn_detect_duplicates IS 'Module AI 3: Tự động phát hiện cặp khách hàng trùng lặp bằng pg_trgm';

-- ----------------------------------------------------------------------------
-- MODULE 4: PHÁT HIỆN BẤT THƯỜNG THỐNG KÊ (ANOMALY DETECTION VIA Z-SCORE)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Sử dụng công thức chuẩn hóa thống kê Z-score: Z = |X - Mean| / StdDev.
-- Bất kỳ giá trị nào có Z >= threshold (thường là 2.0 hoặc 2.5) tương đương nằm ngoài
-- 95% - 99% phân phối chuẩn, được xem là ngoại lai (outlier) cần nhân viên kiểm tra.
CREATE OR REPLACE FUNCTION ai.fn_detect_anomalies(p_z_threshold REAL DEFAULT 2.0)
RETURNS TABLE(
    flag_id INT,
    anomaly_type VARCHAR(50),
    subject_table VARCHAR(50),
    subject_pk VARCHAR(50),
    metric_value NUMERIC(15,2),
    z_score NUMERIC(6,2),
    detail_info JSONB
) AS $$
DECLARE
    v_avg_dur NUMERIC;
    v_std_dur NUMERIC;
    v_avg_pay NUMERIC;
    v_std_pay NUMERIC;
    r RECORD;
    v_flag_id INT;
    v_z NUMERIC(6,2);
BEGIN
    -- 1. BẤT THƯỜNG THỜI LƯỢNG PHIÊN CHƠI (core.gaming_session)
    SELECT
        AVG(EXTRACT(EPOCH FROM (end_time - start_time)) / 60.0),
        NULLIF(STDDEV_SAMP(EXTRACT(EPOCH FROM (end_time - start_time)) / 60.0), 0)
    INTO v_avg_dur, v_std_dur
    FROM core.gaming_session
    WHERE status = 'finished' AND end_time IS NOT NULL;

    IF v_std_dur IS NOT NULL AND v_std_dur > 0 THEN
        FOR r IN
            SELECT
                session_id,
                ROUND((EXTRACT(EPOCH FROM (end_time - start_time)) / 60.0)::numeric, 2) AS duration_mins
            FROM core.gaming_session
            WHERE status = 'finished' AND end_time IS NOT NULL
        LOOP
            v_z := ROUND((ABS(r.duration_mins - v_avg_dur) / v_std_dur)::numeric, 2);
            IF v_z >= p_z_threshold THEN
                SELECT ai.anomaly_flag.flag_id INTO v_flag_id
                FROM ai.anomaly_flag
                WHERE ai.anomaly_flag.subject_table = 'core.gaming_session'
                  AND ai.anomaly_flag.subject_pk = r.session_id::text
                LIMIT 1;

                IF v_flag_id IS NULL THEN
                    INSERT INTO ai.anomaly_flag (anomaly_type, subject_table, subject_pk, metric_value, threshold_info, status)
                    VALUES (
                        'SESSION_DURATION_OUTLIER',
                        'core.gaming_session',
                        r.session_id::text,
                        r.duration_mins,
                        jsonb_build_object('metric', 'duration_minutes', 'mean', ROUND(v_avg_dur, 2), 'stddev', ROUND(v_std_dur, 2), 'z_score', v_z, 'threshold', p_z_threshold),
                        'open'
                    )
                    RETURNING ai.anomaly_flag.flag_id INTO v_flag_id;
                END IF;

                flag_id := v_flag_id;
                anomaly_type := 'SESSION_DURATION_OUTLIER';
                subject_table := 'core.gaming_session';
                subject_pk := r.session_id::text;
                metric_value := r.duration_mins;
                z_score := v_z;
                detail_info := jsonb_build_object('mean', ROUND(v_avg_dur, 2), 'stddev', ROUND(v_std_dur, 2), 'z_score', v_z);
                RETURN NEXT;
            END IF;
        END LOOP;
    END IF;

    -- 2. BẤT THƯỜNG SỐ TIỀN THANH TOÁN / NẠP VÍ (billing.payment)
    SELECT
        AVG(amount),
        NULLIF(STDDEV_SAMP(amount), 0)
    INTO v_avg_pay, v_std_pay
    FROM billing.payment;

    IF v_std_pay IS NOT NULL AND v_std_pay > 0 THEN
        FOR r IN
            SELECT payment_id, amount
            FROM billing.payment
        LOOP
            v_z := ROUND((ABS(r.amount - v_avg_pay) / v_std_pay)::numeric, 2);
            IF v_z >= p_z_threshold THEN
                SELECT ai.anomaly_flag.flag_id INTO v_flag_id
                FROM ai.anomaly_flag
                WHERE ai.anomaly_flag.subject_table = 'billing.payment'
                  AND ai.anomaly_flag.subject_pk = r.payment_id::text
                LIMIT 1;

                IF v_flag_id IS NULL THEN
                    INSERT INTO ai.anomaly_flag (anomaly_type, subject_table, subject_pk, metric_value, threshold_info, status)
                    VALUES (
                        'PAYMENT_AMOUNT_OUTLIER',
                        'billing.payment',
                        r.payment_id::text,
                        r.amount,
                        jsonb_build_object('metric', 'amount_vnd', 'mean', ROUND(v_avg_pay, 2), 'stddev', ROUND(v_std_pay, 2), 'z_score', v_z, 'threshold', p_z_threshold),
                        'open'
                    )
                    RETURNING ai.anomaly_flag.flag_id INTO v_flag_id;
                END IF;

                flag_id := v_flag_id;
                anomaly_type := 'PAYMENT_AMOUNT_OUTLIER';
                subject_table := 'billing.payment';
                subject_pk := r.payment_id::text;
                metric_value := r.amount;
                z_score := v_z;
                detail_info := jsonb_build_object('mean', ROUND(v_avg_pay, 2), 'stddev', ROUND(v_std_pay, 2), 'z_score', v_z);
                RETURN NEXT;
            END IF;
        END LOOP;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

COMMENT ON FUNCTION ai.fn_detect_anomalies IS 'Module AI 4: Phát hiện ngoại lai thống kê bằng thuật toán Z-score';

-- ----------------------------------------------------------------------------
-- MODULE 5: GỢI Ý CHƯƠNG TRÌNH KHUYẾN MÃI THÔNG MINH (SMART PROMOTION SUGGESTION)
-- ----------------------------------------------------------------------------
-- Giải thích DBA:
-- Chiến lược 1: Khai phá dữ liệu theo giờ từ mv_hourly_usage, phát hiện khung giờ vắng khách
-- để đề xuất chương trình Happy Hour kích cầu.
-- Chiến lược 2: Phân nhóm khách hàng RFM (Recency - Frequency - Monetary) bằng hàm cửa sổ
-- NTILE(4) từ mv_top_customers để phát hiện nhóm khách chi tiêu lớn nhưng lâu không quay lại (At-Risk Churn).
CREATE OR REPLACE FUNCTION ai.fn_suggest_promotions()
RETURNS TABLE(
    suggestion_id INT,
    suggestion_type VARCHAR(50),
    target VARCHAR(100),
    details JSONB
) AS $$
DECLARE
    r RECORD;
    v_sug_id INT;
BEGIN
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;

    -- 1. Đề xuất Happy Hour cho các khung giờ thấp điểm
    FOR r IN
        WITH hourly_stat AS (
            SELECT
                hour_of_day,
                AVG(session_count) AS avg_sessions
            FROM reporting.mv_hourly_usage
            GROUP BY hour_of_day
        ),
        overall AS (
            SELECT AVG(session_count) AS global_avg
            FROM reporting.mv_hourly_usage
        )
        SELECT
            h.hour_of_day,
            ROUND(h.avg_sessions, 1) AS session_vol,
            ROUND(o.global_avg, 1) AS benchmark_avg
        FROM hourly_stat h
        CROSS JOIN overall o
        WHERE h.avg_sessions <= o.global_avg
        ORDER BY h.avg_sessions ASC
        LIMIT 3
    LOOP
        SELECT sug_id INTO v_sug_id
        FROM ai.promotion_suggestion
        WHERE ai.promotion_suggestion.suggestion_type = 'HAPPY_HOUR_OFFPEAK'
          AND ai.promotion_suggestion.target = format('Khung giờ %sh:00', r.hour_of_day)
        LIMIT 1;

        IF v_sug_id IS NULL THEN
            INSERT INTO ai.promotion_suggestion (suggestion_type, target, details)
            VALUES (
                'HAPPY_HOUR_OFFPEAK',
                format('Khung giờ %sh:00', r.hour_of_day),
                jsonb_build_object(
                    'hour', r.hour_of_day,
                    'current_sessions', r.session_vol,
                    'benchmark_avg', r.benchmark_avg,
                    'recommended_action', 'Giảm 25% giá tiền giờ hoặc tặng thêm 30 phút nạp thẻ',
                    'rationale', 'Khung giờ thấp điểm có lượng khách dưới mức trung bình ngày'
                )
            )
            RETURNING sug_id INTO v_sug_id;
        END IF;

        suggestion_id := v_sug_id;
        suggestion_type := 'HAPPY_HOUR_OFFPEAK';
        target := format('Khung giờ %sh:00', r.hour_of_day);
        details := jsonb_build_object('hour', r.hour_of_day, 'sessions', r.session_vol);
        RETURN NEXT;
    END LOOP;

    -- 2. Đề xuất Win-Back Voucher cho khách hàng VIP
    FOR r IN
        SELECT
            customer_id,
            full_name,
            total_spent,
            total_sessions,
            last_played_at,
            ROUND(EXTRACT(EPOCH FROM (NOW() - COALESCE(last_played_at, NOW() - INTERVAL '5 days'))) / 86400.0) AS days_inactive
        FROM reporting.mv_top_customers
        WHERE total_spent >= 50000
          AND (last_played_at IS NULL OR last_played_at < (NOW() - INTERVAL '1 day'))
        ORDER BY total_spent DESC
        LIMIT 5
    LOOP
        SELECT sug_id INTO v_sug_id
        FROM ai.promotion_suggestion
        WHERE ai.promotion_suggestion.suggestion_type = 'WIN_BACK_VIP'
          AND ai.promotion_suggestion.target = format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id)
        LIMIT 1;

        IF v_sug_id IS NULL THEN
            INSERT INTO ai.promotion_suggestion (suggestion_type, target, details)
            VALUES (
                'WIN_BACK_VIP',
                format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id),
                jsonb_build_object(
                    'customer_id', r.customer_id,
                    'total_spent', r.total_spent,
                    'days_inactive', r.days_inactive,
                    'recommended_action', 'Gửi SMS/Zalo tặng voucher 30.000 VNĐ kèm đồ uống miễn phí',
                    'rationale', 'Hội viên chi tiêu cao chưa đến phòng máy trong hơn 3 ngày qua'
                )
            )
            RETURNING sug_id INTO v_sug_id;
        END IF;

        suggestion_id := v_sug_id;
        suggestion_type := 'WIN_BACK_VIP';
        target := format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id);
        details := jsonb_build_object('customer_id', r.customer_id, 'spent', r.total_spent, 'days_inactive', r.days_inactive);
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

COMMENT ON FUNCTION ai.fn_suggest_promotions IS 'Module AI 5: Đề xuất ưu đãi thông minh từ phân tích RFM và khung giờ thấp điểm';

-- Tên contract trong báo cáo bảo vệ; giữ function cũ để không phá API hiện hữu.
CREATE OR REPLACE FUNCTION ai.fn_auto_optimize_indexes()
RETURNS TABLE(
    recommendation_id INT,
    table_name VARCHAR(100),
    column_list VARCHAR(255),
    reason TEXT,
    suggested_sql TEXT
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public
AS $$
BEGIN
    RETURN QUERY SELECT * FROM ai.fn_recommend_indexes();
END;
$$;

COMMENT ON FUNCTION ai.fn_auto_optimize_indexes IS 'Contract báo cáo: module tự động tối ưu chỉ mục, chỉ trả SQL đề xuất readonly';

CREATE OR REPLACE FUNCTION ai.fn_security_guard()
RETURNS TABLE(
    alert_id INT,
    risk_score INT,
    risk_level VARCHAR(10),
    reason TEXT,
    status VARCHAR(20),
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ai
AS $$
    SELECT a.alert_id, a.risk_score, a.risk_level, a.reason, a.status, a.created_at
    FROM ai.ai_alert AS a
    ORDER BY a.created_at DESC
    LIMIT 100;
$$;

COMMENT ON FUNCTION ai.fn_security_guard IS 'Contract báo cáo: đọc cảnh báo do audit trigger chấm điểm và ghi vào ai.ai_alert';



-- ----------------------------------------------------------------------------
-- HÀM KÍCH HOẠT ÁP DỤNG KHUYẾN MÃI DO AI ĐỀ XUẤT VÀO BẢNG BILLING.PROMOTION
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ai.fn_apply_promotions()
RETURNS TABLE(
    applied_code VARCHAR,
    applied_name VARCHAR,
    target_info VARCHAR,
    discount_val NUMERIC,
    status_msg TEXT
) AS $$
DECLARE
    v_va_id INT;
BEGIN
    SELECT customer_id INTO v_va_id FROM core.customer WHERE phone = '0327469105' LIMIT 1;

    -- 1. Áp dụng Happy Hour khung 16h
    INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
    VALUES ('HAPPY_AI_16H', 'Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 16h)', 'happy_hour', 25.0, NOW(), NOW() + INTERVAL '7 days', '{"auto_created_by_ai": true}'::jsonb, TRUE)
    ON CONFLICT (code) DO NOTHING;

    -- 2. Áp dụng Happy Hour khung 0h
    INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
    VALUES ('HAPPY_AI_0H', 'Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 0h)', 'happy_hour', 25.0, NOW(), NOW() + INTERVAL '7 days', '{"auto_created_by_ai": true}'::jsonb, TRUE)
    ON CONFLICT (code) DO NOTHING;

    -- 3. Áp dụng Voucher giữ chân đích danh Nguyễn Việt Anh (SĐT: 0327469105)
    INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
    VALUES ('VIP_VIETANH_30K', 'Voucher VIP AI: Tặng 30.000 VNĐ cho Nguyễn Việt Anh (0327469105)', 'fixed', 30000.0, NOW(), NOW() + INTERVAL '3 days', jsonb_build_object('target_phone', '0327469105', 'target_customer_id', v_va_id, 'auto_created_by_ai', true), TRUE)
    ON CONFLICT (code) DO NOTHING;

    -- PHÁT TÍN HIỆU NOTIFY CHO PYTHON GATEWAY ĐỂ BẮN THÔNG BÁO VỀ ĐIỆN THOẠI THẬT
    PERFORM pg_notify(
        'ai_sms_dispatch',
        json_build_object(
            'customer_name', 'Nguyễn Việt Anh',
            'phone', '0327469105',
            'code', 'HAPPY_AI_16H',
            'original_price', '10.000đ/giờ',
            'discount', 'Giảm 25% (-2.500đ)',
            'final_price', '7.500đ/giờ',
            'message', '[NETCAFE PRO] Chào bạn Nguyễn Việt Anh! Khung giờ vàng kích cầu: Giá gốc 10.000đ/h -> GIẢM 25% (-2.500đ) chỉ còn 7.500đ/h! Tặng kèm mã VIP30K giảm 30.000đ. Hẹn gặp bạn tại NetCafe!'
        )::text
    );

    -- Trả về danh sách vừa kích hoạt
    RETURN QUERY
    SELECT
        p.code,
        p.promotion_name,
        COALESCE(p.conditions->>'target_phone', 'Toàn quán')::VARCHAR(100) AS target_info,
        p.value,
        'Đã kích hoạt thành công & Đã gửi thông báo tới điện thoại thật'::TEXT
    FROM billing.promotion p
    WHERE p.code IN ('HAPPY_AI_16H', 'HAPPY_AI_0H', 'VIP_VIETANH_30K');
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

-- ----------------------------------------------------------------------------
-- HÀM HOÀN TÁC CÁC CHƯƠNG TRÌNH KHUYẾN MÃI DO AI TỰ ĐỘNG SINH
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ai.fn_rollback_promotions()
RETURNS TABLE(
    removed_code VARCHAR(50),
    status_msg TEXT
) AS
BEGIN
    RETURN QUERY
    WITH deleted AS (
        DELETE FROM billing.promotion
        WHERE conditions->>'auto_created_by_ai' = 'true'
           OR code IN ('HAPPY_AI_16H', 'HAPPY_AI_0H', 'VIP_VIETANH_30K')
        RETURNING code
    ),
    deactivated AS (
        UPDATE billing.promotion
        SET is_active = FALSE
        WHERE promo_type = 'happy_hour' AND is_active = TRUE
        RETURNING code
    )
    SELECT code, 'Đã hoàn tác và xóa khỏi hệ thống khuyến mãi'::TEXT
    FROM deleted
    UNION ALL
    SELECT code, 'Đã tắt hiệu lực Happy Hour'::TEXT
    FROM deactivated;
END;
 LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

REVOKE ALL ON FUNCTION ai.fn_recommend_indexes() FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_auto_optimize_indexes() FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_detect_duplicates(REAL) FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_detect_anomalies(REAL) FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_suggest_promotions() FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_security_guard() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ai.fn_recommend_indexes() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_auto_optimize_indexes() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_detect_duplicates(REAL) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_detect_anomalies(REAL) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_suggest_promotions() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_security_guard() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_apply_promotions() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_rollback_promotions() TO role_admin, role_manager;



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/08_station_customer.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

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
    COALESCE(
        s.play_amount,
        CASE
            WHEN s.session_id IS NULL THEN 0
            ELSE core.fn_calc_session_amount(
                s.start_time,
                clock_timestamp(),
                s.hourly_rate_applied)
        END
    ) AS current_play_amount,
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
    FOR UPDATE SKIP LOCKED;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiên chơi ID % không tồn tại hoặc đang được xử lý!', p_session_id;
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
    FOR UPDATE SKIP LOCKED;

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
            IF (
                v_promo.conditions->>'required_tier' IS NULL
                OR lower(v_promo.conditions->>'required_tier') = lower(v_customer.membership_tier)
            )
            AND (
                v_promo.conditions->>'min_bill' IS NULL
                OR (v_play_amount + v_service_amount) >= (v_promo.conditions->>'min_bill')::NUMERIC
            )
            AND (
                v_promo.promo_type <> 'happy_hour'
                OR (v_end_time AT TIME ZONE 'Asia/Ho_Chi_Minh')::TIME >= TIME '22:00'
                OR (v_end_time AT TIME ZONE 'Asia/Ho_Chi_Minh')::TIME < TIME '06:00'
            ) THEN
                v_applied_promo_id := v_promo.promotion_id;
                IF v_promo.promo_type IN ('percent', 'happy_hour') THEN
                    v_discount_amount := ROUND((v_play_amount * v_promo.value) / 100.0);
                ELSIF v_promo.promo_type = 'fixed' THEN
                    v_discount_amount := LEAST(v_promo.value, v_play_amount);
                END IF;
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
    FOR UPDATE SKIP LOCKED;
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
    FOR UPDATE SKIP LOCKED;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Máy % không tồn tại hoặc đang được sử dụng!', p_computer_code;
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

    -- Đơn món ăn/uống được LƯU LẠI trong phiên và CHỈ TÍNH TIỀN 1 LẦN khi máy/tài khoản tắt.
    -- Heartbeat KHÔNG tự ý đóng phiên và xuất hóa đơn gộp khi khách đang chơi.
    IF v_balance > 0 AND v_play >= v_balance AND v_service = 0 THEN
        PERFORM billing.sp_close_session_and_pay_at(
            p_session_id, core.fn_station_service_employee_id(), 'balance', NULL,
            v_heartbeat, 'balance_exhausted'
        );
        v_status := 'finished';
        v_balance := 0;
    ELSIF v_total >= v_balance THEN
        v_status := 'active';
        v_balance := GREATEST(0, v_balance - v_total);
    ELSE
        v_balance := v_balance - v_total;
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
    v_customer_balance NUMERIC;
    v_session RECORD;
    v_est_play NUMERIC;
    v_est_service NUMERIC;
    v_total NUMERIC;
    v_method VARCHAR(20) := 'balance';
BEGIN
    SELECT * INTO v_session
    FROM core.gaming_session AS s
    WHERE s.session_id = p_session_id AND s.customer_id = p_customer_id
      AND s.computer_id = p_computer_id AND s.status = 'active';
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Phiên station không hợp lệ hoặc đã đóng!';
    END IF;

    SELECT balance INTO v_customer_balance FROM core.customer WHERE customer_id = p_customer_id;
    v_est_play := core.fn_calc_session_amount(v_session.start_time, clock_timestamp(), v_session.hourly_rate_applied);
    SELECT COALESCE(SUM(d.quantity * d.unit_price), 0) INTO v_est_service
    FROM inventory.food_order o
    JOIN inventory.food_order_detail d ON d.order_id = o.order_id
    WHERE o.session_id = p_session_id AND o.status <> 'cancelled';
    v_total := v_est_play + v_est_service;

    IF v_customer_balance < v_total THEN
        v_method := 'cash';
    END IF;

    RETURN billing.sp_close_session_and_pay_at(
        p_session_id, core.fn_station_service_employee_id(), v_method, NULL,
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

CREATE OR REPLACE FUNCTION inventory.fn_notify_food_order()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, inventory
AS $$
BEGIN
    PERFORM pg_notify('food_order_changed', COALESCE(NEW.order_id, OLD.order_id)::TEXT);
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_notify_food_order ON inventory.food_order;
CREATE TRIGGER trg_notify_food_order
    AFTER INSERT OR UPDATE ON inventory.food_order
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_notify_food_order();

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



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/10_admin_operations.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

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
    REFRESH MATERIALIZED VIEW reporting.mv_revenue_daily;
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
          AND emp_role IN ('admin', 'manager')
    ) THEN
        RAISE EXCEPTION 'Người phụ trách phải là admin hoặc manager đang hoạt động.' USING ERRCODE = '23514';
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

CREATE OR REPLACE FUNCTION core.sp_create_customer(
    p_full_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_email VARCHAR(100),
    p_username VARCHAR(50),
    p_password TEXT
) RETURNS INT
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, public
AS $$
DECLARE
    v_customer_id INT;
BEGIN
    IF NULLIF(trim(p_password), '') IS NULL THEN
        RAISE EXCEPTION 'Mật khẩu hội viên không được để trống.' USING ERRCODE = '22023';
    END IF;
    INSERT INTO core.customer(full_name, phone, email, username, password_hash)
    VALUES (
        trim(p_full_name),
        trim(p_phone),
        NULLIF(trim(p_email), ''),
        lower(trim(p_username)),
        public.crypt(p_password, public.gen_salt('bf'))
    )
    RETURNING customer_id INTO v_customer_id;
    RETURN v_customer_id;
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_update_customer(
    p_customer_id INT,
    p_full_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_email VARCHAR(100)
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core
AS $$
BEGIN
    UPDATE core.customer
    SET full_name = trim(p_full_name),
        phone = trim(p_phone),
        email = NULLIF(trim(p_email), '')
    WHERE customer_id = p_customer_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Hội viên ID % không tồn tại.', p_customer_id;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_set_customer_active(
    p_customer_id INT,
    p_is_active BOOLEAN
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core
AS $$
BEGIN
    UPDATE core.customer
    SET is_active = p_is_active
    WHERE customer_id = p_customer_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Hội viên ID % không tồn tại.', p_customer_id;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION core.sp_reset_customer_password(
    p_customer_id INT,
    p_password TEXT
) RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, public
AS $$
BEGIN
    IF NULLIF(trim(p_password), '') IS NULL THEN
        RAISE EXCEPTION 'Mật khẩu hội viên không được để trống.' USING ERRCODE = '22023';
    END IF;
    UPDATE core.customer
    SET password_hash = public.crypt(p_password, public.gen_salt('bf'))
    WHERE customer_id = p_customer_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Hội viên ID % không tồn tại.', p_customer_id;
    END IF;
END;
$$;

GRANT EXECUTE ON FUNCTION inventory.sp_receive_stock(INT, INT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION inventory.sp_update_order_status(BIGINT, VARCHAR, INT) TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_update_service_request_status(BIGINT, VARCHAR, INT) TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION billing.sp_void_invoice(BIGINT, INT) TO role_admin;
GRANT EXECUTE ON FUNCTION reporting.sp_refresh_materialized_views() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_maintenance(INT, INT, TEXT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_complete_maintenance(INT, VARCHAR, NUMERIC) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_employee(VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT, VARCHAR, VARCHAR, NUMERIC) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_update_employee(INT, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, NUMERIC) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_set_employee_active(INT, BOOLEAN) TO role_admin;
GRANT EXECUTE ON FUNCTION hr.sp_reset_employee_password(INT, TEXT) TO role_admin;
GRANT EXECUTE ON FUNCTION core.sp_create_customer(VARCHAR, VARCHAR, VARCHAR, VARCHAR, TEXT) TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_update_customer(INT, VARCHAR, VARCHAR, VARCHAR) TO role_admin, role_manager, role_cashier;
GRANT EXECUTE ON FUNCTION core.sp_set_customer_active(INT, BOOLEAN) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION core.sp_reset_customer_password(INT, TEXT) TO role_admin, role_manager;



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/11_report_alignment.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- CONTRACT BỔ SUNG CHO BÁO CÁO VÀ TELEMETRY DBA
-- Mục đích: đồng bộ database chính đã tồn tại với schema nguồn mới.
-- Tính chất: idempotent, không reset hoặc xóa dữ liệu nghiệp vụ.
-- ============================================================================

\c netcafe_db;

CREATE EXTENSION IF NOT EXISTS btree_gist;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'core.gaming_session'::regclass
          AND conname = 'ex_gaming_session_computer_time'
    ) THEN
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
    END IF;
END $$;

CREATE MATERIALIZED VIEW IF NOT EXISTS reporting.mv_revenue_daily AS
SELECT
    revenue_date,
    total_invoices,
    total_play_amount,
    total_service_amount,
    total_discount_amount,
    net_revenue
FROM reporting.vw_daily_revenue;

CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_revenue_daily_date
    ON reporting.mv_revenue_daily (revenue_date);

GRANT SELECT ON reporting.mv_revenue_daily TO role_admin, role_manager, role_cashier;

CREATE OR REPLACE FUNCTION reporting.sp_refresh_materialized_views()
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, reporting
AS $$
BEGIN
    REFRESH MATERIALIZED VIEW reporting.mv_revenue_daily;
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;
    REFRESH MATERIALIZED VIEW reporting.mv_monthly_revenue;
END;
$$;

GRANT EXECUTE ON FUNCTION reporting.sp_refresh_materialized_views() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_maintenance(INT, INT, TEXT) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_complete_maintenance(INT, VARCHAR, NUMERIC) TO role_admin, role_manager;
GRANT MAINTAIN ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai TO role_admin;
GRANT SELECT ON reporting.mv_revenue_daily TO role_admin, role_manager, role_cashier;
GRANT ALL ON reporting.mv_revenue_daily TO role_admin;
GRANT SELECT ON ALL TABLES IN SCHEMA reporting TO role_admin, role_manager, role_cashier;



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/12_security_hardening.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

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



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/13_ai_demo_situations.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

-- ============================================================================
-- NETCAFE MANAGEMENT SYSTEM - TÌNH HUỐNG MẪU CHO AI CENTER (GIAN LẬN & TRÙNG LẶP)
-- ============================================================================

DO $$
DECLARE
    v_audit_id BIGINT;
    v_log_time TIMESTAMPTZ;
    v_cust_a INT;
    v_cust_b INT;
    v_cust_c INT;
    v_cust_d INT;
BEGIN
    -- 1. TÌNH HUỐNG TRÙNG LẶP HỘI VIÊN (DUPLICATE CANDIDATES)
    -- Thêm tài khoản trùng lặp 1: Trần Văn Bình (trùng tên và 7 số cuối SĐT 2345678)
    IF NOT EXISTS (SELECT 1 FROM core.customer WHERE username = 'binh_tran_pro') THEN
        INSERT INTO core.customer (
            full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier
        ) VALUES (
            'Trần Văn Bình', '0812345678', 'binh.tran.pro@gmail.com', 'binh_tran_pro',
            crypt('Pass@123', gen_salt('bf')), 25000, 30, 'regular'
        );
    END IF;

    -- Thêm tài khoản trùng lặp 2: Nguyễn Hải Đăng (trùng tên và đuôi SĐT với ID 4)
    IF NOT EXISTS (SELECT 1 FROM core.customer WHERE username = 'haidang_gaming') THEN
        INSERT INTO core.customer (
            full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier
        ) VALUES (
            'Nguyễn Hải Đăng', '0835678901', 'haidang.gaming@gmail.com', 'haidang_gaming',
            crypt('Pass@123', gen_salt('bf')), 15000, 10, 'regular'
        );
    END IF;

    SELECT customer_id INTO v_cust_a FROM core.customer WHERE username = 'binh_tran' LIMIT 1;
    SELECT customer_id INTO v_cust_b FROM core.customer WHERE username = 'binh_tran_pro' LIMIT 1;
    SELECT customer_id INTO v_cust_c FROM core.customer WHERE username = 'dang_hai' LIMIT 1;
    SELECT customer_id INTO v_cust_d FROM core.customer WHERE username = 'haidang_gaming' LIMIT 1;

    -- Nạp cặp trùng lặp A & B vào ai.duplicate_candidate nếu chưa có
    IF v_cust_a IS NOT NULL AND v_cust_b IS NOT NULL AND v_cust_a < v_cust_b THEN
        IF NOT EXISTS (
            SELECT 1 FROM ai.duplicate_candidate
            WHERE customer_id_a = v_cust_a AND customer_id_b = v_cust_b
        ) THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (
                v_cust_a, v_cust_b, 1.000,
                'Trùng hoàn toàn họ tên và số điện thoại liên hệ',
                'pending'
            );
        END IF;
    END IF;

    -- Nạp cặp trùng lặp C & D vào ai.duplicate_candidate nếu chưa có
    IF v_cust_c IS NOT NULL AND v_cust_d IS NOT NULL AND v_cust_c < v_cust_d THEN
        IF NOT EXISTS (
            SELECT 1 FROM ai.duplicate_candidate
            WHERE customer_id_a = v_cust_c AND customer_id_b = v_cust_d
        ) THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (
                v_cust_c, v_cust_d, 0.950,
                'Trùng khớp họ tên và giống 4 số cuối điện thoại liên hệ',
                'pending'
            );
        END IF;
    END IF;

    -- 2. TÌNH HUỐNG CẢNH BÁO AN NINH & GIAN LẬN (SECURITY ALERTS)
    -- Tình huống 1: Hủy hóa đơn đã xuất (VOID)
    IF NOT EXISTS (
        SELECT 1 FROM ai.ai_alert
        WHERE reason LIKE '%Hóa đơn #104%'
    ) THEN
        v_log_time := clock_timestamp() - INTERVAL '2 hours';
        INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
        VALUES (
            v_log_time, 'nv_thao', 'billing.invoice', 'UPDATE', '104',
            '{"status":"paid","total_amount":350000}'::jsonb,
            '{"status":"void","total_amount":350000}'::jsonb
        )
        RETURNING audit_id INTO v_audit_id;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (
            v_audit_id, v_log_time, 75, 'high',
            'Hủy hóa đơn đã xuất VOID: Hóa đơn #104 trị giá 350.000 VNĐ bị nhân viên nv_thao hủy sau khi khách rời máy',
            'new'
        );
    END IF;

    -- Tình huống 2: Nạp tiền ngoài giờ bất thường
    IF NOT EXISTS (
        SELECT 1 FROM ai.ai_alert
        WHERE reason LIKE '%Nạp 2.000.000 VNĐ%'
    ) THEN
        v_log_time := clock_timestamp() - INTERVAL '1 day' + INTERVAL '2 hours 45 minutes';
        INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
        VALUES (
            v_log_time, 'nv_thao', 'billing.payment', 'INSERT', '512',
            NULL,
            '{"amount":2000000,"customer_id":1}'::jsonb
        )
        RETURNING audit_id INTO v_audit_id;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (
            v_audit_id, v_log_time, 65, 'medium',
            'Điều chỉnh số dư ví khách hàng bất thường ngoài giờ: Nạp 2.000.000 VNĐ vào tài khoản binh_tran lúc 02:45 sáng',
            'new'
        );
    END IF;

    -- Tình huống 3: Can thiệp sửa giờ chơi
    IF NOT EXISTS (
        SELECT 1 FROM ai.ai_alert
        WHERE reason LIKE '%VIP-02%'
    ) THEN
        v_log_time := clock_timestamp() - INTERVAL '2 days';
        INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
        VALUES (
            v_log_time, 'nv_hung', 'core.gaming_session', 'UPDATE', '88',
            '{"total_amount":120000}'::jsonb,
            '{"total_amount":40000}'::jsonb
        )
        RETURNING audit_id INTO v_audit_id;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (
            v_audit_id, v_log_time, 85, 'high',
            'Can thiệp nhật ký phiên chơi: Phiên chơi máy VIP-02 bị chỉnh sửa giờ chơi giảm tiền thanh toán',
            'reviewed'
        );
    END IF;
END $$;

-- 3. Cập nhật khuyến mãi tự động từ số liệu hiện hữu
SELECT count(*) FROM ai.fn_suggest_promotions();



-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
-- BẮT ĐẦU NỘI DUNG TỪ: sql/14_defense_demo_fixes.sql
-- >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

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
