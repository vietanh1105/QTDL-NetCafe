-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- HƯỚNG DẪN THỰC THI TOÀN BỘ 5 MODULE IN-DATABASE AI
-- Hướng dẫn: Mở DBeaver / pgAdmin, kết nối tới CSDL Neon Cloud và bôi đen từng lệnh để chạy
-- ============================================================================

-- ============================================================================
-- MODULE 1: CẢNH BÁO AN NINH THÔNG MINH THỜI GIAN THỰC (AI RISK SCORING & LISTEN/NOTIFY)
-- Diễn ra: Khi có giao dịch lớn (>= 5tr), hủy hóa đơn (VOID), hoặc sửa lương ngoài giờ,
--          Trigger tự động chấm điểm Risk Score (0-100), ghi vào ai_alert và bắn tín hiệu NOTIFY.
-- ============================================================================
-- Bước 1.1: (Tùy chọn) Bật cửa sổ PowerShell riêng chạy script lắng nghe tín hiệu:
--           python python/listen_ai_alerts.py

-- Bước 1.2: Kích hoạt Trigger bằng cách tạo một giao dịch nạp tiền lớn (10.000.000 VNĐ):
INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)
VALUES ('topup', 1, 'bank_transfer', 10000000, 3, 'Giao dịch nạp tiền kích hoạt AI Trigger');

-- Bước 1.3: Xem danh sách các cảnh báo an ninh mà AI đã bắt được:
SELECT 
    alert_id,
    audit_logged_at,
    risk_score,
    risk_level,
    reason,
    status
FROM ai.ai_alert
ORDER BY alert_id DESC
LIMIT 5;


-- ============================================================================
-- MODULE 2: TỰ ĐỘNG KHUYẾN NGHỊ CHỈ MỤC (INDEX RECOMMENDATION ENGINE)
-- Diễn ra: AI quét pg_stat_user_tables và các Foreign Keys để tìm các bảng có lượt Seq Scan cao
--          hoặc khóa ngoại chưa có B-Tree index, tự động sinh cú pháp CREATE INDEX tối ưu.
-- ============================================================================
SELECT 
    recommendation_id,
    table_name,
    column_list,
    reason,
    suggested_sql
FROM ai.fn_recommend_indexes();


-- ============================================================================
-- MODULE 3: PHÁT HIỆN TÀI KHOẢN TRÙNG LẶP MỜ (DUPLICATE CUSTOMER VIA PG_TRGM)
-- Diễn ra: Sử dụng thuật toán Trigram (pg_trgm) kết hợp unaccent để so khớp tên tiếng Việt
--          không phụ thuộc dấu và đuôi số điện thoại để chống gian lận tạo nhiều nick.
-- ============================================================================
SELECT 
    candidate_id,
    customer_id_a,
    customer_name_a,
    customer_id_b,
    customer_name_b,
    similarity,
    reason
FROM ai.fn_detect_duplicates(0.65);


-- ============================================================================
-- MODULE 4: PHÁT HIỆN BẤT THƯỜNG THỐNG KÊ (ANOMALY DETECTION VIA Z-SCORE)
-- Diễn ra: Tính độ lệch chuẩn (StdDev) và trung bình mẫu (Mean) của các phiên chơi và nạp tiền.
--          Bất kỳ giá trị nào có Z-Score >= 1.5 sẽ bị gắn cờ ngoại lai (Outlier).
-- ============================================================================
SELECT 
    flag_id,
    anomaly_type,
    subject_table,
    subject_pk,
    metric_value,
    z_score,
    detail_info->>'mean' AS trung_binh_mau,
    detail_info->>'stddev' AS do_lech_chuan
FROM ai.fn_detect_anomalies(1.5);


-- ============================================================================
-- MODULE 5: GỢI Ý CHƯƠNG TRÌNH KHUYẾN MÃI THÔNG MINH (SMART PROMOTION SUGGESTION)
-- Diễn ra: Khai phá Materialized Views: phát hiện khung giờ vắng khách (Happy Hour)
--          và phân tích RFM để phát hiện khách VIP có nguy cơ rời bỏ phòng máy (Win-Back VIP).
-- ============================================================================
SELECT 
    sug_id,
    suggestion_type,
    target,
    details->>'recommended_action' AS hanh_dong_de_xuat,
    details->>'rationale' AS ly_do_ai_phan_tich
FROM ai.promotion_suggestion;
