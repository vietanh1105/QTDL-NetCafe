-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 07_ai_modules.sql
-- Mục đích: Triển khai 5 Module "AI" (Tự động hóa thông minh trong PostgreSQL):
--           1. Cảnh báo an ninh thông minh (Security Alert & Risk Scoring)
--           2. Tự động khuyến nghị chỉ mục (Index Recommendation Engine)
--           3. Phát hiện khách hàng trùng lặp mờ (Duplicate Customer Detection via pg_trgm)
--           4. Phát hiện bất thường thống kê Z-Score (Statistical Anomaly Detection)
--           5. Gợi ý chương trình khuyến mãi thông minh (Smart Promotion Suggestion)
--           Toàn bộ thuật toán chạy 100% bằng SQL/PLpgSQL nội bộ CSDL, không phụ thuộc API ngoài.
-- ============================================================================

\c netcafe_db;

-- ----------------------------------------------------------------------------
-- MODULE 1: CẢNH BÁO AN NINH THÔNG MINH (SECURITY ALERT & RISK SCORING)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Khi có thay đổi trên các bảng nhạy cảm (nhân sự, hóa đơn, thanh toán),
-- Trigger tự động đánh giá mức độ rủi ro (Risk Score từ 0 đến 100) dựa trên quy tắc an ninh
-- và thời gian phát sinh; nếu vượt ngưỡng cảnh báo (>= 40) thì tự động ghi vào ai_alert 
-- và phát tín hiệu NOTIFY thời gian thực tới ứng dụng quản trị.

CREATE OR REPLACE FUNCTION audit.fn_audit_security_alert_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_audit_id BIGINT;
    v_logged_at TIMESTAMPTZ := clock_timestamp();
    v_actor VARCHAR(50);
    v_action VARCHAR(10) := TG_OP;
    v_pk_val VARCHAR(50);
    v_old_json JSONB := NULL;
    v_new_json JSONB := NULL;
    v_risk_score INT := 10;
    v_risk_level VARCHAR(10) := 'low';
    v_reason TEXT := '';
    v_alert_id INT;
