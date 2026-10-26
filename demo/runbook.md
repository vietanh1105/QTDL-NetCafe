# SỔ TAY TỔNG THỂ BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
## ĐỀ TÀI: XÂY DỰNG VÀ TỐI ƯU HÓA CƠ SỞ DỮ LIỆU CHO HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
### Kịch bản chi tiết thao tác trực tiếp trên DBeaver / pgAdmin và Terminal kết nối Neon Cloud PostgreSQL

---

## 1. BỐ TRÍ MÀN HÌNH MÁY CHIẾU (DUAL-WINDOW SETUP)
Trước khi thầy gọi nhóm lên, chuẩn bị sẵn 2 cửa sổ song song:
- **Nửa màn hình TRÁI (60%)**: Mở **DBeaver** hoặc **pgAdmin** (kết nối database `netcafe_db` trên Neon Cloud).
- **Nửa màn hình PHẢI (40%)**: Mở **PowerShell / Windows Terminal** tại thư mục gốc dự án: `c:\Users\Administrator\Documents\QTDL-NetCafe`.

---

## 2. LỘ TRÌNH 5 GIAI ĐOẠN TRÌNH DIỄN (TỔNG THỜI GIAN: 12 - 15 PHÚT)

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ BƯỚC 0: TỰ TIN XÓA SẠCH VÀ KHỞI TẠO CSDL TỪ CON SỐ 0 (Zero-to-Hero trong 4.78s)         │
│ BƯỚC 1: FAIL-SAFE DEMO (Khởi tạo CSDL + Python Data Cleaning + 3 Lỗi ĐỎ Ràng buộc)     │
│ BƯỚC 2: ROLE-PLAYING DEMO (SET ROLE Thu ngân + RLS Hóa đơn + Cấm Cột Lương + AI 3)    │
│ BƯỚC 3: PERFORMANCE DEMO (EXPLAIN ANALYZE Seq Scan -> Index Scan + AI 2 Recommender)  │
│ BƯỚC 4: INTEGRATION & AI DEMO (NoSQL JSONB ->> + Python ETL + Real-time LISTEN/NOTIFY) │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### BƯỚC 0: TRÌNH DIỄN XÓA SẠCH VÀ KHỞI TẠO TỰ ĐỘNG TỪ CON SỐ 0
*Mục đích: Chứng minh với thầy hệ thống được tự động hóa 100%, không dùng dữ liệu mock sẵn từ trước.*

* **Thao tác 0.1 (Terminal - Xóa sạch):**
  ```powershell
  python python/reset_and_init_cloud.py --wipe
  ```
  *(Hoặc chạy file `demo/00_wipe_cloud_database.sql` trên DBeaver).*
  - **DBeaver:** Bấm chuột phải vào Database chọn **Refresh (F5)** $\rightarrow$ Toàn bộ bảng biến mất, số bảng = 0.
  - **Nói với thầy:** *"Dạ thưa Thầy, hiện tại CSDL trên Neon Cloud đang hoàn toàn trống trơn (0 bảng). Bây giờ nhóm sẽ khởi tạo toàn bộ CSDL và nạp dữ liệu mồi bằng script tự động hóa."*

* **Thao tác 0.2 (Terminal - Khởi tạo toàn bộ):**
  ```powershell
  python python/reset_and_init_cloud.py --init
  ```
  *(Hoặc mở file `sql/netcafe_cloud_init.sql` trên DBeaver bấm Alt + X).*
  - **Terminal:** Chỉ mất đúng **4.78 giây** để chạy qua 12 file migration, tạo thành công 8 Schemas và 35 Bảng.
  - **DBeaver:** Bấm **Refresh (F5)** $\rightarrow$ Toàn bộ 8 Schemas và 35 Bảng lập tức xuất hiện đầy đủ!

---

### PHẦN 1: DEMO KHỞI TẠO VÀ RÀNG BUỘC DỮ LIỆU (FAIL-SAFE DEMO)
📁 **File SQL:** `demo/demo_1_init_constraints.sql`  
🐍 **File Python:** `python/generate_dirty_data.py` & `python/clean_data.py`

