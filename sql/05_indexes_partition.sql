-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 05_indexes_partition.sql
-- Mục đích: Tối ưu hóa hiệu năng CSDL toàn diện:
--           1. B-Tree Indexes cho khóa ngoại và cột lọc thường xuyên
--           2. Partial Indexes cho dữ liệu biến động cao (phiên active, máy available)
--           3. GIN Trigram & JSONB Indexes (tìm kiếm mờ, lọc cấu hình phần cứng)
--           4. BRIN Index cho bảng nhật ký thời gian lớn (audit_log)
--           5. Thiết lập Fillfactor tối ưu Heap-Only Tuples (HOT) & Autovacuum
--           6. Thủ tục tự động tạo Partition bảng nhật ký kiểm toán theo tháng
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- 1. B-TREE INDEXES CHO KHÓA NGOẠI VÀ TRUY VẤN TẦN SUẤT CAO
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Mặc định PostgreSQL không tự tạo index trên cột Foreign Key. 
-- Index trên FK chống Table Scan khi JOIN và tránh Row Exclusive Lock lan tỏa khi DELETE/UPDATE bảng cha.

CREATE INDEX IF NOT EXISTS idx_session_customer_id 
    ON core.gaming_session (customer_id);

CREATE INDEX IF NOT EXISTS idx_session_computer_id 
    ON core.gaming_session (computer_id);

CREATE INDEX IF NOT EXISTS idx_session_start_time 
    ON core.gaming_session (start_time DESC);

CREATE INDEX IF NOT EXISTS idx_order_session_id 
    ON inventory.food_order (session_id);

CREATE INDEX IF NOT EXISTS idx_invoice_customer_id 
    ON billing.invoice (customer_id);

CREATE INDEX IF NOT EXISTS idx_invoice_cashier_id 
    ON billing.invoice (cashier_id);

CREATE INDEX IF NOT EXISTS idx_invoice_issued_at 
    ON billing.invoice (issued_at DESC);

CREATE INDEX IF NOT EXISTS idx_payment_customer_id 
    ON billing.payment (customer_id);

CREATE INDEX IF NOT EXISTS idx_payment_invoice_id 
    ON billing.payment (invoice_id) WHERE invoice_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_maint_computer_id 
    ON hr.maintenance (computer_id);

CREATE INDEX IF NOT EXISTS idx_maint_technician_id 
    ON hr.maintenance (technician_id);

-- ----------------------------------------------------------------------------
-- 2. PARTIAL INDEXES (INDEX CÓ ĐIỀU KIỆN)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Chỉ đánh chỉ mục trên tập con dòng thỏa mãn WHERE.
-- Dung lượng index cực nhỏ, cache hoàn toàn trong RAM, tối ưu vượt trội cho các truy vấn thời gian thực.

-- 2.1. Tra cứu các phiên chơi đang hoạt động (chỉ vài chục dòng trong hàng triệu dòng lịch sử)
CREATE INDEX IF NOT EXISTS idx_session_active 
    ON core.gaming_session (customer_id, computer_id) 
    WHERE status = 'active';

-- 2.2. Tra cứu máy tính còn trống theo phòng
CREATE INDEX IF NOT EXISTS idx_computer_available 
    ON core.computer (room_id, type_id) 
    WHERE status = 'available';

-- 2.3. Tra cứu hóa đơn chưa thanh toán
CREATE INDEX IF NOT EXISTS idx_invoice_unpaid 
    ON billing.invoice (invoice_id, customer_id) 
    WHERE status = 'unpaid';

-- ----------------------------------------------------------------------------
-- 3. GIN INDEXES (GENERALIZED INVERTED INDEX)
-- ----------------------------------------------------------------------------
-- 3.1. GIN Trigram cho tìm kiếm mờ họ tên khách hàng không dấu và có dấu (ILIKE / LIKE '%...%')
-- Giải thích DBA: B-Tree không dùng được khi tìm kiếm có ký tự đại diện ở đầu (%abc%).
-- GIN pg_trgm tách chuỗi thành các cụm 3 ký tự, cho phép tăng tốc tìm kiếm chuỗi con tức thì.
CREATE INDEX IF NOT EXISTS idx_customer_fullname_trgm 
    ON core.customer USING gin (full_name gin_trgm_ops);