BEGIN
    -- Nhận diện actor đăng nhập
    v_actor := COALESCE(NULLIF(current_setting('role', true), 'none'), session_user);

    -- Trích xuất Primary Key an toàn theo từng bảng
    IF TG_TABLE_NAME = 'employee' THEN
        v_pk_val := CASE WHEN TG_OP = 'DELETE' THEN OLD.employee_id::text ELSE NEW.employee_id::text END;
    ELSIF TG_TABLE_NAME = 'payment' THEN
        v_pk_val := CASE WHEN TG_OP = 'DELETE' THEN OLD.payment_id::text ELSE NEW.payment_id::text END;
    ELSIF TG_TABLE_NAME = 'invoice' THEN
        v_pk_val := CASE WHEN TG_OP = 'DELETE' THEN OLD.invoice_id::text ELSE NEW.invoice_id::text END;
    ELSE
        v_pk_val := 'unknown';
    END IF;

    -- Trích xuất JSON và kiểm tra rủi ro
    IF (TG_OP = 'DELETE') THEN
        v_old_json := to_jsonb(OLD);
        v_risk_score := v_risk_score + 30;
        v_reason := v_reason || '[Hành động xóa dữ liệu nhạy cảm] ';
    ELSIF (TG_OP = 'UPDATE') THEN
        v_old_json := to_jsonb(OLD);
        v_new_json := to_jsonb(NEW);
        
        -- Kiểm tra thay đổi lương nhân viên
        IF TG_TABLE_NAME = 'employee' THEN
            IF (OLD.salary IS DISTINCT FROM NEW.salary) THEN
                v_risk_score := v_risk_score + 45;
                v_reason := v_reason || format('[Điều chỉnh lương: %s -> %s] ', OLD.salary, NEW.salary);
            END IF;
        ELSIF TG_TABLE_NAME = 'invoice' THEN
            IF NEW.status = 'void' AND OLD.status <> 'void' THEN
                v_risk_score := v_risk_score + 50;
                v_reason := v_reason || '[Hủy hóa đơn đã xuất (VOID)] ';
            END IF;
        END IF;
    ELSIF (TG_OP = 'INSERT') THEN
        v_new_json := to_jsonb(NEW);

        -- Kiểm tra giao dịch nạp tiền hoặc thanh toán lớn bất thường (>= 5.000.000 VNĐ)
        IF TG_TABLE_NAME = 'payment' THEN
            IF NEW.amount >= 5000000 THEN
                v_risk_score := v_risk_score + 40;
                v_reason := v_reason || format('[Giao dịch tài chính lớn: %s VNĐ] ', NEW.amount);
            END IF;
        END IF;
    END IF;

    -- Kiểm tra phát sinh ngoài giờ làm việc (từ 23h đêm đến 6h sáng)
    IF EXTRACT(HOUR FROM v_logged_at) < 6 OR EXTRACT(HOUR FROM v_logged_at) >= 23 THEN
        v_risk_score := v_risk_score + 25;
        v_reason := v_reason || '[Thực hiện ngoài giờ làm việc thông thường] ';
    END IF;

    -- Giới hạn điểm rủi ro tối đa 100
    IF v_risk_score > 100 THEN v_risk_score := 100; END IF;

    -- 1. Ghi vào sổ nhật ký kiểm toán phân vùng audit_log
    INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
    VALUES (v_logged_at, v_actor, TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME, v_action, v_pk_val, v_old_json, v_new_json)
    RETURNING audit_id INTO v_audit_id;

    -- 2. Nếu vượt ngưỡng rủi ro (>= 40), sinh cảnh báo AI và phát NOTIFY
    IF v_risk_score >= 40 THEN
        IF v_risk_score >= 70 THEN
            v_risk_level := 'high';
        ELSE
            v_risk_level := 'medium';
        END IF;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (v_audit_id, v_logged_at, v_risk_score, v_risk_level, TRIM(v_reason), 'new')
        RETURNING alert_id INTO v_alert_id;

        -- Gửi thông điệp thời gian thực cho Desktop App qua LISTEN/NOTIFY
        PERFORM pg_notify(
            'ai_security_alert',
            json_build_object(
                'alert_id', v_alert_id,
                'risk_score', v_risk_score,
                'risk_level', v_risk_level,
                'table', TG_TABLE_NAME,
                'actor', v_actor,
                'reason', v_reason
            )::text
        );
    END IF;

    IF (TG_OP = 'DELETE') THEN
        RETURN OLD;
    ELSE
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Gắn trigger an ninh vào các bảng trọng yếu
DROP TRIGGER IF EXISTS trg_audit_security_employee ON hr.employee;
CREATE TRIGGER trg_audit_security_employee
    AFTER INSERT OR UPDATE OR DELETE ON hr.employee
    FOR EACH ROW EXECUTE FUNCTION audit.fn_audit_security_alert_trigger();

DROP TRIGGER IF EXISTS trg_audit_security_payment ON billing.payment;
CREATE TRIGGER trg_audit_security_payment
    AFTER INSERT OR UPDATE OR DELETE ON billing.payment
    FOR EACH ROW EXECUTE FUNCTION audit.fn_audit_security_alert_trigger();

DROP TRIGGER IF EXISTS trg_audit_security_invoice ON billing.invoice;
CREATE TRIGGER trg_audit_security_invoice
    AFTER INSERT OR UPDATE OR DELETE ON billing.invoice
    FOR EACH ROW EXECUTE FUNCTION audit.fn_audit_security_alert_trigger();

COMMENT ON FUNCTION audit.fn_audit_security_alert_trigger IS 'Module AI 1: Trigger kiểm toán và chấm điểm rủi ro an ninh tự động';

-- ----------------------------------------------------------------------------
-- MODULE 2: TỰ ĐỘNG KHUYẾN NGHỊ CHỈ MỤC (INDEX RECOMMENDATION ENGINE)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Quét qua catalog pg_stat_user_tables và các ràng buộc khóa ngoại (Foreign Key)
-- để nhận diện các bảng có tỷ lệ quét tuần tự cao hoặc các FK chưa được đánh chỉ mục.
CREATE OR REPLACE FUNCTION ai.fn_recommend_indexes()
RETURNS TABLE(
    recommendation_id INT,
    table_name VARCHAR(100),
    column_list VARCHAR(255),
    reason TEXT,
    suggested_sql TEXT
) AS $$
DECLARE
    r RECORD;
    v_rec_id INT;
    v_tbl TEXT;
    v_col TEXT;
    v_sql TEXT;
    v_reason TEXT;
