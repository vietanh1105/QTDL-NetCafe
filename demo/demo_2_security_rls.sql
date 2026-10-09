-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
-- KỊCH BẢN DEMO 2: AN NINH PHÂN QUYỀN RBAC, COLUMN-LEVEL & ROW-LEVEL SECURITY (RLS)
-- Hướng dẫn: Trình diễn việc bảo vệ dữ liệu nhạy cảm và cách ly dữ liệu giữa các nhân viên
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- PHẦN 2.1: COLUMN-LEVEL SECURITY (BẢO VỆ CỘT LƯƠNG VÀ MẬT KHẨU)
-- ----------------------------------------------------------------------------
-- Khi thu ngân 'nv_thao' đăng nhập:
SET ROLE nv_thao;

-- Thu ngân xem được danh sách nhân viên công khai:
SELECT employee_id, full_name, username, emp_role, default_shift 
FROM hr.employee;

-- Cố tình truy vấn cột lương (salary) -> Bị CSDL chặn ngay lập tức (Lỗi 42501):
-- SELECT employee_id, full_name, salary FROM hr.employee;
DO $$
BEGIN
    PERFORM salary FROM hr.employee;
EXCEPTION WHEN insufficient_privilege THEN
    RAISE NOTICE '>>> BẢO VỆ CỘT THÀNH CÔNG: Thu ngân nv_thao bị chặn truy vấn cột salary (permission denied)!';
END $$;

RESET ROLE;

-- ----------------------------------------------------------------------------
-- PHẦN 2.2: ROW-LEVEL SECURITY (RLS) TRÊN HÓA ĐƠN VÀ SỔ QUỸ
-- ----------------------------------------------------------------------------

-- [GÓC NHÌN QUẢN LÝ / ADMIN (nv_manager)]:
SET ROLE nv_manager;
-- Quản lý thấy toàn bộ hóa đơn của tất cả thu ngân:
SELECT cashier_id, count(*) AS total_invoices 
FROM billing.invoice 
GROUP BY cashier_id;
RESET ROLE;

-- [GÓC NHÌN THU NGÂN 1: nv_thao (Employee ID = 3)]:
SET ROLE nv_thao;
-- RLS tự động lọc chỉ trả về những hóa đơn do chính nv_thao lập:
SELECT count(*) AS invoices_seen_by_thao, bool_and(cashier_id = 3) AS strictly_owned_by_thao 
FROM billing.invoice;
RESET ROLE;

-- [GÓC NHÌN THU NGÂN 2: nv_nam (Employee ID = 4)]:
SET ROLE nv_nam;
-- RLS tự động chuyển ngữ cảnh: chỉ thấy hóa đơn của nv_nam:
SELECT count(*) AS invoices_seen_by_nam, bool_and(cashier_id = 4) AS strictly_owned_by_nam 
FROM billing.invoice;
RESET ROLE;

-- ----------------------------------------------------------------------------
-- PHẦN 2.3: ROW-LEVEL SECURITY (RLS) CHO KỸ THUẬT VIÊN (hr.maintenance)
-- ----------------------------------------------------------------------------

-- [GÓC NHÌN KỸ THUẬT VIÊN: nv_hung (Employee ID = 6)]:
SET ROLE nv_hung;
-- Chỉ nhìn thấy đúng phiếu bảo trì được giao cho mình sửa chữa:
SELECT maintenance_id, computer_id, technician_id, issue_description, status 
FROM hr.maintenance;
RESET ROLE;

-- ----------------------------------------------------------------------------
-- PHẦN 2.4: ĐẶC QUYỀN TỐI THIỂU (LEAST PRIVILEGE) GIỮA CÁC SCHEMA
-- ----------------------------------------------------------------------------
-- Nhân viên phục vụ 'nv_tuan' chỉ phụ trách F&B, không được phép sờ vào schema billing:
SET ROLE nv_tuan;
DO $$
BEGIN
    PERFORM count(*) FROM billing.invoice;
EXCEPTION WHEN insufficient_privilege THEN
    RAISE NOTICE '>>> NGUYÊN TẮC ĐẶC QUYỀN TỐI THIỂU HOẠT ĐỘNG: nv_tuan bị chặn hoàn toàn khỏi schema billing!';
END $$;
RESET ROLE;
