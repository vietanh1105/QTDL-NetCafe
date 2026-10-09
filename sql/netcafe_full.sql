-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: netcafe_full.sql (TẬP LỆNH TỔNG HỢP TOÀN BỘ HỆ THỐNG)
-- Mô tả: File duy nhất cho phép giáo viên tái tạo toàn bộ hệ thống từ một máy chủ trống
--        Bao gồm: Database -> Schemas -> Tables -> Contraints -> Functions/Triggers 
--                 -> Views -> Security (RBAC/RLS) -> Indexes & Partition -> Seed Data -> 5 AI Modules
-- Tính chất: 100% IDEMPOTENT - Có thể chạy lại nhiều lần mà không phát sinh lỗi.
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
CREATE EXTENSION IF NOT EXISTS btree_gist;    -- Ràng buộc loại trừ EXCLUDE trên kiểu vô hướng + dải thời gian
CREATE EXTENSION IF NOT EXISTS pg_trgm;       -- Tìm kiếm mờ trigram, đo độ tương đồng similarity
CREATE EXTENSION IF NOT EXISTS unaccent;      -- Bỏ dấu tiếng Việt phục vụ tìm kiếm

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

DROP TABLE IF EXISTS core.booking CASCADE;
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

-- 4.6. Đặt máy trước (Booking)
CREATE TABLE core.booking (
    booking_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id INT NOT NULL REFERENCES core.customer(customer_id) ON DELETE RESTRICT,
    computer_id INT NOT NULL REFERENCES core.computer(computer_id) ON DELETE RESTRICT,
    booked_from TIMESTAMPTZ NOT NULL,
    booked_to TIMESTAMPTZ NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT ck_booking_status CHECK (status IN ('pending', 'confirmed', 'checked_in', 'cancelled', 'no_show')),
    CONSTRAINT ck_booking_time_order CHECK (booked_to > booked_from),
    -- Khóa loại trừ: Chống 2 khách đặt trùng 1 máy trong cùng dải thời gian
    CONSTRAINT ex_booking_no_overlap EXCLUDE USING gist (
        computer_id WITH =,
        tstzrange(booked_from, booked_to) WITH &&
    ) WHERE (status IN ('pending', 'confirmed'))
);
COMMENT ON TABLE core.booking IS 'Đặt lịch máy trước: Khóa EXCLUDE USING gist ngăn chặn overbooking tại CSDL';

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

-- 2.1. Doanh thu theo tháng (Cần UNIQUE INDEX để REFRESH CONCURRENTLY)
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

-- 2.2. Top khách hàng thân thiết và chi tiêu cao nhất (kèm lần chơi gần nhất cho RFM)
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

-- 2.3. Thống kê tần suất chơi theo giờ trong tuần (Phục vụ Module AI 5: Dự báo Happy Hour)
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
    -- Nhóm role NOLOGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_admin') THEN
        CREATE ROLE role_admin NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_manager') THEN
        CREATE ROLE role_manager NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_cashier') THEN
        CREATE ROLE role_cashier NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_staff') THEN
        CREATE ROLE role_staff NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_technician') THEN
        CREATE ROLE role_technician NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'role_report_readonly') THEN
        CREATE ROLE role_report_readonly NOLOGIN;
    END IF;

    -- User đăng nhập demo
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_admin') THEN
        CREATE ROLE nv_admin WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_admin;
    ELSE
        ALTER ROLE nv_admin WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_manager') THEN
        CREATE ROLE nv_manager WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_manager;
    ELSE
        ALTER ROLE nv_manager WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_thao') THEN
        CREATE ROLE nv_thao WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_cashier;
    ELSE
        ALTER ROLE nv_thao WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_nam') THEN
        CREATE ROLE nv_nam WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_cashier;
    ELSE
        ALTER ROLE nv_nam WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_tuan') THEN
        CREATE ROLE nv_tuan WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_staff;
    ELSE
        ALTER ROLE nv_tuan WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_hung') THEN
        CREATE ROLE nv_hung WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_technician;
    ELSE
        ALTER ROLE nv_hung WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