BEGIN
    -- 1. Tìm các bảng có tỷ lệ Sequential Scan cao trên dữ liệu đáng kể
    FOR r IN
        SELECT 
            schemaname || '.' || relname AS full_table,
            relname,
            seq_scan,
            idx_scan,
            n_live_tup
        FROM pg_stat_user_tables
        WHERE schemaname IN ('core', 'inventory', 'billing', 'hr')
          AND seq_scan > 5
          AND (idx_scan IS NULL OR idx_scan < seq_scan)
    LOOP
        v_tbl := r.full_table;
        v_col := 'created_at / status / filter_columns';
        v_reason := format('Bảng %s có %s lượt quét tuần tự (seq_scan) trong khi chỉ có %s lượt quét chỉ mục.', 
                           v_tbl, r.seq_scan, COALESCE(r.idx_scan, 0));
        v_sql := format('CREATE INDEX idx_%s_auto_opt ON %s (created_at);', r.relname, v_tbl);

        -- Ghi vào bảng khuyến nghị nếu chưa tồn tại
        IF NOT EXISTS (SELECT 1 FROM ai.index_recommendation WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql) THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;

            recommendation_id := v_rec_id;
            table_name := v_tbl;
            column_list := v_col;
            reason := v_reason;
            suggested_sql := v_sql;
            RETURN NEXT;
        END IF;
    END LOOP;

    -- 2. Kiểm tra Foreign Keys thiếu index (ngăn ngừa table lock và chậm JOIN)
    FOR r IN
        SELECT 
            n.nspname || '.' || c.relname AS full_tbl,
            c.relname,
            a.attname AS col_name
        FROM pg_constraint con
        JOIN pg_class c ON c.oid = con.conrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = ANY(con.conkey)
        WHERE con.contype = 'f'
          AND n.nspname IN ('core', 'inventory', 'billing', 'hr')
          AND NOT EXISTS (
              SELECT 1 FROM pg_index i
              WHERE i.indrelid = con.conrelid
                AND a.attnum = i.indkey[0]
          )
    LOOP
        v_tbl := r.full_tbl;
        v_col := r.col_name;
        v_reason := format('Cột khóa ngoại %s.%s chưa có B-Tree index hỗ trợ truy vấn JOIN và chống lock.', v_tbl, v_col);
        v_sql := format('CREATE INDEX idx_%s_%s_fk ON %s (%s);', r.relname, v_col, v_tbl, v_col);

        IF NOT EXISTS (SELECT 1 FROM ai.index_recommendation WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql) THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;

            recommendation_id := v_rec_id;
            table_name := v_tbl;
            column_list := v_col;
            reason := v_reason;
            suggested_sql := v_sql;
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ai.fn_recommend_indexes IS 'Module AI 2: Công cụ tự động khuyến nghị chỉ mục từ thống kê catalog';

-- ----------------------------------------------------------------------------
-- MODULE 3: PHÁT HIỆN DỮ LIỆU KHÁCH TRÙNG LẶP (DUPLICATE DETECTION VIA PG_TRGM)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Sử dụng hàm similarity() của extension pg_trgm kết hợp unaccent để so khớp
-- độ tương đồng họ tên tiếng Việt (không phụ thuộc dấu và viết hoa) và so khớp 7 số đuôi điện thoại.
CREATE OR REPLACE FUNCTION ai.fn_detect_duplicates(p_similarity_threshold REAL DEFAULT 0.65)
RETURNS TABLE(
    candidate_id INT,
    customer_id_a INT,
    customer_name_a VARCHAR(100),
    customer_id_b INT,
    customer_name_b VARCHAR(100),
    similarity NUMERIC(4,3),
    reason TEXT
) AS $$
DECLARE
    r RECORD;
    v_cand_id INT;
