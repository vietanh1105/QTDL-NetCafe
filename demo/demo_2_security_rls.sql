-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- PHẦN 2: DEMO BẢO MẬT VÀ RLS (ROLE-PLAYING DEMO)
-- Hướng dẫn: Đăng nhập bằng tài khoản Quản trị, bôi đen từng lệnh để chạy trên DBeaver / pgAdmin
-- ============================================================================

-- ----------------------------------------------------------------------------
-- [BƯỚC 2.1]: ĐÓNG VAI NHÂN VIÊN THU NGÂN CẤP THẤP BẰNG LỆNH SET ROLE
-- ----------------------------------------------------------------------------
-- Đóng vai nhân viên thu ngân Thảo (employee_id = 3):
SET ROLE nv_thao;

-- Kiểm tra vai trò hiện tại đang đóng:
SELECT current_user, session_user;


-- ----------------------------------------------------------------------------
-- [BƯỚC 2.2]: CHỨNG MINH TÍNH NĂNG ROW-LEVEL SECURITY (RLS) HOẠT ĐỘNG
-- Nhân viên chỉ nhìn thấy dòng hóa đơn của chính mình lập (cashier_id = 3)
-- ----------------------------------------------------------------------------
-- Thu ngân Thảo thực hiện SELECT * trên bảng chính billing.invoice:
SELECT invoice_id, customer_id, cashier_id, total_amount, status, created_at
FROM billing.invoice;

-- Chứng minh toàn bộ các dòng nhìn thấy ĐỀU CÓ cashier_id = 3:
SELECT count(*) AS total_invoices_seen, bool_and(cashier_id = 3) AS strictly_owned_by_thao 
FROM billing.invoice;

-- Đổi vai sang Quản lý (Manager) để đối chiếu: Quản lý nhìn thấy TOÀN BỘ hóa đơn:
RESET ROLE;
SET ROLE nv_manager;
SELECT cashier_id, count(*) AS total_invoices_by_cashier
FROM billing.invoice
GROUP BY cashier_id;
RESET ROLE;


-- ----------------------------------------------------------------------------
-- [BƯỚC 2.3]: CHỨNG MINH COLUMN-LEVEL PRIVILEGES (BẢO VỆ CỘT NHẠY CẢM)
-- Thu ngân chỉ được xem thông tin công khai, CỘT LƯƠNG (salary) BỊ CHẶN TUYỆT ĐỐI!
-- ----------------------------------------------------------------------------
SET ROLE nv_thao;

-- Thu ngân xem thông tin nhân sự công khai (HỢP LỆ):
SELECT employee_id, full_name, username, emp_role, default_shift 
FROM hr.employee;

-- CỐ TÌNH TRUY VẬN CỘT NHẠY CẢM "salary" (LƯƠNG NHÂN VIÊN):
-- Mong đợi: PostgreSQL Engine chặn đứng và văng lỗi ĐỎ:
SELECT employee_id, full_name, salary 
FROM hr.employee;

/*
THÔNG BÁO LỖI ĐỎ POSTGRESQL TRẢ VỀ:
ERROR: permission denied for column "salary" of relation "employee"
Code: 42501 (insufficient_privilege)
*/

RESET ROLE;


-- ----------------------------------------------------------------------------
-- [BƯỚC 2.4]: THỬ NGHIỆM MODULE AI TÍCH HỢP TRONG PHẦN BẢO MẬT
-- (Module AI 3: Thuật toán so khớp chuỗi mờ pg_trgm + unaccent phát hiện tài khoản trùng lặp)
-- ----------------------------------------------------------------------------
-- Hệ thống tự động so sánh độ tương đồng họ tên & SĐT để chống gian lận tạo nhiều nick nhận khuyến mãi:
SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason 
FROM ai.fn_detect_duplicates(0.65)
LIMIT 5;
