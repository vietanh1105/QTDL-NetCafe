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