#### 1.1. Chứng minh cấu trúc 8 Schemas độc lập đạt chuẩn 3NF
* **Thao tác DBeaver:**
  ```sql
  SELECT table_schema, count(table_name) AS total_tables
  FROM information_schema.tables
  WHERE table_schema IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging')
  GROUP BY table_schema
  ORDER BY table_schema;
  ```
* **Nói với thầy:** *"Hệ thống được thiết kế phân rã thành 8 schemas nghiệp vụ chuyên biệt (`core`, `inventory`, `billing`, `hr`, `audit`, `ai`, `reporting`, `staging`) với 35 bảng đạt chuẩn 3NF."*

#### 1.2. Sinh dữ liệu bẩn bằng Python và chuẩn hóa dữ liệu (Data Quality Pipeline)
* **Thao tác Terminal:**
  ```powershell
  # Bước 1: Sinh 500 bản ghi bẩn vào bảng Staging
  python python/generate_dirty_data.py
  ```
* **Thao tác DBeaver:** Xem dữ liệu bẩn thô tại bảng Staging:
  ```sql
  SELECT raw_id, raw_full_name, raw_phone, raw_balance, raw_email, source_batch 
  FROM staging.stg_customers 
  ORDER BY raw_id 
  LIMIT 10;
  ```
  👉 **Chỉ chuột vào 6 dòng đầu tiên cho Thầy xem:**
  - **Dòng 1:** SĐT dính mã quốc tế `+84...` (`+84539958838`).
  - **Dòng 2:** SĐT dính dấu chấm phân cách `0339.345.092`.
  - **Dòng 3:** Họ tên viết hoa toàn bộ và thụt lề nhiều khoảng trắng thừa `   PHAN HẢI VI   `.
  - **Dòng 4:** Số dư âm vô lý `-50000`.
  - **Dòng 5:** SĐT cụt chỉ có 5 số `12345` (không thể cứu).
  - **Dòng 6:** Email hỏng sai cấu trúc `sai-email-khong-hop-le`.

* **Thao tác Terminal:**
  ```powershell
  # Bước 2: Chạy Python ETL làm sạch dữ liệu
  python python/clean_data.py
  ```
  *(Terminal in bảng thống kê: Xử lý 500 dòng $\rightarrow$ Chuẩn hóa nạp 474 dòng sạch vào `core.customer`, cách ly 26 dòng lỗi nặng vào `staging.rejected_rows`).*

* **Thao tác DBeaver:** Cho thầy xem bảng cách ly dữ liệu lỗi:
  ```sql
  SELECT rejected_id, source_table, error_reason, raw_record 
  FROM staging.rejected_rows 
  ORDER BY rejected_id 
  LIMIT 10;
  ```
* **Nói với thầy:** *"Mô hình Staging $\rightarrow$ Cleaning $\rightarrow$ Rejected Rows là kiến trúc Data Lakehouse thực tế, giúp không làm ô nhiễm bảng chính `core.customer` mà vẫn lưu vết lý do lỗi để đối soát."*

#### 1.3. Thử nghiệm rủi ro (Bắt buộc) — Xuất hiện thông báo LỖI ĐỎ của PostgreSQL
* **Thao tác DBeaver:** Bôi đen và chạy lần lượt 3 câu lệnh trần trụi:

  1. **Thử nhập sai định dạng SĐT (CHECK `ck_customer_phone`):**
     ```sql
     INSERT INTO core.customer (full_name, phone, username, password_hash)
     VALUES ('Trần Văn Sai SĐT', '12345', 'user_bad_phone_demo', 'hash');
     ```
     🔴 *PostgreSQL văng lỗi ĐỎ:* `ERROR: new row for relation "customer" violates check constraint "ck_customer_phone"`

  2. **Thử cập nhật số tiền âm (CHECK `ck_customer_balance`):**
     ```sql
     UPDATE core.customer SET balance = -50000 WHERE customer_id = 1;
     ```
     🔴 *PostgreSQL văng lỗi ĐỎ:* `ERROR: new row for relation "customer" violates check constraint "ck_customer_balance"`

  3. **Thử xóa dữ liệu cha đang bị con tham chiếu (FK `RESTRICT`):**
     ```sql
     DELETE FROM core.room WHERE room_id = 1;
     ```
     🔴 *PostgreSQL văng lỗi ĐỎ:* `ERROR: update or delete on table "room" violates RESTRICT setting of foreign key constraint "computer_room_id_fkey" on table "computer"`

