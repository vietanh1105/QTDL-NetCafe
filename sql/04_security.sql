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
GRANT pg_read_all_stats TO role_admin, role_manager;

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