-- 3.2. GIN jsonb_path_ops cho cấu hình phần cứng máy trạm (core.computer.specs)
-- Giải thích DBA: jsonb_path_ops băm cả path+value thành 1 hash integer, nhỏ gọn hơn GIN mặc định và tăng tốc toán tử @>.
CREATE INDEX IF NOT EXISTS idx_computer_specs_gin 
    ON core.computer USING gin (specs jsonb_path_ops);

-- 3.3. GIN jsonb_path_ops cho thuộc tính sản phẩm F&B
CREATE INDEX IF NOT EXISTS idx_product_attr_gin 
    ON inventory.product USING gin (attributes jsonb_path_ops);

-- ----------------------------------------------------------------------------
-- 4. BRIN INDEX (BLOCK RANGE INDEX) CHO BẢNG NHẬT KÝ THỜI GIAN LỚN
-- ----------------------------------------------------------------------------
-- Giải thích DBA: BRIN lưu tóm tắt min/max của từng cụm trang đĩa vật lý (block range).
-- Chiếm dưới 1% dung lượng so với B-Tree, hoàn hảo cho dữ liệu audit_log ghi theo thứ tự thời gian tăng dần liên tục.
CREATE INDEX IF NOT EXISTS idx_audit_log_logged_at_brin 
    ON audit.audit_log USING brin (logged_at);

-- ----------------------------------------------------------------------------
-- 5. TỐI ƯU HÓA HEAP-ONLY TUPLES (HOT) & AUTOVACUUM
-- ----------------------------------------------------------------------------
-- Giải thích DBA: 
-- - Fillfactor < 100 chừa khoảng trống (free space) trên mỗi page dữ liệu để khi UPDATE, 
--   tuple mới được ghi ngay trên cùng page (Heap-Only Tuple). 
--   Khi đó không cần cập nhật lại các B-Tree index, giảm triệt để I/O write và chống index bloat.
-- - autovacuum_vacuum_scale_factor thấp hơn giúp dọn dẹp tuple chết sớm hơn trên các bảng cập nhật nhiều.

ALTER TABLE core.gaming_session SET (
    fillfactor = 90,
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_vacuum_cost_limit = 500
);

ALTER TABLE core.computer SET (
    fillfactor = 85,
    autovacuum_vacuum_scale_factor = 0.05
);

ALTER TABLE inventory.product SET (
    fillfactor = 90
);

-- ----------------------------------------------------------------------------
-- 6. THỦ TỤC TỰ ĐỘNG TẠO PARTITION THEO THÁNG CHO AUDIT_LOG
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Tự động hóa vận hành định kỳ để DBA không phải tạo phân vùng thủ công mỗi tháng.
CREATE OR REPLACE FUNCTION audit.fn_create_monthly_partitions(p_months_ahead INT DEFAULT 3)
RETURNS TABLE(partition_name TEXT, start_date DATE, end_date DATE, action_taken TEXT) AS $$
DECLARE
    v_target_date DATE;
    v_next_date DATE;
    v_part_name TEXT;
    v_sql TEXT;
    i INT;
BEGIN
    FOR i IN 0..p_months_ahead LOOP
        v_target_date := date_trunc('month', CURRENT_DATE + (i || ' month')::interval)::DATE;
        v_next_date   := date_trunc('month', v_target_date + '1 month'::interval)::DATE;
        v_part_name   := 'audit_log_' || to_char(v_target_date, 'YYYY_MM');

        IF NOT EXISTS (
            SELECT 1 FROM pg_class c 
            JOIN pg_namespace n ON n.oid = c.relnamespace 
            WHERE n.nspname = 'audit' AND c.relname = v_part_name
        ) THEN
            v_sql := format(
                'CREATE TABLE audit.%I PARTITION OF audit.audit_log FOR VALUES FROM (%L) TO (%L);',
                v_part_name,
                to_char(v_target_date, 'YYYY-MM-01 00:00:00+00'),
                to_char(v_next_date, 'YYYY-MM-01 00:00:00+00')
            );
            EXECUTE v_sql;
            
            partition_name := v_part_name;
            start_date := v_target_date;
            end_date := v_next_date;
            action_taken := 'CREATED';
            RETURN NEXT;
        ELSE
            partition_name := v_part_name;
            start_date := v_target_date;
            end_date := v_next_date;
            action_taken := 'ALREADY_EXISTS';
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION audit.fn_create_monthly_partitions IS 'Tự động tạo partition audit_log cho N tháng tiếp theo';