END $$;

-- Gán role nhóm
GRANT role_admin TO nv_admin;
GRANT role_manager TO nv_manager;
GRANT role_cashier TO nv_thao, nv_nam;
GRANT role_staff TO nv_tuan;
GRANT role_technician TO nv_hung;

-- ----------------------------------------------------------------------------
-- 4. PHÂN QUYỀN TRUY CẬP SCHEMAS (USAGE)
-- ----------------------------------------------------------------------------
-- Admin & Manager truy cập toàn bộ các schema
GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting, audit, ai TO role_admin, role_manager;

-- Thu ngân (Cashier)
GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting TO role_cashier;

-- Nhân viên phục vụ (Staff)
GRANT USAGE ON SCHEMA core, inventory, reporting TO role_staff;

-- Kỹ thuật viên (Technician)
GRANT USAGE ON SCHEMA core, hr, reporting TO role_technician;

-- ----------------------------------------------------------------------------
-- 5. CẤP QUYỀN TRÊN BẢNG VÀ CỘT (TABLE & COLUMN-LEVEL PRIVILEGES)
-- ----------------------------------------------------------------------------

-- 5.1. Quyền trên schema core
GRANT SELECT, INSERT, UPDATE ON core.computer, core.customer, core.gaming_session, core.booking TO role_cashier;
GRANT SELECT ON core.room, core.computer_type TO role_cashier, role_staff, role_technician;
GRANT SELECT ON core.computer, core.gaming_session TO role_staff, role_technician;

-- 5.2. Quyền trên schema inventory
GRANT SELECT, INSERT, UPDATE ON inventory.food_order, inventory.food_order_detail TO role_cashier, role_staff;
GRANT SELECT ON inventory.product_category, inventory.product TO role_cashier, role_staff;
GRANT UPDATE (stock_qty) ON inventory.product TO role_cashier, role_staff;

-- 5.3. Quyền trên schema billing
GRANT SELECT, INSERT ON billing.invoice, billing.invoice_promotion, billing.payment TO role_cashier;
GRANT SELECT ON billing.promotion TO role_cashier;

-- 5.4. Quyền trên schema reporting
GRANT SELECT ON ALL TABLES IN SCHEMA reporting TO role_admin, role_manager, role_cashier, role_report_readonly;

-- 5.5. Quyền trên schema ai (Chỉ Admin và Manager được đọc và xử lý)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA ai TO role_admin, role_manager;

-- 5.6. Admin toàn quyền
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA core, inventory, billing, hr, reporting, audit, ai TO role_admin;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA core, inventory, billing, hr, reporting, ai TO role_manager;

-- 5.7. COLUMN-LEVEL SECURITY: Bảo vệ cột lương (salary) và password_hash
-- Thu ngân, nhân viên, kỹ thuật viên CHỈ được SELECT các cột không nhạy cảm của employee:
REVOKE ALL ON hr.employee FROM role_cashier, role_staff, role_technician;
GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_cashier, role_staff, role_technician;

-- Cung cấp View an toàn công khai thông tin nhân viên
CREATE OR REPLACE VIEW hr.vw_employee_public AS
SELECT employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active
FROM hr.employee;
GRANT SELECT ON hr.vw_employee_public TO role_cashier, role_staff, role_technician;

-- Cấp quyền trên hr.maintenance cho kỹ thuật viên
GRANT SELECT, UPDATE ON hr.maintenance TO role_technician;

-- Cấp quyền trên sequences để có thể INSERT Identity
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA core, inventory, billing, hr, ai TO role_cashier, role_staff, role_technician;

-- 5.8. Cấp quyền thực thi Stored Functions (EXECUTE)
GRANT EXECUTE ON FUNCTION core.fn_calc_session_amount TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION core.fn_membership_tier TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION core.sp_open_session TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION billing.sp_close_session_and_pay TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION billing.sp_top_up TO role_cashier, role_manager, role_admin;
GRANT EXECUTE ON FUNCTION inventory.sp_place_order TO role_cashier, role_staff, role_manager, role_admin;

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
GRANT EXECUTE ON FUNCTION hr.fn_get_current_employee_id() TO role_cashier, role_staff, role_technician, role_manager, role_admin;

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
    TO role_cashier, role_staff, role_technician
    USING (employee_id = hr.fn_get_current_employee_id());