* **Nói với thầy:** *"Các ràng buộc này nằm trực tiếp tại Storage Engine của PostgreSQL. Dù client hay hacker có cố tình bypass giao diện để gửi query sai thì Database Engine vẫn lập tức chặn đứng, rollback giao dịch và bắn lỗi màu đỏ, bảo vệ dữ liệu toàn vẹn 100%."*

---

### PHẦN 2: DEMO BẢO MẬT VÀ RLS (ROLE-PLAYING DEMO)
📁 **File SQL:** `demo/demo_2_security_rls.sql`

#### 2.1. Đóng vai nhân viên thu ngân Thảo (SET ROLE)
* **Thao tác DBeaver:**
  ```sql
  SET ROLE nv_thao;
  SELECT current_user, session_user;
  ```

#### 2.2. Chứng minh Row-Level Security (RLS) hoạt động
* **Thao tác DBeaver:**
  ```sql
  -- Thu ngân Thảo xem danh sách hóa đơn:
  SELECT invoice_id, customer_id, cashier_id, total_amount, status FROM billing.invoice;
  ```
  *(Toàn bộ các dòng trả về đều có `cashier_id = 3` — RLS tự động lọc chỉ trả về hóa đơn do chính Thảo lập).*
* **Thao tác DBeaver:** Đối chiếu với vai trò Quản lý:
  ```sql
  RESET ROLE;
  SET ROLE nv_manager;
  SELECT cashier_id, count(*) AS total_invoices FROM billing.invoice GROUP BY cashier_id;
  RESET ROLE;
  ```
  *(Quản lý nhìn thấy toàn bộ hóa đơn của tất cả nhân viên).*

#### 2.3. Chứng minh Column-Level Security (Chặn cột Lương nhạy cảm)
* **Thao tác DBeaver:**
  ```sql
  SET ROLE nv_thao;

  -- Xem thông tin nhân sự công khai (HỢP LỆ):
  SELECT employee_id, full_name, username, emp_role FROM hr.employee;

  -- Cố tình truy vấn cột LƯƠNG (salary):
  SELECT employee_id, full_name, salary FROM hr.employee;
  ```
  🔴 *PostgreSQL văng lỗi ĐỎ:* `ERROR: permission denied for column "salary" of relation "employee"` (Mã lỗi 42501).
* **Thao tác DBeaver:**
  ```sql
  RESET ROLE;
  ```

#### 2.4. Thử nghiệm Module AI đã tích hợp trong phần Bảo mật
* **Thao tác DBeaver:**
  ```sql
  -- Module AI 3: Thuật toán so khớp chuỗi Trigram + unaccent phát hiện tài khoản trùng lặp/gian lận
  SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason 
  FROM ai.fn_detect_duplicates(0.65) LIMIT 5;
  ```
* **Nói với thầy:** *"Thuật toán so khớp chuỗi mờ chạy 100% nội bộ trong PostgreSQL qua extension `pg_trgm`, nhận diện ngay các cặp tài khoản gần giống tên và số điện thoại để chống gian lận nạp khuyến mãi."*

---

### PHẦN 3: DEMO TỐI ƯU HÓA HIỆU NĂNG (PERFORMANCE DEMO)
📁 **File SQL:** `demo/demo_3_performance.sql`

#### 3.1. Truy vấn tìm kiếm phức tạp trước khi có Index (Seq Scan)
* **Thao tác DBeaver:**
  ```sql
  -- Xóa index để giả lập trạng thái nguyên bản:
  DROP INDEX IF EXISTS core.idx_customer_fullname_trgm;

  -- Chạy EXPLAIN (ANALYZE, BUFFERS) tìm kiếm mờ ký tự đại diện ở đầu: '%Thành%'
  EXPLAIN (ANALYZE, BUFFERS)
  SELECT customer_id, full_name, phone FROM core.customer 
  WHERE full_name ILIKE '%Thành%';
  ```
  👉 **Chỉ chuột vào dòng:** **`Seq Scan on customer`** (Quét tuần tự toàn bộ từng block dữ liệu trên đĩa).

