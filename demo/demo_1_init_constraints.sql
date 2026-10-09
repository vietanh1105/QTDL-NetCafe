-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
-- KỊCH BẢN DEMO 1: KHỞI TẠO CSDL VÀ CHỨNG MINH RÀNG BUỘC TOÀN VẸN (CONSTRAINTS)
-- Hướng dẫn: Mở trên pgAdmin / DBeaver, bôi đen từng khối lệnh và bấm F5 để trình chiếu
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- PHẦN 1.1: TỔNG QUAN KIẾN TRÚC SCHEMAS VÀ BẢNG
-- ----------------------------------------------------------------------------
-- Giảng viên thấy hệ thống phân rã thành 8 schemas nghiệp vụ độc lập:
SELECT table_schema, count(table_name) AS total_tables
FROM information_schema.tables
WHERE table_schema IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging')
GROUP BY table_schema
ORDER BY table_schema;

-- Kiểm tra 4 extensions mở rộng đã kích hoạt:
SELECT extname, extversion 
FROM pg_extension 
WHERE extname IN ('pgcrypto', 'btree_gist', 'pg_trgm', 'unaccent');


-- ----------------------------------------------------------------------------
-- PHẦN 1.2: CHỨNG MINH RÀNG BUỘC TOÀN VẸN VÀ BẢO ĐẢM CHẤT LƯỢNG DỮ LIỆU
-- ----------------------------------------------------------------------------

-- [DEMO C1]: CHỐNG ĐẶT TRÙNG GIỜ MÁY TÍNH (EXCLUDE USING GIST VỚI BTREE_GIST)
-- Tình huống: Máy PC001 đã có người đặt từ 14:00 đến 16:00.
-- Hệ thống tự động chặn khách đặt đè vào lúc 15:00 - 17:00 ngay tại tầng CSDL.
SELECT booking_id, computer_id, booked_from, booked_to, status 
FROM core.booking 
WHERE computer_id = 1;

-- Thử đặt đè vào khung giờ trên -> CSDL ném lỗi vi phạm exclusion constraint:
DO $$
BEGIN
    INSERT INTO core.booking (customer_id, computer_id, booked_from, booked_to, status)
    VALUES (2, 1, '2026-10-10 15:00:00+07', '2026-10-10 17:00:00+07', 'confirmed');
    RAISE EXCEPTION 'LỖI: Ràng buộc không chặn được đặt trùng!';
EXCEPTION WHEN exclusion_violation THEN
    RAISE NOTICE '>>> THÀNH CÔNG: PostgreSQL đã kích hoạt EXCLUDE USING GIST ngăn chặn đặt trùng máy thành công!';
END $$;


-- [DEMO C2]: RÀNG BUỘC CHECK SỐ TIỀN KHÔNG ÂM (CK_INVOICE_TOTAL_AMOUNT)
-- Thử cố tình tạo hóa đơn tổng tiền âm:
DO $$
BEGIN
    INSERT INTO billing.invoice (
        session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount, status
    ) VALUES (99999, 1, 3, 20000, 10000, 0, 50000, -20000, 'unpaid');
    RAISE EXCEPTION 'LỖI: Chấp nhận tiền âm!';
EXCEPTION WHEN check_violation THEN
    RAISE NOTICE '>>> THÀNH CÔNG: CSDL chặn hóa đơn có số tiền âm (check_violation)!';
END $$;


-- [DEMO C3]: RÀNG BUỘC BẤT BIẾN CÔNG THỨC HÓA ĐƠN (CK_INVOICE_FORMULA)
-- total_amount PHẢI = play_amount + service_amount + surcharge - discount_amount
DO $$
BEGIN
    INSERT INTO billing.invoice (
        session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount, status
    ) VALUES (99999, 1, 3, 20000, 10000, 0, 5000, 50000, 'unpaid'); -- 20k + 10k - 5k = 25k, nhưng nhập 50k
    RAISE EXCEPTION 'LỖI: Chấp nhận công thức sai lệch!';
EXCEPTION WHEN check_violation THEN
    RAISE NOTICE '>>> THÀNH CÔNG: CSDL chặn hóa đơn sai lệch công thức toán học bất biến!';
END $$;


-- [DEMO C4]: RÀNG BUỘC CHECK THỜI LƯỢNG PHIÊN CHƠI (CK_SESSION_TIME_ORDER)
-- Giờ kết thúc phiên end_time phải lớn hơn giờ bắt đầu start_time:
DO $$
BEGIN
    INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status)
    VALUES (1, 1, 3, NOW(), NOW() - INTERVAL '1 hour', 'finished');
    RAISE EXCEPTION 'LỖI: Cho phép giờ kết thúc trước giờ bắt đầu!';
EXCEPTION WHEN check_violation THEN
    RAISE NOTICE '>>> THÀNH CÔNG: CSDL chặn phiên chơi có end_time <= start_time!';
END $$;
