-- ============================================================================
-- NETCAFE MANAGEMENT SYSTEM - TÌNH HUỐNG MẪU CHO AI CENTER (GIAN LẬN & TRÙNG LẶP)
-- ============================================================================

DO $$
DECLARE
    v_audit_id BIGINT;
    v_log_time TIMESTAMPTZ;
    v_cust_a INT;
    v_cust_b INT;
    v_cust_c INT;
    v_cust_d INT;
BEGIN
    -- 1. TÌNH HUỐNG TRÙNG LẶP HỘI VIÊN (DUPLICATE CANDIDATES)
    -- Thêm tài khoản trùng lặp 1: Trần Văn Bình (trùng tên và 7 số cuối SĐT 2345678)
    IF NOT EXISTS (SELECT 1 FROM core.customer WHERE username = 'binh_tran_pro') THEN
        INSERT INTO core.customer (
            full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier
        ) VALUES (
            'Trần Văn Bình', '0812345678', 'binh.tran.pro@gmail.com', 'binh_tran_pro',
            crypt('Pass@123', gen_salt('bf')), 25000, 30, 'regular'
        );
    END IF;

    -- Thêm tài khoản trùng lặp 2: Nguyễn Hải Đăng (trùng tên và đuôi SĐT với ID 4)
    IF NOT EXISTS (SELECT 1 FROM core.customer WHERE username = 'haidang_gaming') THEN
        INSERT INTO core.customer (
            full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier
        ) VALUES (
            'Nguyễn Hải Đăng', '0835678901', 'haidang.gaming@gmail.com', 'haidang_gaming',
            crypt('Pass@123', gen_salt('bf')), 15000, 10, 'regular'
        );
    END IF;

    SELECT customer_id INTO v_cust_a FROM core.customer WHERE username = 'binh_tran' LIMIT 1;
    SELECT customer_id INTO v_cust_b FROM core.customer WHERE username = 'binh_tran_pro' LIMIT 1;
    SELECT customer_id INTO v_cust_c FROM core.customer WHERE username = 'dang_hai' LIMIT 1;
    SELECT customer_id INTO v_cust_d FROM core.customer WHERE username = 'haidang_gaming' LIMIT 1;

    -- Nạp cặp trùng lặp A & B vào ai.duplicate_candidate nếu chưa có
    IF v_cust_a IS NOT NULL AND v_cust_b IS NOT NULL AND v_cust_a < v_cust_b THEN
        IF NOT EXISTS (
            SELECT 1 FROM ai.duplicate_candidate
            WHERE customer_id_a = v_cust_a AND customer_id_b = v_cust_b
        ) THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (
                v_cust_a, v_cust_b, 1.000,
                'Trùng hoàn toàn họ tên và số điện thoại liên hệ',
                'pending'
            );
        END IF;
    END IF;

    -- Nạp cặp trùng lặp C & D vào ai.duplicate_candidate nếu chưa có
    IF v_cust_c IS NOT NULL AND v_cust_d IS NOT NULL AND v_cust_c < v_cust_d THEN
        IF NOT EXISTS (
            SELECT 1 FROM ai.duplicate_candidate
            WHERE customer_id_a = v_cust_c AND customer_id_b = v_cust_d
        ) THEN
            INSERT INTO ai.duplicate_candidate (customer_id_a, customer_id_b, similarity, reason, status)
            VALUES (
                v_cust_c, v_cust_d, 0.950,
                'Trùng khớp họ tên và giống 4 số cuối điện thoại liên hệ',
                'pending'
            );
        END IF;
    END IF;

    -- 2. TÌNH HUỐNG CẢNH BÁO AN NINH & GIAN LẬN (SECURITY ALERTS)
    -- Tình huống 1: Hủy hóa đơn đã xuất (VOID)
    IF NOT EXISTS (
        SELECT 1 FROM ai.ai_alert
        WHERE reason LIKE '%Hóa đơn #104%'
    ) THEN
        v_log_time := clock_timestamp() - INTERVAL '2 hours';
        INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
        VALUES (
            v_log_time, 'nv_thao', 'billing.invoice', 'UPDATE', '104',
            '{"status":"paid","total_amount":350000}'::jsonb,
            '{"status":"void","total_amount":350000}'::jsonb
        )
        RETURNING audit_id INTO v_audit_id;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (
            v_audit_id, v_log_time, 75, 'high',
            'Hủy hóa đơn đã xuất VOID: Hóa đơn #104 trị giá 350.000 VNĐ bị nhân viên nv_thao hủy sau khi khách rời máy',
            'new'
        );
    END IF;

    -- Tình huống 2: Nạp tiền ngoài giờ bất thường
    IF NOT EXISTS (
        SELECT 1 FROM ai.ai_alert
        WHERE reason LIKE '%Nạp 2.000.000 VNĐ%'
    ) THEN
        v_log_time := clock_timestamp() - INTERVAL '1 day' + INTERVAL '2 hours 45 minutes';
        INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
        VALUES (
            v_log_time, 'nv_thao', 'billing.payment', 'INSERT', '512',
            NULL,
            '{"amount":2000000,"customer_id":1}'::jsonb
        )
        RETURNING audit_id INTO v_audit_id;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (
            v_audit_id, v_log_time, 65, 'medium',
            'Điều chỉnh số dư ví khách hàng bất thường ngoài giờ: Nạp 2.000.000 VNĐ vào tài khoản binh_tran lúc 02:45 sáng',
            'new'
        );
    END IF;

    -- Tình huống 3: Can thiệp sửa giờ chơi
    IF NOT EXISTS (
        SELECT 1 FROM ai.ai_alert
        WHERE reason LIKE '%VIP-02%'
    ) THEN
        v_log_time := clock_timestamp() - INTERVAL '2 days';
        INSERT INTO audit.audit_log (logged_at, actor, table_name, action, row_pk, old_data, new_data)
        VALUES (
            v_log_time, 'nv_hung', 'core.gaming_session', 'UPDATE', '88',
            '{"total_amount":120000}'::jsonb,
            '{"total_amount":40000}'::jsonb
        )
        RETURNING audit_id INTO v_audit_id;

        INSERT INTO ai.ai_alert (audit_id, audit_logged_at, risk_score, risk_level, reason, status)
        VALUES (
            v_audit_id, v_log_time, 85, 'high',
            'Can thiệp nhật ký phiên chơi: Phiên chơi máy VIP-02 bị chỉnh sửa giờ chơi giảm tiền thanh toán',
            'reviewed'
        );
    END IF;
END $$;

-- 3. Cập nhật khuyến mãi tự động từ số liệu hiện hữu
SELECT count(*) FROM ai.fn_suggest_promotions();