-- 7.4. RLS trên bảng hr.maintenance: Kỹ thuật viên chỉ thấy ca bảo trì giao cho mình
ALTER TABLE hr.maintenance ENABLE ROW LEVEL SECURITY;
ALTER TABLE hr.maintenance FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_maint_admin_manager ON hr.maintenance;
CREATE POLICY p_maint_admin_manager ON hr.maintenance
    FOR ALL
    TO role_admin, role_manager
    USING (TRUE)
    WITH CHECK (TRUE);

DROP POLICY IF EXISTS p_maint_tech ON hr.maintenance;
CREATE POLICY p_maint_tech ON hr.maintenance
    FOR ALL
    TO role_technician
    USING (technician_id = hr.fn_get_current_employee_id())
    WITH CHECK (technician_id = hr.fn_get_current_employee_id());



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
         inventory.food_order_detail, inventory.food_order, core.booking,
         core.gaming_session, inventory.product, inventory.product_category,
         core.customer, core.computer, core.computer_type, core.room,
         hr.employee, billing.promotion RESTART IDENTITY CASCADE;

-- 2. NHÂN SỰ VÀ TÀI KHOẢN HỆ THỐNG (ĐỦ 5 VAI TRÒ)
INSERT INTO hr.employee (full_name, phone, username, db_username, password_hash, emp_role, default_shift, salary, hired_date)
VALUES 
    ('Nguyễn Văn An', '0981234567', 'an_nguyen', 'nv_admin', crypt('Demo@123', gen_salt('bf')), 'admin', 'morning', 20000000, '2024-01-15'),
    ('Lê Quang Minh', '0972345678', 'minh_le', 'nv_manager', crypt('Demo@123', gen_salt('bf')), 'manager', 'morning', 15000000, '2024-03-01'),
    ('Phạm Thu Thảo', '0913456789', 'thao_pham', 'nv_thao', crypt('Demo@123', gen_salt('bf')), 'cashier', 'morning', 8500000, '2024-06-10'),
    ('Trần Đức Nam', '0934567890', 'nam_tran', 'nv_nam', crypt('Demo@123', gen_salt('bf')), 'cashier', 'afternoon', 8500000, '2024-06-15'),
    ('Vũ Anh Tuấn', '0965678901', 'tuan_vu', 'nv_tuan', crypt('Demo@123', gen_salt('bf')), 'staff', 'afternoon', 7000000, '2024-08-01'),
    ('Trần Mạnh Hùng', '0986789012', 'hung_tran', 'nv_hung', crypt('Demo@123', gen_salt('bf')), 'technician', 'morning', 11000000, '2024-04-20');

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

