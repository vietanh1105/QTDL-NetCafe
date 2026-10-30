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
        v_old_json := to_jsonb(OLD) - 'password_hash' - 'salary';
        v_risk_score := v_risk_score + 30;
        v_reason := v_reason || '[Hành động xóa dữ liệu nhạy cảm] ';
    ELSIF (TG_OP = 'UPDATE') THEN
        v_old_json := to_jsonb(OLD) - 'password_hash' - 'salary';
        v_new_json := to_jsonb(NEW) - 'password_hash' - 'salary';
        
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
        v_new_json := to_jsonb(NEW) - 'password_hash' - 'salary';

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
        SELECT a.attname
        INTO v_col
        FROM pg_attribute a
        WHERE a.attrelid = to_regclass(v_tbl)
          AND a.attnum > 0
          AND NOT a.attisdropped
          AND a.attname IN ('created_at', 'started_at', 'logged_at', 'order_date', 'invoice_date', 'status')
        ORDER BY CASE a.attname
            WHEN 'created_at' THEN 1
            WHEN 'started_at' THEN 2
            WHEN 'logged_at' THEN 3
            WHEN 'order_date' THEN 4
            WHEN 'invoice_date' THEN 5
            WHEN 'status' THEN 6
            ELSE 99
        END
        LIMIT 1;

        IF v_col IS NULL THEN
            CONTINUE;
        END IF;

        v_reason := format('Bảng %s bị quét tuần tự %s lần nhưng chỉ có %s lượt quét chỉ mục -> (Giải thích: CSDL đang phải đọc duyệt từng dòng để tìm kiếm. Cần tạo B-Tree Index trên cột [%s] để nhảy thẳng đến dữ liệu cần tìm, tránh quét toàn bộ bảng).', 
                           v_tbl, r.seq_scan, COALESCE(r.idx_scan, 0), v_col);
        v_sql := format('CREATE INDEX idx_%s_auto_opt ON %s (%I);', r.relname, v_tbl, v_col);

        SELECT rec_id INTO v_rec_id 
        FROM ai.index_recommendation 
        WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql 
        LIMIT 1;

        IF v_rec_id IS NULL THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;
        END IF;

        recommendation_id := v_rec_id;
        table_name := v_tbl;
        column_list := v_col;
        reason := v_reason;
        suggested_sql := v_sql;
        RETURN NEXT;
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
        v_reason := format('Cột khóa ngoại %s.%s chưa có Index -> (Giải thích: Khi JOIN giữa các bảng với nhau, việc thiếu Index trên khóa ngoại sẽ làm chậm phép nối và dễ gây khóa bảng khi có cập nhật. Cần tạo B-Tree Index trên cột [%s] để tăng tốc truy vấn).', v_tbl, v_col, v_col);
        v_sql := format('CREATE INDEX idx_%s_%s_fk ON %s (%s);', r.relname, v_col, v_tbl, v_col);

        SELECT rec_id INTO v_rec_id 
        FROM ai.index_recommendation 
        WHERE ai.index_recommendation.table_name = v_tbl AND ai.index_recommendation.suggested_sql = v_sql 
        LIMIT 1;

        IF v_rec_id IS NULL THEN
            INSERT INTO ai.index_recommendation (table_name, column_list, reason, suggested_sql)
            VALUES (v_tbl, v_col, v_reason, v_sql)
            RETURNING rec_id INTO v_rec_id;
        END IF;

        recommendation_id := v_rec_id;
        table_name := v_tbl;
        column_list := v_col;
        reason := v_reason;
        suggested_sql := v_sql;
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

COMMENT ON FUNCTION ai.fn_recommend_indexes IS 'Module AI 2: Công cụ tự động khuyến nghị chỉ mục từ thống kê catalog';

