-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
-- KỊCH BẢN DEMO 3: TỐI ƯU HÓA HIỆU NĂNG, CHỈ MỤC VÀ TỰ ĐỘNG PHÂN VÙNG (PARTITION)
-- Hướng dẫn: Trình diễn EXPLAIN ANALYZE, GIN Trigram, GIN JSONB, và Partition Automation
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- PHẦN 3.1: CHỈ MỤC ĐẶC BIỆT GIN TRIGRAM (TÌM KIẾM MỜ KÝ TỰ ĐẦU %KEYWORD%)
-- ----------------------------------------------------------------------------
-- B-Tree thông thường bắt buộc quét toàn bộ bảng (Seq Scan) khi có ký tự đại diện ở đầu.
-- Với pg_trgm + GIN, truy vấn sử dụng Bitmap Index Scan:
EXPLAIN (ANALYZE, BUFFERS)
SELECT customer_id, full_name, phone 
FROM core.customer 
WHERE full_name ILIKE '%Thành%';


-- ----------------------------------------------------------------------------
-- PHẦN 3.2: CHỈ MỤC GIN JSONB_PATH_OPS (TÌM KIẾM PHẦN CỨNG MÁY TRẠM)
-- ----------------------------------------------------------------------------
-- Tìm các máy có GPU RTX 4070 bên trong trường specs JSONB:
EXPLAIN (ANALYZE, BUFFERS)
SELECT computer_code, computer_name, specs 
FROM core.computer 
WHERE specs @> '{"gpu": "RTX 4070"}'::jsonb;


-- ----------------------------------------------------------------------------
-- PHẦN 3.3: PARTIAL INDEX (CHỈ MỤC CÓ ĐIỀU KIỆN CHO DỮ LIỆU THỜI GIAN THỰC)
-- ----------------------------------------------------------------------------
-- Bảng gaming_session có hàng trăm ngàn phiên lịch sử, nhưng chỉ vài chục phiên đang active.
-- Partial Index (WHERE status = 'active') có kích thước siêu nhỏ, truy vấn cực nhanh:
EXPLAIN (ANALYZE, BUFFERS)
SELECT session_id, customer_id, computer_id, start_time 
FROM core.gaming_session 
WHERE status = 'active';


-- ----------------------------------------------------------------------------
-- PHẦN 3.4: TỰ ĐỘNG HÓA VẬN HÀNH PHÂN VÙNG BẢNG AUDIT_LOG THEO THÁNG
-- ----------------------------------------------------------------------------
-- Giảng viên thấy quy trình DBA tự động hóa: Hàm tự động quét và tạo các partition tháng tương lai:
SELECT * FROM audit.fn_create_monthly_partitions(3);

-- Kiểm tra danh sách các phân vùng vật lý đang hoạt động của audit_log:
SELECT 
    nmsp_parent.nspname AS parent_schema,
    parent.relname      AS parent_table,
    child.relname       AS partition_name
FROM pg_inherits
JOIN pg_class parent        ON pg_inherits.inhparent = parent.oid
JOIN pg_class child         ON pg_inherits.inhrelid   = child.oid
JOIN pg_namespace nmsp_parent ON nmsp_parent.oid = parent.relnamespace
WHERE parent.relname = 'audit_log'
ORDER BY child.relname;