BEGIN
    FOR r IN
        SELECT 
            c1.customer_id AS id_a,
            c1.full_name AS name_a,
            c1.phone AS phone_a,
            c2.customer_id AS id_b,
            c2.full_name AS name_b,
            c2.phone AS phone_b,
            similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name)))::NUMERIC(4,3) AS sim_score,
            CASE 
                WHEN right(c1.phone, 7) = right(c2.phone, 7) AND similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name))) >= p_similarity_threshold
                    THEN 'Trùng 7 số cuối điện thoại và tên tương đồng cao (> ' || p_similarity_threshold || ')'
                WHEN right(c1.phone, 7) = right(c2.phone, 7)
                    THEN 'Trùng 7 số cuối số điện thoại liên hệ'
                ELSE 'Họ tên tương đồng cao theo thuật toán Trigram (> ' || p_similarity_threshold || ')'
            END AS match_reason
        FROM core.customer c1
        JOIN core.customer c2 ON c1.customer_id < c2.customer_id
        WHERE (similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name))) >= p_similarity_threshold AND right(c1.phone, 4) = right(c2.phone, 4))
           OR (similarity(unaccent(lower(c1.full_name)), unaccent(lower(c2.full_name))) >= 0.85)
           OR (right(c1.phone, 7) = right(c2.phone, 7))
    LOOP
        -- Chèn hoặc cập nhật bảng ai.duplicate_candidate
        IF NOT EXISTS (
            SELECT 1 FROM ai.duplicate_candidate 
            WHERE ai.duplicate_candidate.customer_id_a = r.id_a AND ai.duplicate_candidate.customer_id_b = r.id_b
        ) THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (r.id_a, r.id_b, r.sim_score, r.match_reason, 'pending')
            RETURNING cand_id INTO v_cand_id;

            candidate_id := v_cand_id;
            customer_id_a := r.id_a;
            customer_name_a := r.name_a;
            customer_id_b := r.id_b;
            customer_name_b := r.name_b;
            similarity := r.sim_score;
            reason := r.match_reason;
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ai.fn_detect_duplicates IS 'Module AI 3: Tự động phát hiện cặp khách hàng trùng lặp bằng pg_trgm';

-- ----------------------------------------------------------------------------
-- MODULE 4: PHÁT HIỆN BẤT THƯỜNG THỐNG KÊ (ANOMALY DETECTION VIA Z-SCORE)
-- ----------------------------------------------------------------------------
-- Giải thích DBA: Sử dụng công thức chuẩn hóa thống kê Z-score: Z = |X - Mean| / StdDev.
-- Bất kỳ giá trị nào có Z >= threshold (thường là 2.0 hoặc 2.5) tương đương nằm ngoài 
-- 95% - 99% phân phối chuẩn, được xem là ngoại lai (outlier) cần nhân viên kiểm tra.
CREATE OR REPLACE FUNCTION ai.fn_detect_anomalies(p_z_threshold REAL DEFAULT 2.0)
RETURNS TABLE(
    flag_id INT,
    anomaly_type VARCHAR(50),
    subject_table VARCHAR(50),
    subject_pk VARCHAR(50),
    metric_value NUMERIC(15,2),
    z_score NUMERIC(6,2),
    detail_info JSONB
) AS $$
DECLARE
    v_avg_dur NUMERIC;
    v_std_dur NUMERIC;
    v_avg_pay NUMERIC;
    v_std_pay NUMERIC;
    r RECORD;
    v_flag_id INT;
    v_z NUMERIC(6,2);
