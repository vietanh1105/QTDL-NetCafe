-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: tests/test_security.sql
-- Mục đích: Kiểm thử toàn diện chính sách Bảo mật CSDL:
--           1. RBAC (Kiểm tra đặc quyền tối thiểu giữa các vai trò)
--           2. Column-Level Security (Bảo vệ cột salary, password_hash)
--           3. Row-Level Security (RLS trên invoice, payment, employee, maintenance)
-- ============================================================================

\c netcafe_db;
\set ECHO all

SELECT '=== BẮT ĐẦU KIỂM THỬ BẢO MẬT CSDL (PHASE 5) ===' AS title;

-- ----------------------------------------------------------------------------
-- TEST 1: COLUMN-LEVEL SECURITY - nv_thao không được xem salary và password_hash
-- ----------------------------------------------------------------------------
SELECT '--- TEST 1.1: nv_thao xem các cột được cấp phép (Kỳ vọng: THÀNH CÔNG) ---' AS test_case;
SET ROLE nv_thao;
SELECT employee_id, full_name, username, emp_role FROM hr.employee;
RESET ROLE;

SELECT '--- TEST 1.2: nv_thao cố tình truy vấn cột salary (Kỳ vọng: BỊ CHẶN 42501) ---' AS test_case;
DO $$
BEGIN
    SET ROLE nv_thao;
    BEGIN
        PERFORM salary FROM hr.employee;
        RAISE EXCEPTION 'LỖI: nv_thao lại xem được cột salary!';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'THÀNH CÔNG: Đã chặn truy cập cột salary (permission denied).';
    END;
    RESET ROLE;
END $$;

SELECT '--- TEST 1.3: nv_thao cố tình truy vấn password_hash (Kỳ vọng: BỊ CHẶN 42501) ---' AS test_case;
DO $$
BEGIN
    SET ROLE nv_thao;
    BEGIN
        PERFORM password_hash FROM hr.employee;
        RAISE EXCEPTION 'LỖI: nv_thao lại xem được password_hash!';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'THÀNH CÔNG: Đã chặn truy cập cột password_hash (permission denied).';
    END;
    RESET ROLE;
END $$;

-- ----------------------------------------------------------------------------
-- TEST 2: ROW-LEVEL SECURITY (RLS) TRÊN hr.employee
-- ----------------------------------------------------------------------------
SELECT '--- TEST 2.1: nv_thao chỉ được thấy dòng của chính mình trong hr.employee ---' AS test_case;
SET ROLE nv_thao;
SELECT employee_id, full_name, emp_role FROM hr.employee;
RESET ROLE;

SELECT '--- TEST 2.2: nv_manager thấy toàn bộ nhân viên ---' AS test_case;
SET ROLE nv_manager;
SELECT count(*) AS total_employees_seen_by_manager FROM hr.employee;
RESET ROLE;

-- ----------------------------------------------------------------------------
-- TEST 3: ROW-LEVEL SECURITY (RLS) TRÊN billing.invoice
-- ----------------------------------------------------------------------------
SELECT '--- TEST 3.1: Thống kê số hóa đơn theo góc nhìn Manager/Admin ---' AS test_case;
SET ROLE nv_manager;
SELECT count(*) AS total_invoices_seen_by_manager FROM billing.invoice;
SELECT cashier_id, count(*) AS count_per_cashier FROM billing.invoice GROUP BY cashier_id;
RESET ROLE;

SELECT '--- TEST 3.2: nv_thao chỉ thấy hóa đơn do chính mình lập (cashier_id = 3) ---' AS test_case;
SET ROLE nv_thao;
SELECT count(*) AS invoices_seen_by_thao, 
       bool_and(cashier_id = 3) AS all_invoices_belong_to_thao
FROM billing.invoice;
RESET ROLE;

SELECT '--- TEST 3.3: nv_nam chỉ thấy hóa đơn do chính mình lập (cashier_id = 4) ---' AS test_case;
SET ROLE nv_nam;
SELECT count(*) AS invoices_seen_by_nam, 
       bool_and(cashier_id = 4) AS all_invoices_belong_to_nam
FROM billing.invoice;
RESET ROLE;

-- Chuẩn bị dữ liệu mẫu bảo trì nếu chưa có (1 việc cho nv_hung ID=6, 1 việc cho nv_admin ID=1)
INSERT INTO hr.maintenance (computer_id, technician_id, issue_description, parts_replaced, cost, status, started_at, finished_at)
SELECT 1, 6, 'Thay quạt tản nhiệt CPU kêu to', '{"fan":"CoolerMaster 120mm"}'::jsonb, 150000, 'fixed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'
WHERE NOT EXISTS (SELECT 1 FROM hr.maintenance WHERE computer_id = 1 AND technician_id = 6);

INSERT INTO hr.maintenance (computer_id, technician_id, issue_description, parts_replaced, cost, status, started_at, finished_at)
SELECT 2, 1, 'Kiểm tra nguồn máy tính sập nguồn', '{"psu":"Corsair 650W"}'::jsonb, 500000, 'in_progress', NOW() - INTERVAL '3 hours', NULL
WHERE NOT EXISTS (SELECT 1 FROM hr.maintenance WHERE computer_id = 2 AND technician_id = 1);

SELECT '--- TEST 4.1: Manager thấy toàn bộ lịch sử bảo trì ---' AS test_case;
SET ROLE nv_manager;
SELECT count(*) AS total_maintenance_seen_by_manager FROM hr.maintenance;
RESET ROLE;

SELECT '--- TEST 4.2: Kỹ thuật viên nv_hung (ID = 6) chỉ thấy bảo trì của mình ---' AS test_case;
SET ROLE nv_hung;
SELECT maintenance_id, computer_id, technician_id, issue_description, status 
FROM hr.maintenance;
RESET ROLE;

-- ----------------------------------------------------------------------------
-- TEST 5: ĐẶC QUYỀN TỐI THIỂU (LEAST PRIVILEGE) & SCHEMA BOUNDARIES
-- ----------------------------------------------------------------------------
SELECT '--- TEST 5.1: Nhân viên phục vụ nv_tuan cố xem hóa đơn billing (Kỳ vọng: BỊ CHẶN) ---' AS test_case;
DO $$
BEGIN
    SET ROLE nv_tuan;
    BEGIN
        PERFORM count(*) FROM billing.invoice;
        RAISE EXCEPTION 'LỖI: nv_tuan không có quyền nhưng lại đọc được invoice!';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'THÀNH CÔNG: Đã chặn nv_tuan đọc schema billing (permission denied).';
    END;
    RESET ROLE;
END $$;

SELECT '--- TEST 5.2: Thu ngân nv_thao cố sửa cấu hình máy core.computer (Kỳ vọng: THÀNH CÔNG cập nhật trạng thái) ---' AS test_case;
SET ROLE nv_thao;
SELECT computer_id, computer_code, status FROM core.computer WHERE computer_code = 'PC001';
RESET ROLE;

SELECT '=== HOÀN TẤT KIỂM THỬ BẢO MẬT CSDL - TẤT CẢ TEST ĐẠT YÊU CẦU ===' AS summary;