#### 3.2. Tạo Index chuyên dụng và chứng minh tăng tốc (Index Scan)
* **Thao tác DBeaver:**
  ```sql
  -- B-Tree thông thường bất lực với '%'. Ta tạo chỉ mục chuyên dụng GIN Trigram:
  CREATE INDEX idx_customer_fullname_trgm 
  ON core.customer USING gin (full_name gin_trgm_ops);

  -- Chạy lại câu truy vấn chính xác như trên:
  EXPLAIN (ANALYZE, BUFFERS)
  SELECT customer_id, full_name, phone FROM core.customer 
  WHERE full_name ILIKE '%Thành%';
  ```
  👉 **Chỉ chuột vào dòng:** **`Bitmap Index Scan on idx_customer_fullname_trgm`** (Tốc độ tăng tốc vượt bậc, chỉ đọc đúng các con trỏ dòng khớp Trigram).

#### 3.3. Thử nghiệm Module AI tích hợp trong phần Hiệu năng
* **Thao tác DBeaver:**
  ```sql
  -- Module AI 2: In-Database AI tự động phân tích Catalog pg_stat_user_tables và khuyến nghị Index
  SELECT * FROM ai.fn_recommend_indexes();
  ```
* **Nói với thầy:** *"Hàm AI tự động truy vấn catalog thống kê của PostgreSQL, phát hiện các bảng có tỷ lệ Seq Scan cao và tự động đề xuất câu lệnh CREATE INDEX cụ thể."*

---

### PHẦN 4: DEMO ĐIỂM NHẤN TÍCH HỢP DỮ LIỆU HOẶC AI (INTEGRATION/AI DEMO)
📁 **File SQL:** `demo/demo_4_highlights.sql`  
🐍 **File Python:** `python/listen_ai_alerts.py`

#### 4.1. Truy vấn bóc tách dữ liệu bán cấu trúc NoSQL từ cột JSONB (`->>`)
* **Thao tác DBeaver:**
  ```sql
  SELECT 
      computer_code, 
      computer_name,
      specs ->> 'cpu'     AS cpu_model,
      specs ->> 'gpu'     AS graphics_card,
      specs ->> 'ram'     AS memory_size,
      specs ->> 'monitor' AS display_panel
  FROM core.computer
  WHERE specs ->> 'gpu' ILIKE '%RTX%';
  ```
* **Nói với thầy:** *"Cột `specs` sử dụng kiểu dữ liệu JSONB kết hợp toán tử `->>` và `@>`, cho phép lưu trữ cấu hình phần cứng linh hoạt như NoSQL MongoDB nhưng vẫn bảo đảm tính toàn vẹn quan hệ ACID của PostgreSQL."*

#### 4.2. (ĐIỂM CỘNG XUẤT SẮC): Luồng Python lắng nghe LISTEN/NOTIFY kết hợp Trigger Cảnh báo AI
* **Bước 1 (Terminal bên Phải):** Bật tiến trình Python lắng nghe thời gian thực:
  ```powershell
  python python/listen_ai_alerts.py
  ```
  *(Terminal hiển thị: `[+] ĐÃ KÍCH HOẠT LẮNG NGHE KÊNH: 'ai_security_alert'` và chờ tín hiệu).*
* **Bước 2 (DBeaver bên Trái):** Chạy câu lệnh giả lập nạp số tiền lớn bất thường (10 triệu VNĐ):
  ```sql
  INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)
  VALUES ('topup', 1, 'cash', 10000000, 3, 'Demo nạp tiền lớn kích hoạt Trigger AI');
  ```
