# BẢNG MA TRẬN RÀNG BUỘC TOÀN VẸN VÀ CHẤT LƯỢNG DỮ LIỆU

> **Môn học:** Quản trị Cơ sở Dữ liệu (QTDL)  
> **Hệ thống:** Phòng Net / Game Cafe (`netcafe_db`)  
> **Triết lý thiết kế:** Bảo đảm 4 tiêu chí chất lượng dữ liệu: **SẠCH - SỐNG - ĐÚNG - ĐỦ** ngay tại tầng CSDL.

---

## 1. MA TRẬN RÀNG BUỘC TOÀN VẸN CHI TIẾT

| Tên bảng | Cột áp dụng | Loại ràng buộc | Biểu thức SQL / Định nghĩa | Mục đích nghiệp vụ | Ví dụ lỗi khi vi phạm |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `hr.employee` | `employee_id` | **PK (Identity)** | `GENERATED ALWAYS AS IDENTITY PRIMARY KEY` | Định danh duy nhất nhân viên | Chèn trùng hoặc ép giá trị ID |
| `hr.employee` | `username` | **UNIQUE** | `CONSTRAINT uq_employee_username UNIQUE (username)` | Tránh trùng tài khoản đăng nhập | Chèn 2 nhân viên cùng username |
| `hr.employee` | `username` | **CHECK (Regex)** | `CHECK (username ~ '^[a-z][a-z0-9_]{3,29}$')` | Chuẩn hóa tài khoản chữ thường 4-30 ký tự | Chèn username bắt đầu bằng số `123admin` |
| `hr.employee` | `phone` | **CHECK (Regex)** | `CHECK (phone ~ '^0[35789][0-9]{8}$')` | Chuẩn hóa SĐT di động Việt Nam 10 chữ số | Chèn SĐT quá ngắn `12345` hoặc đầu số `01...` |
| `hr.employee` | `salary` | **CHECK** | `CHECK (salary >= 0)` | Lương không được âm | Nhập lương `-5000000` |
| `hr.employee` | `emp_role` | **CHECK** | `CHECK (emp_role IN ('admin','manager','cashier','staff','technician'))` | Giới hạn 5 vai trò hệ thống hợp lệ | Gán quyền không xác định `super_user` |
| `hr.employee` | `default_shift` | **CHECK** | `CHECK (default_shift IN ('morning','afternoon','night'))` | Chuẩn hóa 3 ca làm việc | Gán ca `midnight` |
| `core.room` | `room_name` | **UNIQUE** | `CONSTRAINT uq_room_name UNIQUE (room_name)` | Không trùng tên khu vực phòng | Tạo 2 phòng cùng tên "VIP 1" |
| `core.computer_type` | `hourly_rate` | **CHECK** | `CHECK (hourly_rate >= 0)` | Đơn giá thuê giờ không được âm | Nhập giá giờ `-10000` |
| `core.computer` | `computer_code` | **CHECK (Regex)** | `CHECK (computer_code ~ '^PC[0-9]{3,4}$')` | Chuẩn hóa mã máy trạm: PCxxx hoặc PCxxxx | Nhập mã `SERVER01` hoặc `PC1` |
| `core.computer` | `ip_address` | **UNIQUE + Type** | `ip_address INET UNIQUE` | Đảm bảo tính duy nhất và đúng chuẩn IP | Nhập IP sai `999.999.1.1` hoặc trùng IP |
| `core.computer` | `mac_address` | **UNIQUE + Type** | `mac_address MACADDR UNIQUE` | Đảm bảo đúng định dạng MAC card mạng | Nhập MAC sai format hex |
| `core.computer` | `status` | **CHECK** | `CHECK (status IN ('available','in_use','maintenance','broken'))` | Kiểm soát 4 trạng thái vòng đời máy | Gán trạng thái lạ `sleeping` |
| `core.computer` | `room_id`, `type_id` | **FK (RESTRICT)** | `REFERENCES core.room ON DELETE RESTRICT` | Chống xóa phòng/loại máy đang chứa máy | Xóa phòng "VIP 1" khi còn PC001 |
| `core.customer` | `phone` | **CHECK (Regex)** | `CHECK (phone ~ '^0[35789][0-9]{8}$')` | Chuẩn hóa số điện thoại hội viên | Nhập `0912345` hoặc `+84912345678` |
| `core.customer` | `email` | **CHECK (Regex)** | `CHECK (email IS NULL OR email ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')` | Chuẩn hóa định dạng email quốc tế | Nhập `abc@com` hoặc `abc@.vn` |
| `core.customer` | `username` | **CHECK (Regex)** | `CHECK (username ~ '^[a-z][a-z0-9_]{3,29}$')` | Chuẩn hóa tên đăng nhập hội viên | Nhập username hoa hoặc ký tự có dấu |
| `core.customer` | `balance` | **CHECK** | `CHECK (balance >= 0)` | Tài khoản trả trước không được âm | Trừ tiền vượt số dư hiện có |
| `core.customer` | `membership_tier` | **CHECK** | `CHECK (membership_tier IN ('regular','silver','gold','vip'))` | Kiểm soát 4 hạng thành viên | Gán hạng `diamond` |
| `core.gaming_session` | `start_time`, `end_time` | **CHECK** | `CHECK (end_time IS NULL OR end_time > start_time)` | Thời gian kết thúc phải sau bắt đầu | `end_time = '08:00'` trong khi `start_time = '09:00'` |
| `core.gaming_session` | `status`, `end_time` | **CHECK** | `CHECK ((status = 'active' AND end_time IS NULL) OR (status <> 'active' AND end_time IS NOT NULL))` | Trạng thái đồng nhất với mốc thời gian | Phiên `finished` nhưng `end_time` vẫn là `NULL` |
| `core.booking` | `booked_from`, `booked_to` | **CHECK** | `CHECK (booked_to > booked_from)` | Thời gian kết thúc đặt sau bắt đầu | Đặt lịch lùi thời gian |
| `core.booking` | `computer_id`, `tstzrange` | **EXCLUDE (GiST)** | `EXCLUDE USING gist (computer_id WITH =, tstzrange(booked_from, booked_to) WITH &&) WHERE (status IN ('pending','confirmed'))` | **Chống đặt trùng máy (Overbooking)** | Đặt cùng máy PC001 từ 14:00-18:00 và 16:00-20:00 |
| `inventory.product` | `stock_qty` | **CHECK** | `CHECK (stock_qty >= 0)` | Tồn kho không âm, chặn bán lố kho | Bán hàng khi số lượng trong kho = 0 |
| `inventory.product` | `unit_price` | **CHECK** | `CHECK (unit_price >= 0)` | Đơn giá sản phẩm F&B không âm | Đặt giá `-15000` |
| `inventory.food_order_detail`| `quantity` | **CHECK** | `CHECK (quantity > 0)` | Số lượng gọi món phải lớn hơn 0 | Gọi 0 ly nước hoặc số lượng âm |
| `inventory.food_order_detail`| `(order_id, product_id)`| **Composite PK** | `PRIMARY KEY (order_id, product_id)` | Mỗi món chỉ xuất hiện 1 dòng trong 1 đơn | Thêm 2 dòng cùng 1 món trong 1 đơn |
| `billing.promotion` | `valid_from`, `valid_to` | **CHECK** | `CHECK (valid_to >= valid_from)` | Thời hạn khuyến mãi hợp lệ | `valid_to` nằm trước `valid_from` |
| `billing.invoice` | `session_id` | **UNIQUE + FK** | `session_id BIGINT UNIQUE REFERENCES core.gaming_session` | **Quan hệ 1-1**: Một phiên chỉ có duy nhất 1 hóa đơn | Tạo 2 hóa đơn cho cùng một phiên chơi |
| `billing.invoice` | `total_amount` | **CHECK (Công thức)** | `CHECK (total_amount = play_amount + service_amount + surcharge - discount_amount)` | Đảm bảo tính toán doanh thu bất biến | Tiền tổng bị lệch so với chi tiết |
| `billing.payment` | `amount` | **CHECK** | `CHECK (amount > 0)` | Số tiền giao dịch phải dương | Giao dịch nạp hoặc trả bằng 0 đồng |
| `billing.payment` | `kind`, `invoice_id` | **CHECK** | `CHECK ((kind = 'invoice' AND invoice_id IS NOT NULL) OR (kind = 'topup' AND invoice_id IS NULL))` | Ràng buộc đa hình giao dịch sổ quỹ | Giao dịch trả tiền hóa đơn nhưng không gắn `invoice_id` |
| `hr.maintenance` | `started_at`, `finished_at`| **CHECK** | `CHECK (finished_at IS NULL OR finished_at >= started_at)` | Giờ hoàn tất sau giờ bắt đầu sửa | Giờ sửa xong trước giờ báo lỗi |
| `ai.duplicate_candidate` | `customer_id_a`, `_b` | **CHECK** | `CHECK (customer_id_a < customer_id_b)` | Chống đảo chiều cặp khách trùng (A,B) và (B,A) | Chèn cặp (2, 1) khi đã có (1, 2) |
| `ai.ai_alert` | `risk_score` | **CHECK** | `CHECK (risk_score BETWEEN 0 AND 100)` | Điểm rủi ro nằm trong thang điểm 0-100 | Điểm rủi ro vượt ngưỡng `150` |