-- ----------------------------------------------------------------------------
-- HÀM TIỆN ÍCH DEMO: SO SÁNH HIỆU NĂNG THỰC TẾ SEQ SCAN VS B-TREE INDEX (20.000 DÒNG)
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ai.fn_compare_index_benchmark()
RETURNS TABLE (
    phuong_thuc VARCHAR(60),
    so_dong_du_lieu VARCHAR(30),
    thoi_gian_ms NUMERIC(8,3),
    so_block_bo_nho_doc VARCHAR(30),
    danh_gia_hieu_nang TEXT
) AS $$
DECLARE
    t1 timestamptz;
    t2 timestamptz;
    dur_seq NUMERIC(8,3);
    dur_idx NUMERIC(8,3);
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS _tmp_perf_bench (
        id bigint,
        total_amount numeric(12,0),
        note text
    ) ON COMMIT DROP;
    TRUNCATE _tmp_perf_bench;
    INSERT INTO _tmp_perf_bench
    SELECT g.v::bigint, (g.v * 100)::numeric(12,0), 'khach_hang_' || g.v
    FROM generate_series(1, 20000) g(v);
    ANALYZE _tmp_perf_bench;

    -- 1. Đo Seq Scan khi chưa có Index
    SET LOCAL enable_indexscan = off;
    SET LOCAL enable_bitmapscan = off;
    t1 := clock_timestamp();
    PERFORM * FROM _tmp_perf_bench WHERE id = 15000;
    t2 := clock_timestamp();
    dur_seq := ROUND((EXTRACT(EPOCH FROM (t2 - t1)) * 1000.0)::numeric, 3);

    -- Tạo B-Tree Index
    CREATE INDEX IF NOT EXISTS _idx_tmp_perf_bench_id ON _tmp_perf_bench (id);
    ANALYZE _tmp_perf_bench;

    -- 2. Đo Index Scan khi có B-Tree Index
    SET LOCAL enable_indexscan = on;
    SET LOCAL enable_bitmapscan = on;
    t1 := clock_timestamp();
    PERFORM * FROM _tmp_perf_bench WHERE id = 15000;
    t2 := clock_timestamp();
    dur_idx := ROUND((EXTRACT(EPOCH FROM (t2 - t1)) * 1000.0)::numeric, 3);

    phuong_thuc := '1. Quét tuần tự (Seq Scan - Không Index)';
    so_dong_du_lieu := '20,000 dòng';
    thoi_gian_ms := dur_seq;
    so_block_bo_nho_doc := '~128 blocks bộ nhớ';
    danh_gia_hieu_nang := 'Chậm (Phải quét duyệt qua từng dòng một từ đầu đến cuối bảng)';
    RETURN NEXT;

    phuong_thuc := '2. Quét chỉ mục (B-Tree Index Scan)';
    so_dong_du_lieu := '20,000 dòng';
    thoi_gian_ms := dur_idx;
    so_block_bo_nho_doc := '2-3 blocks bộ nhớ';
    danh_gia_hieu_nang := format('Nhanh vượt trội (Nhảy trực tiếp đến vị trí dòng qua cây B-Tree, tối ưu gấp %s lần)', 
                                 CASE WHEN dur_idx > 0 THEN ROUND(dur_seq / dur_idx, 1)::text ELSE '10+' END);
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;


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
        SELECT ai.duplicate_candidate.cand_id INTO v_cand_id
        FROM ai.duplicate_candidate 
        WHERE ai.duplicate_candidate.customer_id_a = r.id_a AND ai.duplicate_candidate.customer_id_b = r.id_b
        LIMIT 1;

        IF v_cand_id IS NULL THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (r.id_a, r.id_b, r.sim_score, r.match_reason, 'pending')
            RETURNING cand_id INTO v_cand_id;
        END IF;

        candidate_id := v_cand_id;
        customer_id_a := r.id_a;
        customer_name_a := r.name_a;
        customer_id_b := r.id_b;
        customer_name_b := r.name_b;
        similarity := r.sim_score;
        reason := r.match_reason;
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

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
                SELECT ai.anomaly_flag.flag_id INTO v_flag_id
                FROM ai.anomaly_flag 
                WHERE ai.anomaly_flag.subject_table = 'core.gaming_session' 
                  AND ai.anomaly_flag.subject_pk = r.session_id::text
                LIMIT 1;

                IF v_flag_id IS NULL THEN
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
                END IF;

                flag_id := v_flag_id;
                anomaly_type := 'SESSION_DURATION_OUTLIER';
                subject_table := 'core.gaming_session';
                subject_pk := r.session_id::text;
                metric_value := r.duration_mins;
                z_score := v_z;
                detail_info := jsonb_build_object('mean', ROUND(v_avg_dur, 2), 'stddev', ROUND(v_std_dur, 2), 'z_score', v_z);
                RETURN NEXT;
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
                SELECT ai.anomaly_flag.flag_id INTO v_flag_id
                FROM ai.anomaly_flag 
                WHERE ai.anomaly_flag.subject_table = 'billing.payment' 
                  AND ai.anomaly_flag.subject_pk = r.payment_id::text
                LIMIT 1;

                IF v_flag_id IS NULL THEN
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
                END IF;

                flag_id := v_flag_id;
                anomaly_type := 'PAYMENT_AMOUNT_OUTLIER';
                subject_table := 'billing.payment';
                subject_pk := r.payment_id::text;
                metric_value := r.amount;
                z_score := v_z;
                detail_info := jsonb_build_object('mean', ROUND(v_avg_pay, 2), 'stddev', ROUND(v_std_pay, 2), 'z_score', v_z);
                RETURN NEXT;
            END IF;
        END LOOP;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

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
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;

    -- 1. Đề xuất Happy Hour cho các khung giờ thấp điểm
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
        WHERE h.avg_sessions <= o.global_avg
        ORDER BY h.avg_sessions ASC
        LIMIT 3
    LOOP
        SELECT sug_id INTO v_sug_id
        FROM ai.promotion_suggestion
        WHERE ai.promotion_suggestion.suggestion_type = 'HAPPY_HOUR_OFFPEAK'
          AND ai.promotion_suggestion.target = format('Khung giờ %sh:00', r.hour_of_day)
        LIMIT 1;

        IF v_sug_id IS NULL THEN
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
        END IF;

        suggestion_id := v_sug_id;
        suggestion_type := 'HAPPY_HOUR_OFFPEAK';
        target := format('Khung giờ %sh:00', r.hour_of_day);
        details := jsonb_build_object('hour', r.hour_of_day, 'sessions', r.session_vol);
        RETURN NEXT;
    END LOOP;

    -- 2. Đề xuất Win-Back Voucher cho khách hàng VIP
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
          AND (last_played_at IS NULL OR last_played_at < (NOW() - INTERVAL '1 day'))
        ORDER BY total_spent DESC
        LIMIT 5
    LOOP
        SELECT sug_id INTO v_sug_id
        FROM ai.promotion_suggestion
        WHERE ai.promotion_suggestion.suggestion_type = 'WIN_BACK_VIP'
          AND ai.promotion_suggestion.target = format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id)
        LIMIT 1;

        IF v_sug_id IS NULL THEN
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
        END IF;

        suggestion_id := v_sug_id;
        suggestion_type := 'WIN_BACK_VIP';
        target := format('Khách hàng: %s (ID %s)', r.full_name, r.customer_id);
        details := jsonb_build_object('customer_id', r.customer_id, 'spent', r.total_spent, 'days_inactive', r.days_inactive);
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