* **Bước 3 (Quan sát Terminal bên Phải):** Ngay khi bạn bấm Run trên DBeaver, cửa sổ Terminal Python **LẬP TỨC NỔ CẢNH BÁO THỜI GIAN THỰC**:
  ```
  [🚨 CẢNH BÁO AN NINH PHÁT HIỆN LÚC ...]
  - Kênh (Channel)     : ai_security_alert
  - Mức độ rủi ro      : [HIGH] (Score: 70/100)
  - Bảng bị tác động   : payment
  - Nguyên nhân AI     : Thanh toán/nạp tiền lớn bất thường: 10,000,000 VNĐ
  ```
* **Nói với thầy:** *"Hệ thống sử dụng kiến trúc Real-Time Push Notification trực tiếp từ PostgreSQL Engine qua socket mạng `LISTEN/NOTIFY`. Khi có hành vi bất thường, Trigger nội bộ của CSDL tự động tính điểm rủi ro và bắn cảnh báo ra ứng dụng ngay lập tức mà không cần client phải tốn tài nguyên Polling định kỳ."*

---

## 3. BỘ CÂU HỎI PHẢN BIỆN "VÌ SAO" DÀNH CHO GIẢNG VIÊN VÀ CÂU TRẢ LỜI ĂN ĐIỂM 10

### Câu 1: Vì sao lại chia thành 8 Schemas độc lập mà không gom chung vào schema `public`?
> **Trả lời:** *"Dạ thưa Thầy, việc phân chia 8 schemas nghiệp vụ chuyên biệt (`core`, `inventory`, `billing`, `hr`, `audit`, `ai`, `reporting`, `staging`) giúp phân quyền đặc quyền tối thiểu (Least Privilege). Nhân viên thu ngân chỉ được SELECT trên `billing` và `core`, hoàn toàn bị cô lập khỏi `hr` và `audit`. Đồng thời việc này giúp quản trị vòng đời dữ liệu (backup, partition, maintenance) trên từng schema riêng biệt mà không ảnh hưởng tới toàn bộ hệ thống."*

### Câu 2: Vì sao lại dùng `NUMERIC(12,0)` cho số tiền thay vì `BIGINT` hay `FLOAT`?
> **Trả lời:** *"Dạ thưa Thầy, `FLOAT/DOUBLE` có sai số làm tròn dấu phẩy động chuẩn IEEE 754 nên tuyệt đối cấm kỵ trong tài chính kế toán. `NUMERIC(12,0)` trong PostgreSQL là số học dấu chấm tĩnh (Fixed-Point) chính xác tuyệt đối từng đơn vị đồng, thang đo 12 chữ số đủ biểu diễn số tiền tới hàng trăm tỷ VNĐ mà không tốn dung lượng thừa."*

### Câu 3: Thuật toán AI trong đề tài này có gọi API bên ngoài (như OpenAI / ChatGPT) không?
> **Trả lời:** *"Dạ thưa Thầy, đề tài tuân thủ 100% nguyên tắc **In-Database Intelligence**: Toàn bộ 5 module AI chạy nội bộ bằng toán học, thống kê và hàm PL/pgSQL có sẵn của PostgreSQL (Z-score phát hiện ngoại lai, khoảng cách Trigram so khớp chuỗi, phân tích thống kê `pg_stat_user_tables`). Hệ thống không gọi API bên ngoài, bảo mật dữ liệu tuyệt đối và hoạt động độc lập không cần Internet."*

### Câu 4: Vì sao chọn GIN Trigram Index cho họ tên thay vì B-Tree thông thường?
> **Trả lời:** *"Dạ thưa Thầy, chỉ mục B-Tree chỉ có tác dụng khi tìm kiếm chính xác hoặc tiền tố (`'Nguyễn%'`). Khi người dùng tìm kiếm chuỗi con có ký tự đại diện ở đầu như `'%Thành%'`, B-Tree bị vô hiệu hóa hoàn toàn và PostgreSQL buộc phải quét tuần tự (Seq Scan). GIN Trigram tách chuỗi thành các bộ 3 ký tự (3-grams), cho phép Index Scan tăng tốc hàng chục lần ngay cả khi tìm kiếm mờ hoặc tìm ở giữa chuỗi."*
