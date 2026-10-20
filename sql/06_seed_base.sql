-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 06_seed_base.sql
-- Mục đích: Dữ liệu mồi nhỏ thuần SQL (20 máy, phòng, menu F&B, nhân viên, khách hàng)
--           Phục vụ demo trực tiếp trên pgAdmin và phần mềm desktop không phụ thuộc Python
-- ============================================================================

\c netcafe_db;

-- 1. XÓA DỮ LIỆU CŨ VÀ RESET IDENTITY SEQUENCES
TRUNCATE hr.maintenance, billing.payment, billing.invoice_promotion, billing.invoice,
         inventory.food_order_detail, inventory.food_order,
         core.gaming_session, inventory.product, inventory.product_category,
         core.customer, core.computer, core.computer_type, core.room,
         hr.employee, billing.promotion RESTART IDENTITY CASCADE;

-- 2. NHÂN SỰ VÀ TÀI KHOẢN HỆ THỐNG (ĐỦ 5 VAI TRÒ)
INSERT INTO hr.employee (full_name, phone, username, db_username, password_hash, emp_role, default_shift, salary, hired_date)
VALUES 
    ('Nguyễn Văn An', '0981234567', 'an_nguyen', 'nv_admin', crypt('Demo@123', gen_salt('bf')), 'admin', 'morning', 20000000, '2024-01-15'),
    ('Lê Quang Minh', '0972345678', 'minh_le', 'nv_manager', crypt('Demo@123', gen_salt('bf')), 'manager', 'morning', 15000000, '2024-03-01'),
    ('Phạm Thu Thảo', '0913456789', 'thao_pham', 'nv_thao', crypt('Demo@123', gen_salt('bf')), 'cashier', 'morning', 8500000, '2024-06-10'),
    ('Trần Đức Nam', '0934567890', 'nam_tran', 'nv_nam', crypt('Demo@123', gen_salt('bf')), 'cashier', 'afternoon', 8500000, '2024-06-15'),
    ('Vũ Anh Tuấn', '0965678901', 'tuan_vu', 'nv_tuan', crypt('Demo@123', gen_salt('bf')), 'staff', 'afternoon', 7000000, '2024-08-01'),
    ('Trần Mạnh Hùng', '0986789012', 'hung_tran', 'nv_hung', crypt('Demo@123', gen_salt('bf')), 'technician', 'morning', 11000000, '2024-04-20');

-- 3. KHU VỰC VÀ PHÒNG MÁY
INSERT INTO core.room (room_name, description)
VALUES 
    ('Khu Máy Lạnh', 'Không gian thoáng mát cấm hút thuốc, ghế công thái học'),
    ('Khu Hút Thuốc', 'Khu vực có hệ thống hút mùi riêng biệt'),
    ('Phòng VIP Thi Đấu', 'Khu máy cấu hình cực cao, màn hình 240Hz, tai nghe chuyên game');

-- 4. LOẠI MÁY VÀ GIÁ THUÊ/GIỜ (CHUẨN HÓA 3NF)
INSERT INTO core.computer_type (type_name, hourly_rate)
VALUES 
    ('Tiêu Chuẩn', 8000),      -- Core i5, GTX 1660 Super
    ('Nâng Cao', 12000),       -- Core i5 13th, RTX 3060
    ('VIP Thi Đấu', 18000);    -- Core i7 13th, RTX 4070

-- 5. DANH SÁCH 20 MÁY TRẠM (PC001 -> PC020)
-- 8 máy Tiêu chuẩn (Khu hút thuốc)
INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address, specs, status)
SELECT 
    v.code, v.name, r.room_id, ct.type_id, v.ip::inet, v.mac::macaddr, v.specs::jsonb, 'available'
FROM (VALUES 
    ('PC001', 'Máy 01', '192.168.1.101', '00:1A:2B:3C:4D:01', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC002', 'Máy 02', '192.168.1.102', '00:1A:2B:3C:4D:02', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC003', 'Máy 03', '192.168.1.103', '00:1A:2B:3C:4D:03', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC004', 'Máy 04', '192.168.1.104', '00:1A:2B:3C:4D:04', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC005', 'Máy 05', '192.168.1.105', '00:1A:2B:3C:4D:05', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC006', 'Máy 06', '192.168.1.106', '00:1A:2B:3C:4D:06', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC007', 'Máy 07', '192.168.1.107', '00:1A:2B:3C:4D:07', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}'),
    ('PC008', 'Máy 08', '192.168.1.108', '00:1A:2B:3C:4D:08', '{"cpu":"i5-10400F","ram":"16GB","gpu":"GTX 1660S"}')
) AS v(code, name, ip, mac, specs)
CROSS JOIN (SELECT room_id FROM core.room WHERE room_name = 'Khu Hút Thuốc') r
CROSS JOIN (SELECT type_id FROM core.computer_type WHERE type_name = 'Tiêu Chuẩn') ct;

