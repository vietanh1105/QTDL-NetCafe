-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: tests/verify.sql
-- Mục đích: Kiểm thử toàn diện Ràng buộc toàn vẹn, Kiểu dữ liệu, Regex và EXCLUDE
-- Phương pháp: Sử dụng khối DO $$ ... BEGIN ... EXCEPTION để bắt lỗi chính xác,
--              in thông báo PASS/FAIL và không làm dừng tiến trình kiểm thử.
-- ============================================================================

\c netcafe_db;

SET client_min_messages = NOTICE;

DO $$
DECLARE
    v_test_count INT := 0;
    v_pass_count INT := 0;
    v_fail_count INT := 0;

    v_emp_id INT;
    v_room_id INT;
    v_type_id INT;
    v_comp_id INT;
    v_cust_id INT;
    v_cat_id INT;
    v_prod_id INT;
    v_sess_id BIGINT;
    v_inv_id BIGINT;
    v_order_id BIGINT;
BEGIN
    RAISE NOTICE '============================================================';
    RAISE NOTICE 'BẮT ĐẦU KIỂM THỬ RÀNG BUỘC TOÀN VẸN (SCHEMA CONSTRAINTS)';
    RAISE NOTICE '============================================================';

    -- Dọn dẹp dữ liệu cũ để test có thể chạy lại độc lập nhiều lần
    EXECUTE 'TRUNCATE hr.employee, core.room, core.computer_type, core.computer, core.customer, 
                      core.gaming_session, inventory.product_category, inventory.product,
                      inventory.food_order, inventory.food_order_detail, billing.promotion, 
                      billing.invoice, billing.invoice_promotion, billing.payment, hr.maintenance CASCADE';

    -- ------------------------------------------------------------------------
    -- BƯỚC 1: DỮ LIỆU CƠ BẢN HỢP LỆ (HAPPY PATH)
    -- ------------------------------------------------------------------------
    INSERT INTO hr.employee (full_name, phone, username, db_username, password_hash, emp_role, default_shift, salary)
    VALUES ('Nguyen Van An', '0981234567', 'an_nguyen', 'nv_an', crypt('Demo@123', gen_salt('bf')), 'admin', 'morning', 15000000)
    RETURNING employee_id INTO v_emp_id;

    INSERT INTO core.room (room_name, description)
    VALUES ('Phòng VIP 1', 'Khu máy cấu hình cao hút thuốc riêng')
    RETURNING room_id INTO v_room_id;

    INSERT INTO core.computer_type (type_name, hourly_rate)
    VALUES ('VIP RTX 4070', 15000)
    RETURNING type_id INTO v_type_id;

    INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, specs, ip_address, mac_address, status)
    VALUES ('PC001', 'Máy VIP 01', v_room_id, v_type_id, '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070"}'::jsonb, '192.168.1.101', '00:1A:2B:3C:4D:5E', 'available')
    RETURNING computer_id INTO v_comp_id;

    INSERT INTO core.customer (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier)
    VALUES ('Tran Van Binh', '0912345678', 'binh.tran@gmail.com', 'binh_tran', crypt('Pass@123', gen_salt('bf')), 50000, 100, 'silver')
    RETURNING customer_id INTO v_cust_id;

    INSERT INTO inventory.product_category (category_name)
    VALUES ('Nước giải khát')
    RETURNING category_id INTO v_cat_id;

    INSERT INTO inventory.product (sku, product_name, category_id, unit_price, stock_qty)
    VALUES ('STING-DO', 'Sting dâu 330ml', v_cat_id, 12000, 50)
    RETURNING product_id INTO v_prod_id;

    RAISE NOTICE '-> [HAPPY PATH] Khởi tạo dữ liệu mồi hợp lệ thành công.';

    -- ------------------------------------------------------------------------
    -- TEST 1: Regex Số điện thoại sai định dạng (Quá ngắn, sai đầu số)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.customer (full_name, phone, email, username, password_hash)
        VALUES ('Test Phone Error', '12345', 'p1@test.com', 'cust_err_1', 'dummy');
        RAISE WARNING '[FAIL] Test 1: SĐT "12345" phải bị chặn bởi ck_customer_phone!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 1: Chặn thành công SĐT sai định dạng (SQLSTATE: % - %)', SQLSTATE, SQLERRM;
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 2: Regex Số điện thoại đầu số cũ không hợp lệ (ví dụ đầu 01...)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.customer (full_name, phone, email, username, password_hash)
        VALUES ('Test Phone 01', '0123456789', 'p2@test.com', 'cust_err_2', 'dummy');
        RAISE WARNING '[FAIL] Test 2: SĐT "0123456789" (đầu số cũ) phải bị chặn!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 2: Chặn thành công SĐT có đầu số không thuộc ^0[35789]';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 3: Regex Email sai định dạng (Thiếu tên miền cấp cao)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.customer (full_name, phone, email, username, password_hash)
        VALUES ('Test Email Error', '0931112233', 'sai-email-khong-co-domain', 'cust_err_3', 'dummy');
        RAISE WARNING '[FAIL] Test 3: Email sai format phải bị chặn bởi ck_customer_email!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 3: Chặn thành công Email sai cấu trúc';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 4: Regex Mã máy vi phạm định dạng (^PC[0-9]{3,4}$)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address)
        VALUES ('SERVER01', 'Máy Server', v_room_id, v_type_id, '192.168.1.200', '00:1A:2B:3C:4D:99');
        RAISE WARNING '[FAIL] Test 4: Mã máy "SERVER01" không đúng chuẩn PCxxx phải bị chặn!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 4: Chặn thành công mã máy không đúng định dạng ^PC[0-9]{3,4}$';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 5: Regex Username sai định dạng (Bắt đầu bằng chữ số hoặc chứa ký tự đặc biệt)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.customer (full_name, phone, email, username, password_hash)
        VALUES ('Test User Error', '0932223344', 'u5@test.com', '123user_sai', 'dummy');
        RAISE WARNING '[FAIL] Test 5: Username "123user_sai" bắt đầu bằng số phải bị chặn!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 5: Chặn thành công username không bắt đầu bằng chữ cái thường';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 6: Số dư tài khoản âm (balance < 0)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.customer (full_name, phone, email, username, password_hash, balance)
        VALUES ('Test Negative Balance', '0933334455', 'u6@test.com', 'user_negative', 'dummy', -50000);
        RAISE WARNING '[FAIL] Test 6: Số dư âm -50000 phải bị chặn bởi ck_customer_balance!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 6: Chặn thành công số dư tài khoản âm';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 7: Đơn giá giờ chơi âm (hourly_rate < 0)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.computer_type (type_name, hourly_rate)
        VALUES ('Loại giá âm', -10000);
        RAISE WARNING '[FAIL] Test 7: Giá giờ âm phải bị chặn bởi ck_computer_type_hourly_rate!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 7: Chặn thành công giá giờ chơi âm';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 8: Số lượng tồn kho âm (stock_qty < 0)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO inventory.product (sku, product_name, category_id, unit_price, stock_qty)
        VALUES ('AM-KHO', 'Sản phẩm âm kho', v_cat_id, 10000, -5);
        RAISE WARNING '[FAIL] Test 8: Tồn kho âm phải bị chặn bởi ck_product_stock_qty!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 8: Chặn thành công tồn kho âm';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 9: Số lượng mua trong đơn hàng bằng 0 hoặc âm (quantity <= 0)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, hourly_rate_applied)
        VALUES (v_cust_id, v_comp_id, v_emp_id, 15000)
        RETURNING session_id INTO v_sess_id;

        INSERT INTO inventory.food_order (session_id, employee_id)
        VALUES (v_sess_id, v_emp_id)
        RETURNING order_id INTO v_order_id;

        INSERT INTO inventory.food_order_detail (order_id, product_id, quantity, unit_price)
        VALUES (v_order_id, v_prod_id, 0, 12000);
        RAISE WARNING '[FAIL] Test 9: Quantity = 0 phải bị chặn bởi ck_order_detail_quantity!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 9: Chặn thành công chi tiết món có số lượng = 0';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 10: Xóa phòng còn máy - Ràng buộc Khóa ngoại ON DELETE RESTRICT
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        DELETE FROM core.room WHERE room_id = v_room_id;
        RAISE WARNING '[FAIL] Test 10: Xóa phòng đang chứa máy phải bị chặn bởi ON DELETE RESTRICT!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN foreign_key_violation THEN
        RAISE NOTICE '[PASS] Test 10: Chặn thành công xóa dữ liệu cha đang được tham chiếu (RESTRICT)';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 11: Trùng lặp Khóa Unique (Trùng SĐT hoặc Username)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.customer (full_name, phone, email, username, password_hash)
        VALUES ('Trung Lap Phone', '0912345678', 'other@gmail.com', 'user_khac', 'dummy');
        RAISE WARNING '[FAIL] Test 11: Trùng SĐT 0912345678 phải bị chặn bởi uq_customer_phone!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN unique_violation THEN
        RAISE NOTICE '[PASS] Test 11: Chặn thành công chèn bản ghi trùng lặp UNIQUE SĐT';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 12: Thời gian kết thúc phiên trước thời gian bắt đầu (end_time < start_time)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied)
        VALUES (v_cust_id, v_comp_id, v_emp_id, now(), now() - INTERVAL '1 hour', 'finished', 15000);
        RAISE WARNING '[FAIL] Test 12: end_time < start_time phải bị chặn bởi ck_session_time_order!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 12: Chặn thành công phiên chơi có end_time < start_time';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 13: Phiên kết thúc nhưng end_time là NULL hoặc active mà có end_time
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied)
        VALUES (v_cust_id, v_comp_id, v_emp_id, now(), NULL, 'finished', 15000);
        RAISE WARNING '[FAIL] Test 13: status=finished nhưng end_time=NULL phải bị chặn!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 13: Chặn thành công mâu thuẫn giữa status phiên và end_time';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 14: Công thức tổng tiền hóa đơn không khớp (total <> play + service + surcharge - discount)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        -- Tạo phiên finished để lập hóa đơn
        INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied, play_amount)
        VALUES (v_cust_id, v_comp_id, v_emp_id, now() - INTERVAL '2 hours', now(), 'finished', 15000, 30000)
        RETURNING session_id INTO v_sess_id;

        -- Hóa đơn sai: play=30000, service=12000, surcharge=0, discount=5000 -> Tổng phải là 37000 nhưng cố tình nhập 50000
        INSERT INTO billing.invoice (session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount)
        VALUES (v_sess_id, v_cust_id, v_emp_id, 30000, 12000, 0, 5000, 50000);

        RAISE WARNING '[FAIL] Test 14: Sai công thức hóa đơn phải bị chặn bởi ck_invoice_formula!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 14: Chặn thành công sai lệch công thức tổng tiền hóa đơn';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 15: Ràng buộc đa hình của bảng Payment (kind='invoice' nhưng invoice_id là NULL)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        INSERT INTO billing.payment (kind, invoice_id, customer_id, method, amount, employee_id)
        VALUES ('invoice', NULL, v_cust_id, 'cash', 50000, v_emp_id);

        RAISE WARNING '[FAIL] Test 15: kind=invoice mà invoice_id=NULL phải bị chặn!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE '[PASS] Test 15: Chặn thành công giao dịch hóa đơn thiếu invoice_id';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- TEST 16: Ràng buộc quan hệ 1-1 của Invoice (Hai hóa đơn trỏ về cùng một session_id)
    -- ------------------------------------------------------------------------
    v_test_count := v_test_count + 1;
    BEGIN
        -- Tạo 1 phiên finished riêng cho Test 16
        INSERT INTO core.gaming_session (customer_id, computer_id, opened_by, start_time, end_time, status, hourly_rate_applied, play_amount)
        VALUES (v_cust_id, v_comp_id, v_emp_id, now() - INTERVAL '1 hour', now(), 'finished', 15000, 15000)
        RETURNING session_id INTO v_sess_id;

        -- Tạo 1 hóa đơn hợp lệ cho session vừa rồi
        INSERT INTO billing.invoice (session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount)
        VALUES (v_sess_id, v_cust_id, v_emp_id, 15000, 0, 0, 0, 15000)
        RETURNING invoice_id INTO v_inv_id;

        -- Cố tình tạo hóa đơn thứ 2 cho cùng session_id
        INSERT INTO billing.invoice (session_id, customer_id, cashier_id, play_amount, service_amount, surcharge, discount_amount, total_amount)
        VALUES (v_sess_id, v_cust_id, v_emp_id, 15000, 0, 0, 0, 15000);

        RAISE WARNING '[FAIL] Test 16: Tạo 2 hóa đơn cho cùng 1 phiên phải bị chặn bởi uq_invoice_session!';
        v_fail_count := v_fail_count + 1;
    EXCEPTION WHEN unique_violation THEN
        RAISE NOTICE '[PASS] Test 16: Khóa UNIQUE session_id bảo đảm quan hệ 1-1 giữa phiên chơi và hóa đơn';
        v_pass_count := v_pass_count + 1;
    END;

    -- ------------------------------------------------------------------------
    -- BÁO CÁO TỔNG KẾT
    -- ------------------------------------------------------------------------
    RAISE NOTICE '============================================================';
    RAISE NOTICE 'KẾT QUẢ KIỂM THỬ: TỔNG SỐ TEST: %, ĐẠT: %, THẤT BẠI: %', v_test_count, v_pass_count, v_fail_count;
    RAISE NOTICE '============================================================';

    IF v_fail_count > 0 THEN
        RAISE EXCEPTION 'Có % bài kiểm thử thất bại!', v_fail_count;
    END IF;
END $$;