---

## 2. ĐỊNH NGHĨA 4 TIÊU CHÍ CHẤT LƯỢNG DỮ LIỆU (SẠCH - SỐNG - ĐÚNG - ĐỦ)

1. **SẠCH (Cleanliness):**
   - Không chứa khoảng trắng thừa ở đầu/cuối (được xử lý qua `TRIM()`).
   - Chuẩn hóa chữ hoa/chữ thường: Email luôn viết thường (`LOWER()`), Tên người viết hoa chữ cái đầu (`Title Case`).
   - Chuẩn hóa định dạng số điện thoại về dạng duy nhất `0xxxxxxxxx` (10 chữ số), loại bỏ mã quốc tế `+84` hay dấu chấm/gạch ngang.
   - Không trùng lặp tài khoản: Số điện thoại, Email, Username là duy nhất (`UNIQUE`).

2. **SỐNG (Liveness):**
   - Dữ liệu phản ánh hoạt động thực tế theo thời gian thực: có các phiên máy đang chơi (`status = 'active'`).
   - Phân bố theo hành vi người dùng thực: Giờ cao điểm (18h - 23h), cuối tuần có mật độ chơi và gọi món tăng đột biến.
   - Có dữ liệu mới nhất ở thời điểm hiện tại (`now()`), không bị đóng băng trong quá khứ.

