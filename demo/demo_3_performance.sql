-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- PHẦN 3: DEMO TỐI ƯU HÓA HIỆU NĂNG (PERFORMANCE DEMO)
-- Bài toán: Tìm kiếm hóa đơn có số tiền lớn (total_amount = 630.000 VNĐ)
-- Hướng dẫn: Mở trên Neon SQL Editor / DBeaver, chạy từng bước để so sánh
-- ============================================================================

-- Đảm bảo thoát khỏi vai trò nhân viên cấp thấp, về lại vai trò Admin/DBA:
RESET ROLE;

-- ----------------------------------------------------------------------------
-- [BƯỚC 3.1]: CHẠY TRUY VẤN TÌM KIẾM TRƯỚC KHI CÓ INDEX (SEQ SCAN)
-- Truy vấn tìm hóa đơn có giá trị lớn: total_amount = 630.000 VNĐ
-- ----------------------------------------------------------------------------
-- Tạm thời xóa index (nếu có) để đưa bảng về trạng thái nguyên bản chưa tối ưu:
DROP INDEX IF EXISTS billing.idx_invoice_total_amount;

-- Chạy EXPLAIN (ANALYZE, BUFFERS) trước khi có index:
EXPLAIN (ANALYZE, BUFFERS)
SELECT invoice_id, customer_id, cashier_id, total_amount, status 
FROM billing.invoice 
WHERE total_amount = 630000;

/*
KẾT QUẢ KHI CHƯA CÓ INDEX:
- Thuật toán quét: "Seq Scan on invoice" (Quét tuần tự toàn bộ từng hóa đơn)
- Đời thường: Giống như phải lật giở từng tờ hóa đơn từ đầu đến cuối xấp để tìm
*/


-- ----------------------------------------------------------------------------
-- [BƯỚC 3.2]: TẠO CHỈ MỤC B-TREE VÀ CHẠY LẠI TRUY VẤN (INDEX SCAN)
-- B-Tree Index tự động sắp xếp số tiền theo thứ tự để tra cứu trúng đích O(logN)
-- ----------------------------------------------------------------------------
-- Thực thi tạo chỉ mục B-Tree trên cột total_amount:
CREATE INDEX idx_invoice_total_amount ON billing.invoice (total_amount);

-- Bật ép dùng Index và chạy lại câu truy vấn:
SET enable_seqscan = off;

EXPLAIN (ANALYZE, BUFFERS)
SELECT invoice_id, customer_id, cashier_id, total_amount, status 
FROM billing.invoice 
WHERE total_amount = 630000;

SET enable_seqscan = on;

/*
KẾT QUẢ SAU KHI CÓ INDEX:
- Thuật toán quét: "Index Scan using idx_invoice_total_amount on invoice"
- Tốc độ: Nhảy thẳng đến địa chỉ dòng hóa đơn cần tìm, bỏ qua hoàn toàn các hóa đơn khác!
*/


-- ----------------------------------------------------------------------------
-- [BƯỚC 3.3]: THỬ NGHIỆM MODULE AI TÍCH HỢP TRONG PHẦN HIỆU NĂNG
-- (Module AI 2: In-Database AI tự động phân tích Catalog và khuyến nghị chỉ mục)
-- ----------------------------------------------------------------------------
-- Hệ thống tự động quét pg_stat_user_tables và tìm các khóa ngoại / bảng quét Seq Scan cao:
SELECT recommendation_id, table_name, column_list, reason, suggested_sql 
FROM ai.fn_recommend_indexes() 
LIMIT 3;