BEGIN
    -- 1. BẤT THƯỜNG THỜI LƯỢNG PHIÊN CHƠI (core.gaming_session)
    SELECT 
        AVG(EXTRACT(EPOCH FROM (end_time - start_time)) / 60.0),
        NULLIF(STDDEV_SAMP(EXTRACT(EPOCH FROM (end_time - start_time)) / 60.0), 0)
    INTO v_avg_dur, v_std_dur
    FROM core.gaming_session
    WHERE status = 'finished' AND end_time IS NOT NULL;

    IF v_std_dur IS NOT NULL AND v_std_dur > 0 THEN
        FOR r IN
            SELECT 
                session_id,
                ROUND((EXTRACT(EPOCH FROM (end_time - start_time)) / 60.0)::numeric, 2) AS duration_mins
            FROM core.gaming_session
            WHERE status = 'finished' AND end_time IS NOT NULL
        LOOP
            v_z := ROUND((ABS(r.duration_mins - v_avg_dur) / v_std_dur)::numeric, 2);
            IF v_z >= p_z_threshold THEN
                IF NOT EXISTS (
                    SELECT 1 FROM ai.anomaly_flag 
                    WHERE ai.anomaly_flag.subject_table = 'core.gaming_session' 
                      AND ai.anomaly_flag.subject_pk = r.session_id::text
                ) THEN
                    INSERT INTO ai.anomaly_flag (anomaly_type, subject_table, subject_pk, metric_value, threshold_info, status)
                    VALUES (
                        'SESSION_DURATION_OUTLIER',
                        'core.gaming_session',
                        r.session_id::text,
                        r.duration_mins,
                        jsonb_build_object('metric', 'duration_minutes', 'mean', ROUND(v_avg_dur, 2), 'stddev', ROUND(v_std_dur, 2), 'z_score', v_z, 'threshold', p_z_threshold),
                        'open'
                    )
                    RETURNING ai.anomaly_flag.flag_id INTO v_flag_id;

                    flag_id := v_flag_id;
                    anomaly_type := 'SESSION_DURATION_OUTLIER';
                    subject_table := 'core.gaming_session';
                    subject_pk := r.session_id::text;
                    metric_value := r.duration_mins;
                    z_score := v_z;
                    detail_info := jsonb_build_object('mean', ROUND(v_avg_dur, 2), 'stddev', ROUND(v_std_dur, 2), 'z_score', v_z);
                    RETURN NEXT;
                END IF;
            END IF;
        END LOOP;
    END IF;

    -- 2. BẤT THƯỜNG SỐ TIỀN THANH TOÁN / NẠP VÍ (billing.payment)
    SELECT 
        AVG(amount),
        NULLIF(STDDEV_SAMP(amount), 0)
    INTO v_avg_pay, v_std_pay
    FROM billing.payment;

    IF v_std_pay IS NOT NULL AND v_std_pay > 0 THEN
        FOR r IN
            SELECT payment_id, amount
            FROM billing.payment
        LOOP
            v_z := ROUND((ABS(r.amount - v_avg_pay) / v_std_pay)::numeric, 2);
            IF v_z >= p_z_threshold THEN
                IF NOT EXISTS (
                    SELECT 1 FROM ai.anomaly_flag 
                    WHERE ai.anomaly_flag.subject_table = 'billing.payment' 
                      AND ai.anomaly_flag.subject_pk = r.payment_id::text
                ) THEN
                    INSERT INTO ai.anomaly_flag (anomaly_type, subject_table, subject_pk, metric_value, threshold_info, status)
                    VALUES (
                        'PAYMENT_AMOUNT_OUTLIER',
                        'billing.payment',
                        r.payment_id::text,
                        r.amount,
                        jsonb_build_object('metric', 'amount_vnd', 'mean', ROUND(v_avg_pay, 2), 'stddev', ROUND(v_std_pay, 2), 'z_score', v_z, 'threshold', p_z_threshold),
                        'open'
                    )
                    RETURNING ai.anomaly_flag.flag_id INTO v_flag_id;

                    flag_id := v_flag_id;
                    anomaly_type := 'PAYMENT_AMOUNT_OUTLIER';
                    subject_table := 'billing.payment';
                    subject_pk := r.payment_id::text;
                    metric_value := r.amount;
                    z_score := v_z;
                    detail_info := jsonb_build_object('mean', ROUND(v_avg_pay, 2), 'stddev', ROUND(v_std_pay, 2), 'z_score', v_z);
                    RETURN NEXT;
                END IF;
            END IF;
        END LOOP;
    END IF;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ai.fn_detect_anomalies IS 'Module AI 4: Phát hiện ngoại lai thống kê bằng thuật toán Z-score';

-- ----------------------------------------------------------------------------
-- MODULE 5: GỢI Ý CHƯƠNG TRÌNH KHUYẾN MÃI THÔNG MINH (SMART PROMOTION SUGGESTION)
-- ----------------------------------------------------------------------------
-- Giải thích DBA:
-- Chiến lược 1: Khai phá dữ liệu theo giờ từ mv_hourly_usage, phát hiện khung giờ vắng khách
-- để đề xuất chương trình Happy Hour kích cầu.
-- Chiến lược 2: Phân nhóm khách hàng RFM (Recency - Frequency - Monetary) bằng hàm cửa sổ
-- NTILE(4) từ mv_top_customers để phát hiện nhóm khách chi tiêu lớn nhưng lâu không quay lại (At-Risk Churn).
CREATE OR REPLACE FUNCTION ai.fn_suggest_promotions()
RETURNS TABLE(
    suggestion_id INT,
    suggestion_type VARCHAR(50),
    target VARCHAR(100),
    details JSONB
) AS $$
DECLARE
    r RECORD;
    v_sug_id INT;