3. **ĐÚNG (Correctness & Consistency):**
   - Tính nhất quán thời gian: `end_time > start_time`, `finished_at >= started_at`, `booked_to > booked_from`.
   - Tính nhất quán tài chính: Công thức hóa đơn luôn thỏa mãn `total_amount = play_amount + service_amount + surcharge - discount_amount`.
   - Tính toàn vẹn phi mâu thuẫn: Không bao giờ có 2 phiên chơi hoặc 2 lịch đặt máy chồng lấn giờ trên cùng 1 máy trạm (`EXCLUDE USING gist`).
   - Tính toàn vẹn số học: Số dư tài khoản $\ge 0$, tồn kho $\ge 0$, số lượng mua $> 0$, giá tiền $\ge 0$.

4. **ĐỦ (Completeness):**
   - Không khuyết thiếu (`NOT NULL`) ở tất cả các trường dữ liệu định danh và nghiệp vụ bắt buộc.
   - Đủ mọi trạng thái nghiệp vụ trong thực tế: phòng máy có cả máy `available`, `in_use`, `maintenance`, `broken`.
   - Đủ khối lượng bản ghi cho bài toán tối ưu hóa (quy mô triệu dòng) để chứng minh sự khác biệt rõ rệt của Index khi chạy `EXPLAIN (ANALYZE, BUFFERS)`.
