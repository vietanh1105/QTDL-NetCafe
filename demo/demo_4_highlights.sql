-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
-- KỊCH BẢN DEMO 4: ĐIỂM SÁNG 5 MODULE TỰ ĐỘNG HÓA THÔNG MINH (IN-DATABASE AI)
-- Hướng dẫn: Trình diễn các thuật toán thông minh chạy 100% nội bộ trong PostgreSQL 17
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- MODULE 1: CẢNH BÁO AN NINH THÔNG MINH (CHẤM ĐIỂM RỦI RO & PHÁT NOTIFY)
-- ----------------------------------------------------------------------------
-- Minh họa: Đăng ký lắng nghe thông điệp qua LISTEN trên pgAdmin:
LISTEN ai_security_alert;

-- Thực hiện một hành vi đáng ngờ: Nạp số tiền lớn bất thường (10.000.000 VNĐ)
INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)
VALUES ('topup', 1, 'cash', 10000000, 3, 'Giao dịch nạp tiền demo an ninh');

-- Kiểm tra nhật ký kiểm toán và điểm rủi ro vừa được tính toán tự động:
SELECT alert_id, risk_score, risk_level, reason, status, created_at 
FROM ai.ai_alert 
ORDER BY alert_id DESC 
LIMIT 1;


-- ----------------------------------------------------------------------------
-- MODULE 2: TỰ ĐỘNG TƯ VẤN VÀ KHUYẾN NGHỊ CHỈ MỤC (INDEX RECOMMENDATION)
-- ----------------------------------------------------------------------------
-- Quét các bảng có tỷ lệ Sequential Scan cao và phát hiện khóa ngoại chưa có index:
SELECT * FROM ai.fn_recommend_indexes();


-- ----------------------------------------------------------------------------
-- MODULE 3: PHÁT HIỆN TÀI KHOẢN KHÁCH HÀNG TRÙNG LẶP (PG_TRGM SIMILARITY)
-- ----------------------------------------------------------------------------
-- Sử dụng similarity kết hợp unaccent để so khớp tên và 7 số cuối điện thoại:
-- Nhận diện ngay cặp tài khoản 'Tran Van Binh' và 'Trần Văn Bình':
SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason 
FROM ai.fn_detect_duplicates(0.65)
LIMIT 5;


-- ----------------------------------------------------------------------------
-- MODULE 4: PHÁT HIỆN BẤT THƯỜNG THỐNG KÊ (Z-SCORE / NGOẠI LAI OUTLIERS)
-- ----------------------------------------------------------------------------
-- Tự động tính Mean và StdDev, gắn cờ phiên chơi quên tắt (>35h) và tiền nạp đột biến:
SELECT 
    flag_id, 
    anomaly_type, 
    subject_table, 
    subject_pk, 
    metric_value, 
    z_score, 
    detail_info->>'mean' AS mean_val, 
    detail_info->>'stddev' AS stddev_val
FROM ai.fn_detect_anomalies(2.0);


-- ----------------------------------------------------------------------------
-- MODULE 5: GỢI Ý CHƯƠNG TRÌNH KHUYẾN MÃI THÔNG MINH (OFF-PEAK & WIN-BACK)
-- ----------------------------------------------------------------------------
-- Khai phá dữ liệu từ Materialized Views đề xuất Happy Hour và Voucher giữ chân khách VIP:
SELECT * FROM ai.fn_suggest_promotions();

SELECT 
    sug_id, 
    suggestion_type, 
    target, 
    details->>'recommended_action' AS action,
    details->>'rationale' AS rationale
FROM ai.promotion_suggestion;