BEGIN
    -- Làm mới Materialized Views để có số liệu phân tích mới nhất
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;

    -- 1. Đề xuất Happy Hour cho các khung giờ thấp điểm (lượng phiên dưới mức trung bình)
    FOR r IN
        WITH hourly_stat AS (
            SELECT 
                hour_of_day,
                AVG(session_count) AS avg_sessions
            FROM reporting.mv_hourly_usage
            GROUP BY hour_of_day
        ),
        overall AS (
            SELECT AVG(session_count) AS global_avg
            FROM reporting.mv_hourly_usage
        )
        SELECT 
            h.hour_of_day,
            ROUND(h.avg_sessions, 1) AS session_vol,
            ROUND(o.global_avg, 1) AS benchmark_avg
        FROM hourly_stat h
        CROSS JOIN overall o
        WHERE h.avg_sessions < COALESCE(o.global_avg * 0.7, 10)
        ORDER BY h.avg_sessions ASC
        LIMIT 3
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM ai.promotion_suggestion 
            WHERE ai.promotion_suggestion.suggestion_type = 'HAPPY_HOUR_OFFPEAK'
              AND ai.promotion_suggestion.target = format('Khung giờ %sh:00', r.hour_of_day)
        ) THEN
            INSERT INTO ai.promotion_suggestion (suggestion_type, target, details)
            VALUES (
                'HAPPY_HOUR_OFFPEAK',
                format('Khung giờ %sh:00', r.hour_of_day),
                jsonb_build_object(
                    'hour', r.hour_of_day,
                    'current_sessions', r.session_vol,
                    'benchmark_avg', r.benchmark_avg,
                    'recommended_action', 'Giảm 25% giá tiền giờ hoặc tặng thêm 30 phút nạp thẻ',
                    'rationale', 'Khung giờ thấp điểm có lượng khách dưới mức trung bình ngày'
                )
            )
            RETURNING sug_id INTO v_sug_id;

            suggestion_id := v_sug_id;
            suggestion_type := 'HAPPY_HOUR_OFFPEAK';
            target := format('Khung giờ %sh:00', r.hour_of_day);
            details := jsonb_build_object('hour', r.hour_of_day, 'sessions', r.session_vol);
            RETURN NEXT;
        END IF;
    END LOOP;

    -- 2. Đề xuất Win-Back Voucher cho khách hàng VIP có nguy cơ rời bỏ (At-Risk High-Value)
    FOR r IN
        SELECT 
            customer_id,
            full_name,
            total_spent,
            total_sessions,
            last_played_at,
            ROUND(EXTRACT(EPOCH FROM (NOW() - COALESCE(last_played_at, NOW() - INTERVAL '5 days'))) / 86400.0) AS days_inactive
        FROM reporting.mv_top_customers
        WHERE total_spent >= 50000 
          AND (last_played_at IS NULL OR last_played_at < (NOW() - INTERVAL '2 days'))
        ORDER BY total_spent DESC
        LIMIT 5
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM ai.promotion_suggestion 
            WHERE ai.promotion_suggestion.suggestion_type = 'WIN_BACK_VIP'
              AND ai.promotion_suggestion.target = format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id)
        ) THEN
            INSERT INTO ai.promotion_suggestion (suggestion_type, target, details)
            VALUES (
                'WIN_BACK_VIP',
                format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id),
                jsonb_build_object(
                    'customer_id', r.customer_id,
                    'total_spent', r.total_spent,
                    'days_inactive', r.days_inactive,
                    'recommended_action', 'Gửi SMS/Zalo tặng voucher 30.000 VNĐ kèm đồ uống miễn phí',
                    'rationale', 'Hội viên chi tiêu cao chưa đến phòng máy trong hơn 3 ngày qua'
                )
            )
            RETURNING sug_id INTO v_sug_id;

            suggestion_id := v_sug_id;
            suggestion_type := 'WIN_BACK_VIP';
            target := format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id);
            details := jsonb_build_object('customer_id', r.customer_id, 'spent', r.total_spent, 'days_inactive', r.days_inactive);
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ai.fn_suggest_promotions IS 'Module AI 5: Đề xuất ưu đãi thông minh từ phân tích RFM và khung giờ thấp điểm';