-- 7 máy Nâng cao (Khu máy lạnh)
INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address, specs, status)
SELECT 
    v.code, v.name, r.room_id, ct.type_id, v.ip::inet, v.mac::macaddr, v.specs::jsonb, 'available'
FROM (VALUES 
    ('PC009', 'Máy 09', '192.168.1.109', '00:1A:2B:3C:4D:09', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC010', 'Máy 10', '192.168.1.110', '00:1A:2B:3C:4D:10', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC011', 'Máy 11', '192.168.1.111', '00:1A:2B:3C:4D:11', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC012', 'Máy 12', '192.168.1.112', '00:1A:2B:3C:4D:12', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC013', 'Máy 13', '192.168.1.113', '00:1A:2B:3C:4D:13', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC014', 'Máy 14', '192.168.1.114', '00:1A:2B:3C:4D:14', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}'),
    ('PC015', 'Máy 15', '192.168.1.115', '00:1A:2B:3C:4D:15', '{"cpu":"i5-13400F","ram":"16GB","gpu":"RTX 3060"}')
) AS v(code, name, ip, mac, specs)
CROSS JOIN (SELECT room_id FROM core.room WHERE room_name = 'Khu Máy Lạnh') r
CROSS JOIN (SELECT type_id FROM core.computer_type WHERE type_name = 'Nâng Cao') ct;

-- 5 máy VIP Thi đấu (Phòng VIP)
INSERT INTO core.computer (computer_code, computer_name, room_id, type_id, ip_address, mac_address, specs, status)
SELECT 
    v.code, v.name, r.room_id, ct.type_id, v.ip::inet, v.mac::macaddr, v.specs::jsonb, 'available'
FROM (VALUES 
    ('PC016', 'Máy VIP 16', '192.168.1.116', '00:1A:2B:3C:4D:16', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC017', 'Máy VIP 17', '192.168.1.117', '00:1A:2B:3C:4D:17', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC018', 'Máy VIP 18', '192.168.1.118', '00:1A:2B:3C:4D:18', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC019', 'Máy VIP 19', '192.168.1.119', '00:1A:2B:3C:4D:19', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}'),
    ('PC020', 'Máy VIP 20', '192.168.1.120', '00:1A:2B:3C:4D:20', '{"cpu":"i7-13700F","ram":"32GB","gpu":"RTX 4070","hz":240}')
) AS v(code, name, ip, mac, specs)
CROSS JOIN (SELECT room_id FROM core.room WHERE room_name = 'Phòng VIP Thi Đấu') r
CROSS JOIN (SELECT type_id FROM core.computer_type WHERE type_name = 'VIP Thi Đấu') ct;

-- 6. DANH MỤC VÀ SẢN PHẨM F&B
INSERT INTO inventory.product_category (category_name)
VALUES 
    ('Đồ Uống Pha Chế'),
    ('Nước Ngọt Đóng Lon'),
    ('Đồ Ăn Nhanh'),
    ('Snack & Ăn Vặt');

-- Thêm sản phẩm qua danh mục
INSERT INTO inventory.product (sku, product_name, category_id, unit_price, stock_qty, attributes)
SELECT v.sku, v.name, cat.category_id, v.price, v.qty, v.attr::jsonb
FROM (VALUES 
    ('STING-RED', 'Sting Dâu Đỏ 330ml', 'Nước Ngọt Đóng Lon', 12000, 100, '{"size":"lon","cold":true}'),
    ('REDBULL', 'Bò Húc RedBull Thái', 'Nước Ngọt Đóng Lon', 18000, 80, '{"size":"lon","cold":true}'),
    ('COCA-ZERO', 'Coca Cola Zero Calo', 'Nước Ngọt Đóng Lon', 12000, 60, '{"size":"lon","cold":true}'),
    ('AQUA-500', 'Nước Khoáng Aquafina 500ml', 'Nước Ngọt Đóng Lon', 8000, 120, '{"size":"chai"}'),
    ('CF-SUA-DA', 'Cà Phê Sữa Đá', 'Đồ Uống Pha Chế', 18000, 200, '{"type":"pha_che"}'),
    ('TRA-DAO', 'Trà Đào Cam Sả', 'Đồ Uống Pha Chế', 22000, 150, '{"type":"pha_che"}'),
    ('MI-TRUNG', 'Mì Tôm Trứng Xúc Xích', 'Đồ Ăn Nhanh', 25000, 75, '{"type":"do_an_nong"}'),
    ('COM-XAO-BO', 'Cơm Rang Dưa Bò', 'Đồ Ăn Nhanh', 40000, 40, '{"type":"do_an_nong"}'),
    ('BANH-MI-PATE', 'Bánh Mì Pate Chả Lụa', 'Đồ Ăn Nhanh', 20000, 50, '{"type":"do_an_nhe"}'),
    ('BIM-BIM-KHOAI', 'Snack Khoai Tây Lays', 'Snack & Ăn Vặt', 15000, 85, '{"flavor":"rong_bien"}')
) AS v(sku, name, cat_name, price, qty, attr)
JOIN inventory.product_category cat ON v.cat_name = cat.category_name;

