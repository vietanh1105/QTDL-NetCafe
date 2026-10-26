-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- KỊCH BẢN DEMO 1: KHỞI TẠO CSDL - PYTHON ETL LÀM SẠCH - THỬ NGHIỆM RỦI RO LỖI ĐỎ
-- ============================================================================

-- ============================================================================
-- PHẦN 1: KHỞI TẠO TOÀN BỘ CSDL VÀ CHÈN DỮ LIỆU MỒI
-- (Sử dụng 1 file duy nhất: sql/netcafe_full.sql nạp qua Neon SQL Editor / psql)
-- ============================================================================

-- Kiểm tra 8 Schemas đã khởi tạo thành công:
SELECT table_schema, count(table_name) AS total_tables
FROM information_schema.tables
WHERE table_schema IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging')
GROUP BY table_schema
ORDER BY table_schema;

-- Kiểm tra 3 Extensions mở rộng (Bảo mật, Trigram AI, Tiếng Việt):
SELECT extname, extversion 
FROM pg_extension 
WHERE extname IN ('pgcrypto', 'pg_trgm', 'unaccent');


-- ============================================================================
-- PHẦN 2: DEMO PIPELINE PYTHON ETL (SINH DỮ LIỆU BẨN & CHUẨN HÓA DỮ LIỆU)
-- Bước 1 (Terminal): Chạy "python python/generate_dirty_data.py"
-- Bước 2 (Terminal): Chạy "python python/clean_data.py"
-- Bước 3 (SQL Editor): Chạy các câu query dưới đây để chứng minh với Giảng viên:
-- ============================================================================

-- 2.1. Xem dữ liệu bẩn ban đầu trong bảng Staging (chứa SĐT +84, số âm, khoảng trắng, ...):
SELECT raw_id, raw_full_name, raw_phone, raw_balance, raw_tier, source_batch
FROM staging.stg_customers
ORDER BY raw_id
LIMIT 10;

-- 2.2. Xem các dòng bẩn không thể cứu bị Python cách ly vào bảng Rejected:
SELECT rejected_id, source_table, error_reason, raw_record, rejected_at
FROM staging.rejected_rows
ORDER BY rejected_id DESC
LIMIT 10;

-- 2.3. Xem dữ liệu sạch đã được chuẩn hóa và nạp thành công vào bảng chính core.customer:
SELECT customer_id, full_name, phone, email, username, balance, membership_tier
FROM core.customer
ORDER BY customer_id DESC
LIMIT 10;


-- ============================================================================
-- PHẦN 3: THỬ NGHIỆM RỦI RO (BẮT BUỘC - XUẤT HIỆN THÔNG BÁO LỖI MÀU ĐỎ CỦA POSTGRESQL)
-- Lưu ý: Chạy từng câu lệnh TRẦN TRỤI dưới đây trên Neon Query Tool / DBeaver
-- để PostgreSQL trả về thông báo lỗi MÀU ĐỎ trực tiếp trên màn hình!
-- ============================================================================

-- ----------------------------------------------------------------------------
-- [TEST RỦI RO 1]: CỐ TÌNH INSERT SAI ĐỊNH DẠNG SỐ ĐIỆN THOẠI
-- Mong đợi: PostgreSQL Engine văng lỗi ĐỎ vi phạm check constraint "ck_customer_phone"
-- ----------------------------------------------------------------------------
INSERT INTO core.customer (full_name, phone, username, password_hash)
VALUES ('Trần Văn Sai SĐT', '12345', 'user_bad_phone_demo', '$2a$06$fakehashforrejectiontest00000000000000000000000000000');

/*
THÔNG BÁO LỖI ĐỎ POSTGRESQL TRẢ VỀ:
ERROR: new row for relation "customer" violates check constraint "ck_customer_phone"
DETAIL: Failing row contains (..., Trần Văn Sai SĐT, 12345, ..., user_bad_phone_demo, ...).
*/


-- ----------------------------------------------------------------------------
-- [TEST RỦI RO 2]: CỐ TÌNH CẬP NHẬT GIÁ TRỊ ÂM (TIỀN SỐ DƯ ÂM)
-- Mong đợi: PostgreSQL Engine văng lỗi ĐỎ vi phạm check constraint "ck_customer_balance"
-- ----------------------------------------------------------------------------
UPDATE core.customer
SET balance = -50000
WHERE customer_id = 1;

/*
THÔNG BÁO LỖI ĐỎ POSTGRESQL TRẢ VỀ:
ERROR: new row for relation "customer" violates check constraint "ck_customer_balance"
DETAIL: Failing row contains (1, ..., balance = -50000, ...).
*/


-- ----------------------------------------------------------------------------
-- [TEST RỦI RO 3]: CỐ TÌNH XÓA DỮ LIỆU CHA ĐANG BỊ RÀNG BUỘC FK RESTRICT
-- Phòng máy số 1 (room_id = 1) hiện đang có nhiều máy trạm (computer) trực thuộc.
-- Mong đợi: PostgreSQL Engine chặn đứng thao tác xóa và văng lỗi ĐỎ ON DELETE RESTRICT!
-- ----------------------------------------------------------------------------
DELETE FROM core.room
WHERE room_id = 1;

/*
THÔNG BÁO LỖI ĐỎ POSTGRESQL TRẢ VỀ:
ERROR: update or delete on table "room" violates foreign key constraint "fk_computer_room" on table "computer"
DETAIL: Key (room_id)=(1) is still referenced from table "computer".
*/
