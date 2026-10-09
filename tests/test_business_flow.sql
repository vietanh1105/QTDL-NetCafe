-- Kiểm tra luồng nghiệp vụ: Mở phiên -> Gọi món -> Đóng phiên & Thanh toán
\c netcafe_db;

DO $$
DECLARE
    v_sess_id BIGINT;
    v_order_id BIGINT;
    v_inv_id BIGINT;
    v_status VARCHAR;
    v_stock INT;
    v_bal NUMERIC;
BEGIN
    -- 1. Mở phiên
    v_sess_id := core.sp_open_session(1, 1, 1);
    SELECT status INTO v_status FROM core.computer WHERE computer_id = 1;
    RAISE NOTICE 'Đã mở phiên #%, Trạng thái máy PC001: % (Mong đợi: in_use)', v_sess_id, v_status;

    -- 2. Gọi 2 chai Sting
    v_order_id := inventory.sp_place_order(v_sess_id, 1, 1, 2);
    SELECT stock_qty INTO v_stock FROM inventory.product WHERE product_id = 1;
    RAISE NOTICE 'Đã gọi món, Đơn #%, Tồn kho Sting còn: % (Mong đợi: 98)', v_order_id, v_stock;

    -- 3. Đóng phiên và thanh toán bằng số dư tài khoản
    v_inv_id := billing.sp_close_session_and_pay(v_sess_id, 1, 'balance');
    SELECT status INTO v_status FROM core.computer WHERE computer_id = 1;
    SELECT balance INTO v_bal FROM core.customer WHERE customer_id = 1;
    RAISE NOTICE 'Đã đóng phiên và tạo Hóa đơn #%, Trạng thái máy PC001: % (Mong đợi: available), Số dư khách còn: % VNĐ', v_inv_id, v_status, v_bal;
END $$;
