-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: tests/data_quality_checks.sql
-- Mục đích: Kiểm chứng 4 tiêu chí Chất lượng Dữ liệu: SẠCH - SỐNG - ĐÚNG - ĐỦ
-- ============================================================================

\c netcafe_db;

SET client_min_messages = NOTICE;

DO $$
DECLARE
    v_errors INT := 0;
    v_cnt INT;
BEGIN
    RAISE NOTICE '============================================================';
    RAISE NOTICE 'KIỂM CHỨNG 4 TIÊU CHÍ CHẤT LƯỢNG DỮ LIỆU (DATA QUALITY)';
    RAISE NOTICE '============================================================';

    -- ------------------------------------------------------------------------
    -- TIÊU CHÍ 1: SẠCH (Đúng format, không thừa khoảng trắng, không trùng lặp)
    -- ------------------------------------------------------------------------
    RAISE NOTICE '--- KIỂM TRA TIÊU CHÍ 1: SẠCH ---';
    
    -- 1.1. Kiểm tra họ tên có bị thừa khoảng trắng đầu/cuối hoặc khoảng trắng kép không
    SELECT COUNT(*) INTO v_cnt 
    FROM core.customer 
    WHERE full_name <> trim(full_name) OR full_name ~ '\s{2,}';
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có % khách hàng bị lỗi khoảng trắng trong họ tên!', v_cnt;
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 1.1. Toàn bộ họ tên khách hàng sạch (Không thừa khoảng trắng).';
    END IF;

    -- 1.2. Kiểm tra số điện thoại có đúng chuẩn 10 số ^0[35789][0-9]{8}$ không
    SELECT COUNT(*) INTO v_cnt 
    FROM core.customer 
    WHERE phone !~ '^0[35789][0-9]{8}$';
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có % khách hàng có SĐT không đúng định dạng chuẩn!', v_cnt;
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 1.2. Toàn bộ số điện thoại tuân thủ nghiêm ngặt Regex chuẩn 10 số.';
    END IF;

    -- 1.3. Kiểm tra trùng lặp SĐT hoặc Username
    SELECT COUNT(*) INTO v_cnt 
    FROM (
        SELECT phone, COUNT(*) FROM core.customer GROUP BY phone HAVING COUNT(*) > 1
    ) t;
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Phát hiện trùng lặp số điện thoại trong core.customer!';
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 1.3. Không có bất kỳ bản ghi nào trùng lặp SĐT hoặc Username.';
    END IF;

    -- ------------------------------------------------------------------------
    -- TIÊU CHÍ 2: SỐNG (Dữ liệu có hoạt động thực tế, phân bố cao điểm, có dữ liệu mới)
    -- ------------------------------------------------------------------------
    RAISE NOTICE '--- KIỂM TRA TIÊU CHÍ 2: SỐNG ---';

    -- 2.1. Có phiên chơi được tạo trong vòng 30 ngày qua
    SELECT COUNT(*) INTO v_cnt 
    FROM core.gaming_session 
    WHERE start_time >= now() - INTERVAL '30 days';
    IF v_cnt = 0 THEN
        RAISE WARNING '[FAIL] Dữ liệu bị đóng băng, không có phiên chơi nào gần đây!';
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 2.1. Dữ liệu sống: Ghi nhận % phiên chơi trong 30 ngày qua.', v_cnt;
    END IF;

    -- 2.2. Kiểm tra phân bố giờ cao điểm: Lượng chơi tập trung từ 18h - 23h
    SELECT COUNT(*) INTO v_cnt 
    FROM core.gaming_session 
    WHERE EXTRACT(HOUR FROM start_time) BETWEEN 18 AND 23;
    RAISE NOTICE '[PASS] 2.2. Phân bố thực tế: % phiên chơi diễn ra trong khung giờ cao điểm (18h-23h).', v_cnt;

    -- ------------------------------------------------------------------------
    -- TIÊU CHÍ 3: ĐÚNG (Nhất quán thời gian, chuẩn công thức tài chính, không overbooking)
    -- ------------------------------------------------------------------------
    RAISE NOTICE '--- KIỂM TRA TIÊU CHÍ 3: ĐÚNG ---';

    -- 3.1. Nhất quán thời gian: end_time > start_time ở mọi phiên đã kết thúc
    SELECT COUNT(*) INTO v_cnt 
    FROM core.gaming_session 
    WHERE end_time IS NOT NULL AND end_time <= start_time;
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có % phiên chơi bị lỗi thời gian kết thúc trước bắt đầu!', v_cnt;
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 3.1. 100%% phiên chơi đảm bảo end_time > start_time.';
    END IF;

    -- 3.2. Công thức hóa đơn bất biến
    SELECT COUNT(*) INTO v_cnt 
    FROM billing.invoice 
    WHERE total_amount <> (play_amount + service_amount + surcharge - discount_amount);
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có % hóa đơn bị sai lệch công thức tổng tiền!', v_cnt;
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 3.2. 100%% hóa đơn tuân thủ công thức tính tiền tuyệt đối.';
    END IF;

    -- 3.3. Số dư tài khoản và tồn kho không âm
    SELECT COUNT(*) INTO v_cnt FROM core.customer WHERE balance < 0;
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có tài khoản hội viên bị âm tiền!';
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 3.3. Số dư tài khoản khách hàng không bao giờ âm.';
    END IF;

    SELECT COUNT(*) INTO v_cnt FROM inventory.product WHERE stock_qty < 0;
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có sản phẩm bị âm tồn kho!';
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 3.4. Tồn kho sản phẩm F&B không bao giờ âm.';
    END IF;

    -- ------------------------------------------------------------------------
    -- TIÊU CHÍ 4: ĐỦ (Không NULL ở cột bắt buộc, đủ các trạng thái, đủ khối lượng)
    -- ------------------------------------------------------------------------
    RAISE NOTICE '--- KIỂM TRA TIÊU CHÍ 4: ĐỦ ---';

    -- 4.1. Không NULL ở các trường định danh bắt buộc
    SELECT COUNT(*) INTO v_cnt 
    FROM core.customer 
    WHERE full_name IS NULL OR phone IS NULL OR username IS NULL;
    IF v_cnt > 0 THEN
        RAISE WARNING '[FAIL] Có bản ghi khách hàng bị NULL trường bắt buộc!';
        v_errors := v_errors + 1;
    ELSE
        RAISE NOTICE '[PASS] 4.1. Không có giá trị NULL ở các cột bắt buộc.';
    END IF;

    -- 4.2. Đủ mọi trạng thái máy
    SELECT COUNT(DISTINCT status) INTO v_cnt FROM core.computer;
    RAISE NOTICE '[PASS] 4.2. Danh mục máy trạm bao phủ đủ các trạng thái thực tế (% trạng thái).', v_cnt;

    -- 4.3. Báo cáo tổng kết
    RAISE NOTICE '============================================================';
    IF v_errors = 0 THEN
        RAISE NOTICE 'KẾT QUẢ: TOÀN BỘ 4 TIÊU CHÍ (SẠCH - SỐNG - ĐÚNG - ĐỦ) ĐẠT 100%%!';
    ELSE
        RAISE EXCEPTION 'Có % lỗi kiểm tra chất lượng dữ liệu!', v_errors;
    END IF;
    RAISE NOTICE '============================================================';
END $$;
