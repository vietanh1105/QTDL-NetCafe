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

    -- User đăng nhập demo: Mô hình 4 vai trò chính (nv_admin, nv_manager, nv_thao, nv_hung)
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_admin') THEN
        CREATE ROLE nv_admin WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_admin;
    ELSE
        ALTER ROLE nv_admin WITH PASSWORD 'Demo@2026NetCafe';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'nv_manager') THEN
        CREATE ROLE nv_manager WITH LOGIN PASSWORD 'Demo@2026NetCafe' IN ROLE role_manager;
    ELSE
        ALTER ROLE nv_manager WITH LOGIN PASSWORD 'Demo@2026NetCafe';
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
GRANT SELECT, INSERT, UPDATE ON core.computer, core.customer, core.gaming_session TO role_cashier;
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
GRANT MAINTAIN ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai TO role_admin;
GRANT pg_read_all_stats TO role_admin, role_manager;

-- Manager được quản lý dữ liệu vận hành nhưng không được đọc/sửa salary hoặc password_hash.
REVOKE ALL ON hr.employee FROM role_manager;
GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_manager;
GRANT INSERT (full_name, phone, username, db_username, password_hash, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_manager;
GRANT UPDATE (full_name, phone, db_username, emp_role, default_shift, hired_date, is_active)
    ON hr.employee TO role_manager;

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
GRANT SELECT ON hr.vw_employee_public TO role_manager;

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
