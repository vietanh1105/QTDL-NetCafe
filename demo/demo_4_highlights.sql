-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- PHẦN 4: DEMO ĐIỂM NHẤN TÍCH HỢP DỮ LIỆU VÀ AI (INTEGRATION / AI DEMO)
-- Hướng dẫn: Trình diễn 3 điểm nhấn theo đúng thang điểm và điểm cộng của Giảng viên
-- ============================================================================

-- ----------------------------------------------------------------------------
-- [ĐIỂM NHẤN 4.1]: TRUY VẤN BÓC TÁCH DỮ LIỆU BÁN CẤU TRÚC (NOSQL) TỪ CỘT JSONB
-- Sử dụng toán tử ->> (trích xuất text) và toán tử chứa tập con @> (JSON Containment)
-- ----------------------------------------------------------------------------
-- Bóc tách cấu hình phần cứng linh hoạt của từng máy trạm (CPU, GPU, RAM, Màn hình):
SELECT 
    computer_id, 
    computer_code, 
    computer_name,
    specs ->> 'cpu'     AS cpu_model,
    specs ->> 'gpu'     AS graphics_card,
    specs ->> 'ram'     AS memory_size,
    specs ->> 'monitor' AS display_panel
FROM core.computer
WHERE specs ->> 'gpu' ILIKE '%RTX%'
ORDER BY computer_id;

-- Kết hợp chỉ mục GIN jsonb_path_ops để truy vấn NoSQL với tốc độ O(1):
EXPLAIN (ANALYZE, BUFFERS)
SELECT computer_code, computer_name, specs 
FROM core.computer 
WHERE specs @> '{"gpu": "RTX 4070"}'::jsonb;


-- ----------------------------------------------------------------------------
-- [ĐIỂM NHẤN 4.2]: LUỒNG PYTHON ETL TỰ ĐỘNG LẤY DỮ LIỆU, LÀM SẠCH VÀ NẠP LẠI
-- Thao tác trên Terminal:
--   1. python python/generate_dirty_data.py  (Nạp 20 dòng bẩn vào staging)
--   2. python python/clean_data.py           (Làm sạch, nạp 16 dòng chuẩn, cách ly 4 dòng)
-- ----------------------------------------------------------------------------
-- Kiểm tra bảng cách ly dữ liệu bẩn (Rejected Rows) trên DBeaver / pgAdmin:
SELECT rejected_id, source_table, error_reason, raw_record, rejected_at
FROM staging.rejected_rows
ORDER BY rejected_id DESC
LIMIT 10;


-- ----------------------------------------------------------------------------
-- [ĐIỂM NHẤN 4.3 - ĐIỂM CỘNG XUẤT SẮC]: LUỒNG PYTHON LẮNG NGHE LISTEN/NOTIFY
-- KẾT HỢP VỚI TRIGGER CẢNH BÁO AI THỜI GIAN THỰC (REAL-TIME STREAMING)
-- ----------------------------------------------------------------------------
-- CÁCH THỰC HIỆN TRONG BUỔI BẢO VỆ:
-- Bước 1 (Terminal): Bật terminal chạy tiến trình lắng nghe:
--    python python/listen_ai_alerts.py
-- Bước 2 (DBeaver / pgAdmin): Chạy câu lệnh nạp tiền lớn bất thường dưới đây:

INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)
VALUES ('topup', 1, 'cash', 10000000, 3, 'Giao dịch nạp tiền demo kích hoạt AI Trigger');

-- -> QUAN SÁT: Ngay lập tức trên cửa sổ Terminal Python sẽ nổ ra CẢNH BÁO ĐỎ thời gian thực:
--    [🚨 CẢNH BÁO AN NINH PHÁT HIỆN LÚC ...]
--    - Kênh (Channel)     : ai_security_alert
--    - Mức độ rủi ro      : [HIGH] (Score: 70/100)
--    - Nguyên nhân AI     : Thanh toán/nạp tiền lớn bất thường: 10,000,000 VNĐ


-- ----------------------------------------------------------------------------
-- [ĐIỂM NHẤN 4.4 - MỞ RỘNG]: THUẬT TOÁN THỐNG KÊ PHÁT HIỆN BẤT THƯỜNG Z-SCORE
-- Tự động tính Độ lệch chuẩn (StdDev) và Trung bình mẫu để phát hiện phiên chơi bất thường:
-- ----------------------------------------------------------------------------
SELECT 
    flag_id, 
    anomaly_type, 
    subject_table, 
    subject_pk, 
    metric_value, 
    z_score, 
    detail_info->>'mean' AS mean_val, 
    detail_info->>'stddev' AS stddev_val
FROM ai.fn_detect_anomalies(1.5);


-- ----------------------------------------------------------------------------
-- [ĐIỂM NHẤN 4.5 - MỞ RỘNG]: GỢI Ý & TỰ ĐỘNG KÍCH HOẠT CHƯƠNG TRÌNH KHUYẾN MÃI
-- (Module AI 5: Khai phá dữ liệu giờ thấp điểm và phân nhóm RFM chống rời bỏ khách VIP)
-- KÈM BẮN TIN NHẮN SMS THỜI GIAN THỰC VỀ ĐIỆN THOẠI (0327469105 - Nguyễn Việt Anh)
-- ----------------------------------------------------------------------------
-- Hướng dẫn kích hoạt Demo SMS về máy thật:
--   Bước A (Terminal): python python/sms_gateway_listener.py
--   Bước B (Điện thoại): Mở link https://ntfy.sh/netcafe_vietanh (hoặc app ntfy)
--   Bước C (DBeaver): Chạy lệnh 2 bên dưới -> Tin nhắn VIP voucher lập tức nổ trên điện thoại!

-- 1. Xem 3 đề xuất thông minh do AI phân tích:
SELECT 
    sug_id,
    suggestion_type,
    target,
    details->>'recommended_action' AS recommended_action,
    details->>'rationale' AS rationale
FROM ai.promotion_suggestion;

-- 2. THỰC THI ÁP DỤNG: Tự động kích hoạt 3 khuyến mãi & BẮN TIN NHẮN SMS VỀ ĐIỆN THOẠI:
SELECT * FROM ai.fn_apply_promotions();

-- 3. KIỂM TRA BẢNG PROMOTION CHÍNH THỨC:
SELECT code, promotion_name, promo_type, value, valid_from, valid_to, is_active
FROM billing.promotion
WHERE conditions->>'auto_created_by_ai' = 'true';

-- 4. HOÀN TÁC (ROLLBACK): Hủy các khuyến mãi vừa kích hoạt, trả về trạng thái ban đầu:
SELECT * FROM ai.fn_rollback_promotions();