-- 8. DANH SÁCH KHÁCH HÀNG MẪU (20 KHÁCH CÓ SỐ DƯ TÀI KHOẢN)
INSERT INTO core.customer (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier)
VALUES 
    ('Trần Văn Bình', '0912345678', 'binh.tran@gmail.com', 'binh_tran', crypt('Pass@123', gen_salt('bf')), 80000, 120, 'silver'),
    ('Hoàng Đức Anh', '0983456789', 'anh.hoang@gmail.com', 'anh_hoang', crypt('Pass@123', gen_salt('bf')), 150000, 650, 'gold'),
    ('Đặng Thùy Dung', '0974567890', 'dung.dang@gmail.com', 'dung_dang', crypt('Pass@123', gen_salt('bf')), 250000, 2200, 'vip'),
    ('Nguyễn Hải Đăng', '0935678901', 'dang.hai@gmail.com', 'dang_hai', crypt('Pass@123', gen_salt('bf')), 50000, 45, 'regular'),
    ('Lê Bảo Châu', '0966789012', 'chau.le@gmail.com', 'chau_le', crypt('Pass@123', gen_salt('bf')), 120000, 520, 'gold'),
    ('Phan Thanh Tùng', '0987890123', 'tung.phan@gmail.com', 'tung_phan', crypt('Pass@123', gen_salt('bf')), 30000, 20, 'regular'),
    ('Võ Quốc Huy', '0918901234', 'huy.vo@gmail.com', 'huy_vo', crypt('Pass@123', gen_salt('bf')), 90000, 180, 'silver'),
    ('Bùi Minh Quân', '0939012345', 'quan.bui@gmail.com', 'quan_bui', crypt('Pass@123', gen_salt('bf')), 300000, 2800, 'vip'),
    ('Đỗ Gia Hân', '0970123456', 'han.do@gmail.com', 'han_do', crypt('Pass@123', gen_salt('bf')), 60000, 80, 'regular'),
    ('Dương Tuấn Kiệt', '0981122334', 'kiet.duong@gmail.com', 'kiet_duong', crypt('Pass@123', gen_salt('bf')), 110000, 310, 'silver');