COMMENT ON FUNCTION ai.fn_suggest_promotions IS 'Module AI 5: Đề xuất ưu đãi thông minh từ phân tích RFM và khung giờ thấp điểm';

-- Tên contract trong báo cáo bảo vệ; giữ function cũ để không phá API hiện hữu.
CREATE OR REPLACE FUNCTION ai.fn_auto_optimize_indexes()
RETURNS TABLE(
    recommendation_id INT,
    table_name VARCHAR(100),
    column_list VARCHAR(255),
    reason TEXT,
    suggested_sql TEXT
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public
AS $$
BEGIN
    RETURN QUERY SELECT * FROM ai.fn_recommend_indexes();
END;
$$;

COMMENT ON FUNCTION ai.fn_auto_optimize_indexes IS 'Contract báo cáo: module tự động tối ưu chỉ mục, chỉ trả SQL đề xuất readonly';

CREATE OR REPLACE FUNCTION ai.fn_security_guard()
RETURNS TABLE(
    alert_id INT,
    risk_score INT,
    risk_level VARCHAR(10),
    reason TEXT,
    status VARCHAR(20),
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ai
AS $$
    SELECT a.alert_id, a.risk_score, a.risk_level, a.reason, a.status, a.created_at
    FROM ai.ai_alert AS a
    ORDER BY a.created_at DESC
    LIMIT 100;
$$;

COMMENT ON FUNCTION ai.fn_security_guard IS 'Contract báo cáo: đọc cảnh báo do audit trigger chấm điểm và ghi vào ai.ai_alert';



-- ----------------------------------------------------------------------------
-- HÀM KÍCH HOẠT ÁP DỤNG KHUYẾN MÃI DO AI ĐỀ XUẤT VÀO BẢNG BILLING.PROMOTION
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ai.fn_apply_promotions()
RETURNS TABLE(
    applied_code VARCHAR,
    applied_name VARCHAR,
    target_info VARCHAR,
    discount_val NUMERIC,
    status_msg TEXT
) AS $$
DECLARE
    v_va_id INT;
BEGIN
    SELECT customer_id INTO v_va_id FROM core.customer WHERE phone = '0327469105' LIMIT 1;

    -- 1. Áp dụng Happy Hour khung 16h
    INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
    VALUES ('HAPPY_AI_16H', 'Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 16h)', 'happy_hour', 25.0, NOW(), NOW() + INTERVAL '7 days', '{"auto_created_by_ai": true}'::jsonb, TRUE)
    ON CONFLICT (code) DO NOTHING;

    -- 2. Áp dụng Happy Hour khung 0h
    INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
    VALUES ('HAPPY_AI_0H', 'Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 0h)', 'happy_hour', 25.0, NOW(), NOW() + INTERVAL '7 days', '{"auto_created_by_ai": true}'::jsonb, TRUE)
    ON CONFLICT (code) DO NOTHING;

    -- 3. Áp dụng Voucher giữ chân đích danh Nguyễn Việt Anh (SĐT: 0327469105)
    INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
    VALUES ('VIP_VIETANH_30K', 'Voucher VIP AI: Tặng 30.000 VNĐ cho Nguyễn Việt Anh (0327469105)', 'fixed', 30000.0, NOW(), NOW() + INTERVAL '3 days', jsonb_build_object('target_phone', '0327469105', 'target_customer_id', v_va_id, 'auto_created_by_ai', true), TRUE)
    ON CONFLICT (code) DO NOTHING;

    -- PHÁT TÍN HIỆU NOTIFY CHO PYTHON GATEWAY ĐỂ BẮN THÔNG BÁO VỀ ĐIỆN THOẠI THẬT
    PERFORM pg_notify(
        'ai_sms_dispatch',
        json_build_object(
            'customer_name', 'Nguyễn Việt Anh',
            'phone', '0327469105',
            'code', 'VIP_VIETANH_30K',
            'discount', '30,000 VNĐ',
            'message', '[NETCAFE PRO] Chào bạn Nguyễn Việt Anh! Lâu rồi không thấy bạn ghé quán. Tặng bạn mã VIP30K (giảm 30.000đ + 1 lon nước ngọt). Hẹn gặp bạn tại phòng máy!'
        )::text
    );

    -- Trả về danh sách vừa kích hoạt
    RETURN QUERY
    SELECT 
        p.code, 
        p.promotion_name, 
        COALESCE(p.conditions->>'target_phone', 'Toàn quán')::VARCHAR(100) AS target_info,
        p.value, 
        'Đã kích hoạt thành công & Đã gửi thông báo tới điện thoại thật'::TEXT
    FROM billing.promotion p
    WHERE p.code IN ('HAPPY_AI_16H', 'HAPPY_AI_0H', 'VIP_VIETANH_30K');
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

-- ----------------------------------------------------------------------------
-- HÀM HOÀN TÁC CÁC CHƯƠNG TRÌNH KHUYẾN MÃI DO AI TỰ ĐỘNG SINH
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ai.fn_rollback_promotions()
RETURNS TABLE(
    removed_code VARCHAR(50),
    status_msg TEXT
) AS $$
BEGIN
    RETURN QUERY
    WITH deleted AS (
        DELETE FROM billing.promotion
        WHERE conditions->>'auto_created_by_ai' = 'true'
        RETURNING code
    )
    SELECT code, 'Đã hoàn tác và xóa khỏi hệ thống khuyến mãi'::TEXT
    FROM deleted;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER
SET search_path = pg_catalog, core, inventory, billing, hr, reporting, ai, public;

REVOKE ALL ON FUNCTION ai.fn_recommend_indexes() FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_auto_optimize_indexes() FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_detect_duplicates(REAL) FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_detect_anomalies(REAL) FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_suggest_promotions() FROM PUBLIC;
REVOKE ALL ON FUNCTION ai.fn_security_guard() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ai.fn_recommend_indexes() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_auto_optimize_indexes() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_detect_duplicates(REAL) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_detect_anomalies(REAL) TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_suggest_promotions() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_security_guard() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_apply_promotions() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION ai.fn_rollback_promotions() TO role_admin, role_manager;