-- 7. CHƯƠNG TRÌNH KHUYẾN MÃI
INSERT INTO billing.promotion (code, promotion_name, promo_type, value, valid_from, valid_to, conditions, is_active)
VALUES 
    ('HAPPY22', 'Khung Giờ Vàng Đêm (22h - 6h)', 'happy_hour', 20.0, '2026-01-01 00:00:00+07', '2027-01-01 00:00:00+07', '{"time_window":"22:00-06:00","discount_percent":20}'::jsonb, TRUE),
    ('GOLD10', 'Ưu Đãi Hội Viên Hạng Gold', 'percent', 10.0, '2026-01-01 00:00:00+07', '2027-01-01 00:00:00+07', '{"required_tier":"gold"}'::jsonb, TRUE),
    ('CHAOHAY26', 'Mừng Năm Mới 2026', 'fixed', 15000.0, '2026-01-01 00:00:00+07', '2027-01-01 00:00:00+07', '{"min_bill":50000}'::jsonb, TRUE);

-- 8. DANH SÁCH KHÁCH HÀNG MẪU (20 KHÁCH CÓ SỐ DƯ TÀI KHOẢN)
INSERT INTO core.customer (full_name, phone, email, username, password_hash, balance, loyalty_points, membership_tier)
VALUES 
    ('Trần Văn Bình', '0912345678', 'binh.tran@gmail.com', 'binh_tran', crypt('Pass@123', gen_salt('bf')), 80000, 120, 'silver'),
    ('Hoàng Đức Anh', '0983456789', 'anh.hoang@gmail.com', 'anh_hoang', crypt('Pass@123', gen_salt('bf')), 150000, 650, 'gold'),
    ('Đặng Thùy Dung', '0974567890', 'dung.dang@gmail.com', 'dung_dang', crypt('Pass@123', gen_salt('bf')), 250000, 2200, 'vip'),
    ('Nguyễn Hải Đăng', '0935678901', 'dang.hai@gmail.com', 'dang_hai', crypt('Pass@123', gen_salt('bf')), 50000, 45, 'regular'),
    ('Lê Bảo Châu', '0966789012', 'chau.le@gmail.com', 'chau_le', crypt('Pass@123', gen_salt('bf')), 120000, 520, 'gold'),
    ('Phan Thanh Tùng', '0987890123', 'tung.phan@gmail.com', 'tung_phan', crypt('Pass@123', gen_salt('bf')), 30000, 20, 'regular'),
    ('Võ Quốc Huy', '0918901234', 'huy.vo@gmail.com', 'huy_vo', crypt('Pass@123', gen_salt('bf')), 90000, 180, 'silver'),
    ('Bùi Minh Quân', '0939012345', 'quan.bui@gmail.com', 'quan_bui', crypt('Pass@123', gen_salt('bf')), 300000, 2800, 'vip'),
    ('Đỗ Gia Hân', '0970123456', 'han.do@gmail.com', 'han_do', crypt('Pass@123', gen_salt('bf')), 60000, 80, 'regular'),
    ('Dương Tuấn Kiệt', '0981122334', 'kiet.duong@gmail.com', 'kiet_duong', crypt('Pass@123', gen_salt('bf')), 110000, 310, 'silver');

-- 9. LỊCH SỬ BẢO TRÌ MẪU (CHO DEMO RLS VÀ MODULE BẢO TRÌ)
INSERT INTO hr.maintenance (computer_id, technician_id, issue_description, parts_replaced, cost, status, started_at, finished_at)
VALUES 
    (1, 6, 'Thay quạt tản nhiệt CPU kêu to', '{"fan":"CoolerMaster 120mm"}'::jsonb, 150000, 'fixed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    (2, 1, 'Kiểm tra nguồn máy tính sập nguồn', '{"psu":"Corsair 650W"}'::jsonb, 500000, 'in_progress', NOW() - INTERVAL '3 hours', NULL);

-- 10. LÀM MỚI MATERIALIZED VIEWS
REFRESH MATERIALIZED VIEW reporting.mv_monthly_revenue;
REFRESH MATERIALIZED VIEW reporting.mv_top_customers;
REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