-- 9. LỊCH SỬ BẢO TRÌ MẪU (CHO DEMO RLS VÀ MODULE BẢO TRÌ)
INSERT INTO hr.maintenance (computer_id, technician_id, issue_description, parts_replaced, cost, status, started_at, finished_at)
VALUES 
    (1, 6, 'Thay quạt tản nhiệt CPU kêu to', '{"fan":"CoolerMaster 120mm"}'::jsonb, 150000, 'fixed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    (2, 1, 'Kiểm tra nguồn máy tính sập nguồn', '{"psu":"Corsair 650W"}'::jsonb, 500000, 'in_progress', NOW() - INTERVAL '3 hours', NULL);

-- 10. LÀM MỚI MATERIALIZED VIEWS
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
        v_old_json := to_jsonb(OLD);
        v_risk_score := v_risk_score + 30;
        v_reason := v_reason || '[Hành động xóa dữ liệu nhạy cảm] ';
    ELSIF (TG_OP = 'UPDATE') THEN
        v_old_json := to_jsonb(OLD);
        v_new_json := to_jsonb(NEW);
        
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
        v_new_json := to_jsonb(NEW);

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
        v_col := 'created_at / status / filter_columns';
        v_reason := format('Bảng %s có %s lượt quét tuần tự (seq_scan) trong khi chỉ có %s lượt quét chỉ mục.', 
                           v_tbl, r.seq_scan, COALESCE(r.idx_scan, 0));
        v_sql := format('CREATE INDEX idx_%s_auto_opt ON %s (created_at);', r.relname, v_tbl);

        -- Ghi vào bảng khuyến nghị nếu chưa tồn tại
        IF NOT EXISTS (SELECT 1 FROM ai.index_recommendation WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql) THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;

            recommendation_id := v_rec_id;
            table_name := v_tbl;
            column_list := v_col;
            reason := v_reason;
            suggested_sql := v_sql;
            RETURN NEXT;
        END IF;
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
        v_reason := format('Cột khóa ngoại %s.%s chưa có B-Tree index hỗ trợ truy vấn JOIN và chống lock.', v_tbl, v_col);
        v_sql := format('CREATE INDEX idx_%s_%s_fk ON %s (%s);', r.relname, v_col, v_tbl, v_col);

        IF NOT EXISTS (SELECT 1 FROM ai.index_recommendation WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql) THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;

            recommendation_id := v_rec_id;
            table_name := v_tbl;
            column_list := v_col;
            reason := v_reason;
            suggested_sql := v_sql;
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ai.fn_recommend_indexes IS 'Module AI 2: Công cụ tự động khuyến nghị chỉ mục từ thống kê catalog';

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
        -- Chèn hoặc cập nhật bảng ai.duplicate_candidate
        IF NOT EXISTS (
            SELECT 1 FROM ai.duplicate_candidate 
            WHERE ai.duplicate_candidate.customer_id_a = r.id_a AND ai.duplicate_candidate.customer_id_b = r.id_b
        ) THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (r.id_a, r.id_b, r.sim_score, r.match_reason, 'pending')
            RETURNING cand_id INTO v_cand_id;

            candidate_id := v_cand_id;
            customer_id_a := r.id_a;
            customer_name_a := r.name_a;
            customer_id_b := r.id_b;
            customer_name_b := r.name_b;
            similarity := r.sim_score;
            reason := r.match_reason;
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

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
                IF NOT EXISTS (
                    SELECT 1 FROM ai.anomaly_flag 
                    WHERE ai.anomaly_flag.subject_table = 'core.gaming_session' 
                      AND ai.anomaly_flag.subject_pk = r.session_id::text
                ) THEN
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

                    flag_id := v_flag_id;
                    anomaly_type := 'SESSION_DURATION_OUTLIER';
                    subject_table := 'core.gaming_session';
                    subject_pk := r.session_id::text;
                    metric_value := r.duration_mins;
                    z_score := v_z;
                    detail_info := jsonb_build_object('mean', ROUND(v_avg_dur, 2), 'stddev', ROUND(v_std_dur, 2), 'z_score', v_z);
                    RETURN NEXT;
                END IF;
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
                IF NOT EXISTS (
                    SELECT 1 FROM ai.anomaly_flag 
                    WHERE ai.anomaly_flag.subject_table = 'billing.payment' 
                      AND ai.anomaly_flag.subject_pk = r.payment_id::text
                ) THEN
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

                    flag_id := v_flag_id;
                    anomaly_type := 'PAYMENT_AMOUNT_OUTLIER';
                    subject_table := 'billing.payment';
                    subject_pk := r.payment_id::text;
                    metric_value := r.amount;
                    z_score := v_z;
                    detail_info := jsonb_build_object('mean', ROUND(v_avg_pay, 2), 'stddev', ROUND(v_std_pay, 2), 'z_score', v_z);
                    RETURN NEXT;
                END IF;
            END IF;
        END LOOP;
    END IF;
END;
$$ LANGUAGE plpgsql;

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
    -- Làm mới Materialized Views để có số liệu phân tích mới nhất
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;

    -- 1. Đề xuất Happy Hour cho các khung giờ thấp điểm (lượng phiên dưới mức trung bình)
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
        WHERE h.avg_sessions < COALESCE(o.global_avg * 0.7, 10)
        ORDER BY h.avg_sessions ASC
        LIMIT 3
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM ai.promotion_suggestion 
            WHERE ai.promotion_suggestion.suggestion_type = 'HAPPY_HOUR_OFFPEAK'
              AND ai.promotion_suggestion.target = format('Khung giờ %sh:00', r.hour_of_day)
        ) THEN
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

            suggestion_id := v_sug_id;
            suggestion_type := 'HAPPY_HOUR_OFFPEAK';
            target := format('Khung giờ %sh:00', r.hour_of_day);
            details := jsonb_build_object('hour', r.hour_of_day, 'sessions', r.session_vol);
            RETURN NEXT;
        END IF;
    END LOOP;

    -- 2. Đề xuất Win-Back Voucher cho khách hàng VIP có nguy cơ rời bỏ (At-Risk High-Value)
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
          AND (last_played_at IS NULL OR last_played_at < (NOW() - INTERVAL '2 days'))
        ORDER BY total_spent DESC
        LIMIT 5
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM ai.promotion_suggestion 
            WHERE ai.promotion_suggestion.suggestion_type = 'WIN_BACK_VIP'
              AND ai.promotion_suggestion.target = format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id)
        ) THEN
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

            suggestion_id := v_sug_id;
            suggestion_type := 'WIN_BACK_VIP';
            target := format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id);
            details := jsonb_build_object('customer_id', r.customer_id, 'spent', r.total_spent, 'days_inactive', r.days_inactive);
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ai.fn_suggest_promotions IS 'Module AI 5: Đề xuất ưu đãi thông minh từ phân tích RFM và khung giờ thấp điểm';
