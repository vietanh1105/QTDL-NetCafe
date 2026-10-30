-- So sánh Seq Scan / Index Scan bằng dữ liệu giả lập có kích thước xác định.
-- Chạy trên Neon bằng DBeaver/pgAdmin, trong MỘT kết nối.
-- Bảng tạm chỉ tồn tại trong phiên; ROLLBACK không thay đổi bảng nghiệp vụ.
-- Đây là bài minh họa cơ chế planner, không phải benchmark của dữ liệu phòng máy thật.

BEGIN;
SET LOCAL TIME ZONE 'Asia/Ho_Chi_Minh';
SET LOCAL statement_timeout = '30s';

CREATE TEMP TABLE baove_invoice_perf ON COMMIT DROP AS
SELECT g.value::bigint AS invoice_id,
       c.ids[1 + ((g.value - 1) % cardinality(c.ids))] AS customer_id,
       (10000 + g.value * 7)::numeric(12,0) AS total_amount,
       timestamptz '2026-10-01 00:00:00+07' + g.value * interval '1 minute' AS issued_at,
       repeat('du_lieu_demo_', 8) AS note
FROM generate_series(1, 20000) AS g(value)
CROSS JOIN (
    SELECT array_agg(customer_id ORDER BY customer_id) AS ids
    FROM core.customer
) AS c
WHERE cardinality(c.ids) > 0;

ANALYZE baove_invoice_perf;
SELECT count(*) AS synthetic_rows FROM baove_invoice_perf;

-- Trước khi có chỉ mục: quan sát Seq Scan trên bảng tạm.
EXPLAIN (ANALYZE, BUFFERS)
SELECT d.invoice_id, c.full_name, d.total_amount, d.issued_at
FROM baove_invoice_perf d
JOIN core.customer c ON c.customer_id = d.customer_id
WHERE d.invoice_id = 12345
  AND d.issued_at >= timestamptz '2026-10-01 00:00:00+07'
  AND d.issued_at < timestamptz '2026-10-31 00:00:00+07';

CREATE INDEX baove_invoice_perf_lookup ON baove_invoice_perf (invoice_id);
ANALYZE baove_invoice_perf;

-- Sau khi có chỉ mục: planner tự lựa chọn, không tắt enable_seqscan.
EXPLAIN (ANALYZE, BUFFERS)
SELECT d.invoice_id, c.full_name, d.total_amount, d.issued_at
FROM baove_invoice_perf d
JOIN core.customer c ON c.customer_id = d.customer_id
WHERE d.invoice_id = 12345
  AND d.issued_at >= timestamptz '2026-10-01 00:00:00+07'
  AND d.issued_at < timestamptz '2026-10-31 00:00:00+07';

ROLLBACK;
