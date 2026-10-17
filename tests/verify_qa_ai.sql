-- Trách nhiệm: kiểm chứng 5 module AI trên duy nhất netcafe_qa.
-- Không dùng file demo có \c netcafe_db hoặc truncate dữ liệu chính.

\set ON_ERROR_STOP on

DO $$
DECLARE
    v_before TIMESTAMPTZ;
    v_alert_count INT;
BEGIN
    IF current_database() <> 'netcafe_qa' THEN
        RAISE EXCEPTION 'verify_qa_ai.sql chỉ được chạy trên netcafe_qa';
    END IF;

    v_before := clock_timestamp();
    INSERT INTO billing.payment(kind, customer_id, method, amount, employee_id, reference)
    VALUES ('topup', 1, 'cash', 10000000, 1, 'TEST_AI_QA');

    SELECT count(*) INTO v_alert_count
    FROM ai.ai_alert
    WHERE audit_logged_at >= v_before;
    IF v_alert_count = 0 THEN
        RAISE EXCEPTION 'Security Alert không sinh bản ghi sau giao dịch TEST_AI_QA';
    END IF;
    RAISE NOTICE 'PASS security_alert=%', v_alert_count;
END $$;

SELECT count(*) AS index_recommendations FROM ai.fn_recommend_indexes();
SELECT count(*) AS duplicate_candidates FROM ai.fn_detect_duplicates(0.65);
SELECT count(*) AS anomaly_flags FROM ai.fn_detect_anomalies(2.0);
SELECT count(*) AS promotion_suggestions FROM ai.fn_suggest_promotions();
SELECT 'PASS ai_modules_5' AS result;
