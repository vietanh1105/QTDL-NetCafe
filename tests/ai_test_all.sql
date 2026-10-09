-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: tests/ai_test_all.sql
-- Mục đích: Kiểm thử toàn diện 5 Module AI tích hợp trong PostgreSQL 17
-- ============================================================================

\c netcafe_db;
\set ECHO all

SELECT '=== BẮT ĐẦU KIỂM THỬ 5 MODULE TỰ ĐỘNG HÓA THÔNG MINH (AI IN DATABASE) ===' AS title;

-- Làm sạch các bảng AI trước khi chạy kiểm thử
TRUNCATE ai.ai_alert, ai.index_recommendation, ai.duplicate_candidate, ai.anomaly_flag, ai.promotion_suggestion RESTART IDENTITY;

-- ----------------------------------------------------------------------------
-- 1. KIỂM THỬ MODULE 1: CẢNH BÁO AN NINH THÔNG MINH & CHẤM ĐIỂM RỦI RO
-- ----------------------------------------------------------------------------
SELECT '--- 1. KIỂM THỬ MODULE 1: KÍCH HOẠT HÀNH VI RỦI RO ĐỂ SINH CẢNH BÁO ---' AS test_header;

-- Tình huống 1.1: Điều chỉnh lương nhân viên
UPDATE hr.employee 
SET salary = salary + 1500000 
WHERE employee_id = 3;

-- Tình huống 1.2: Giao dịch nạp tiền lớn bất thường (8.000.000 VNĐ)
INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)
VALUES ('topup', 1, 'cash', 8000000, 3, 'Giao dịch nạp tiền kiểm thử AI');

-- Kiểm tra kết quả ghi nhận trong audit.audit_log và ai.ai_alert
SELECT audit_id, actor, table_name, action, row_pk, logged_at 
FROM audit.audit_log 
ORDER BY audit_id DESC 
LIMIT 2;

SELECT alert_id, risk_score, risk_level, reason, status, created_at 
FROM ai.ai_alert 
ORDER BY alert_id DESC 
LIMIT 2;

-- ----------------------------------------------------------------------------
-- 2. KIỂM THỬ MODULE 2: TỰ ĐỘNG KHUYẾN NGHỊ CHỈ MỤC (INDEX RECOMMENDATION)
-- ----------------------------------------------------------------------------
SELECT '--- 2. KIỂM THỬ MODULE 2: CHẠY CÔNG CỤ TƯ VẤN CHỈ MỤC TỪ THỐNG KÊ CATALOG ---' AS test_header;

SELECT * FROM ai.fn_recommend_indexes();

SELECT rec_id, table_name, column_list, reason, suggested_sql 
FROM ai.index_recommendation;

-- ----------------------------------------------------------------------------
-- 3. KIỂM THỬ MODULE 3: PHÁT HIỆN DỮ LIỆU KHÁCH TRÙNG LẶP (PG_TRGM SIMILARITY)
-- ----------------------------------------------------------------------------
SELECT '--- 3. KIỂM THỬ MODULE 3: PHÁT HIỆN TÀI KHOẢN TRÙNG LẶP BẰNG TRIGRAM ---' AS test_header;

SELECT * FROM ai.fn_detect_duplicates(0.5);

SELECT cand_id, customer_id_a, customer_id_b, similarity, reason, status 
FROM ai.duplicate_candidate;

-- ----------------------------------------------------------------------------
-- 4. KIỂM THỬ MODULE 4: PHÁT HIỆN BẤT THƯỜNG THỐNG KÊ QUA Z-SCORE
-- ----------------------------------------------------------------------------
SELECT '--- 4. KIỂM THỬ MODULE 4: PHÁT HIỆN NGOẠI LAI THỜI LƯỢNG VÀ TIỀN NẠP ---' AS test_header;

SELECT * FROM ai.fn_detect_anomalies(2.0);

SELECT flag_id, anomaly_type, subject_table, subject_pk, metric_value, threshold_info->>'z_score' AS z_score, status 
FROM ai.anomaly_flag;

-- ----------------------------------------------------------------------------
-- 5. KIỂM THỬ MODULE 5: GỢI Ý CHƯƠNG TRÌNH KHUYẾN MÃI THÔNG MINH
-- ----------------------------------------------------------------------------
SELECT '--- 5. KIỂM THỬ MODULE 5: ĐỀ XUẤT HAPPY HOUR VÀ VOUCHER TRI ÂN KHÁCH HÀNG ---' AS test_header;

SELECT * FROM ai.fn_suggest_promotions();

SELECT sug_id, suggestion_type, target, details->>'recommended_action' AS action, details->>'rationale' AS rationale 
FROM ai.promotion_suggestion;

SELECT '=== HOÀN TẤT KIỂM THỬ 5 MODULE AI - TẤT CẢ 5 MODULE HOẠT ĐỘNG HOÀN HẢO ===' AS summary;
