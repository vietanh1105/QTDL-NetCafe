-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- PHẦN 3: DEMO TỐI ƯU HÓA HIỆU NĂNG (PERFORMANCE DEMO)
-- Hướng dẫn: Mở trên DBeaver / pgAdmin, chạy từng bước để so sánh trước và sau khi có Index
-- ============================================================================

-- Đảm bảo thoát khỏi vai trò nhân viên cấp thấp (tránh lỗi 42501 permission denied):
RESET ROLE;

-- ----------------------------------------------------------------------------
-- [BƯỚC 3.1]: CHẠY TRUY VẤN TÌM KIẾM PHỨC TẠP TRƯỚC KHI CÓ INDEX (SEQ SCAN)
-- Truy vấn tìm kiếm mờ ký tự đại diện ở đầu: '%Thành%'
-- ----------------------------------------------------------------------------
-- Tạm thời xóa index GIN Trigram (nếu có) để giả lập trạng thái bảng nguyên bản:
DROP INDEX IF EXISTS core.idx_customer_fullname_trgm;

-- Chạy EXPLAIN (ANALYZE, BUFFERS) trước khi có index:
EXPLAIN (ANALYZE, BUFFERS)
SELECT customer_id, full_name, phone, membership_tier 
FROM core.customer 
WHERE full_name ILIKE '%Thành%';

/*
KẾT QUẢ KHI CHƯA CÓ INDEX:
- Thuật toán quét: "Seq Scan on customer" (Quét tuần tự toàn bộ từng block dữ liệu)
- Buffers: Phải đọc nhiều shared hit blocks
- Thời gian thực thi: Cao
*/


-- ----------------------------------------------------------------------------
-- [BƯỚC 3.2]: TẠO CHỈ MỤC TỐI ƯU VÀ CHẠY LẠI TRUY VẤN (INDEX SCAN / BITMAP SCAN)
-- B-Tree thông thường bất lực với chuỗi bắt đầu bằng '%'.
-- Giải pháp tối ưu: Sử dụng GIN Index kết hợp toán tử gin_trgm_ops (pg_trgm)
-- ----------------------------------------------------------------------------
-- Thực thi tạo chỉ mục chuyên dụng:
CREATE INDEX idx_customer_fullname_trgm 
ON core.customer USING gin (full_name gin_trgm_ops);

-- Chạy lại câu truy vấn chính xác như trên để đối chiếu:
EXPLAIN (ANALYZE, BUFFERS)
SELECT customer_id, full_name, phone, membership_tier 
FROM core.customer 
WHERE full_name ILIKE '%Thành%';

/*
KẾT QUẢ SAU KHI CÓ INDEX:
- Thuật toán quét: "Bitmap Index Scan on idx_customer_fullname_trgm"
- Thuật toán đọc: "Bitmap Heap Scan on customer"
- Tốc độ: Tối ưu hóa vượt bậc, chỉ đọc đúng các con trỏ dòng khớp Trigram!
*/


-- ----------------------------------------------------------------------------
-- [BƯỚC 3.3]: THỬ NGHIỆM MODULE AI TÍCH HỢP TRONG PHẦN HIỆU NĂNG
-- (Module AI 2: In-Database AI tự động phân tích Catalog và khuyến nghị chỉ mục)
-- ----------------------------------------------------------------------------
-- Hệ thống tự động quét pg_stat_user_tables và tìm các khóa ngoại / bảng quét Seq Scan cao:
SELECT * FROM ai.fn_recommend_indexes();


-- ----------------------------------------------------------------------------
-- [BƯỚC 3.4 - MỞ RỘNG]: CHỈ MỤC CÓ ĐIỀU KIỆN (PARTIAL INDEX) CHO THỜI GIAN THỰC
-- Bảng gaming_session có hàng trăm ngàn dòng lịch sử, nhưng chỉ vài chục phiên đang chơi.
-- Partial Index (WHERE status = 'active') có kích thước siêu nhỏ, truy cập tức thì:
-- ----------------------------------------------------------------------------
EXPLAIN (ANALYZE, BUFFERS)
SELECT session_id, customer_id, computer_id, start_time 
FROM core.gaming_session 
WHERE status = 'active';
