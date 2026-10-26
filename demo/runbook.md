# KỊCH BẢN TỔNG THỂ BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
## ĐỀ TÀI: QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
### Hướng dẫn thao tác trực tiếp trên DBeaver / pgAdmin và Terminal kết nối PostgreSQL

---

## 1. BỐ TRÍ MÀN HÌNH (DUAL-WINDOW SETUP)
Trước khi thầy gọi lên trình bày, bố trí 2 cửa sổ trên màn hình máy chiếu:
- **Cửa sổ TRÁI (60%)**: DBeaver / pgAdmin (kết nối database `netcafe_db` trên Neon Cloud).
- **Cửa sổ PHẢI (40%)**: PowerShell / Windows Terminal tại thư mục gốc dự án.

---

## 2. TIẾN TRÌNH 4 BƯỚC DEMO THEO ĐÚNG YÊU CẦU GIẢNG VIÊN

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. FAIL-SAFE DEMO (Khởi tạo CSDL + Python Data Cleaning + Lỗi ĐỎ rủi ro)     │
│ 2. ROLE-PLAYING DEMO (SET ROLE nv_thao + RLS Invoice + Cấm cột Lương + AI)  │
│ 3. PERFORMANCE DEMO (EXPLAIN ANALYZE Seq Scan -> Index Scan + AI Recommender)│
│ 4. INTEGRATION & AI DEMO (NoSQL JSONB ->> + Python ETL + LISTEN/NOTIFY AI)   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### PHẦN 1: DEMO KHỞI TẠO VÀ RÀNG BUỘC DỮ LIỆU (FAIL-SAFE DEMO)
📁 **File SQL:** `demo/demo_1_init_constraints.sql`  
🐍 **File Python:** `python/generate_dirty_data.py` & `python/clean_data.py`

#### 1.1. Chứng minh Khởi tạo toàn bộ Database, Schemas, Tables & Mock Data
- **Thuyết minh:** *"Dạ thưa Thầy, toàn bộ CSDL đã được đóng gói trong 1 script khởi tạo duy nhất `sql/netcafe_full.sql`. Hệ thống được phân rã thành 8 Schemas độc lập đạt chuẩn 3NF."*
- **Thao tác DBeaver:** Chạy kiểm tra 8 schemas:
  ```sql
  SELECT table_schema, count(table_name) AS total_tables
  FROM information_schema.tables
  WHERE table_schema IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging')
  GROUP BY table_schema;
  ```

#### 1.2. Sinh dữ liệu giả lập chưa chuẩn bằng Python & Chuẩn hóa dữ liệu
- **Thao tác Terminal:**
  ```powershell
  # Bước 1: Sinh 500 dòng dữ liệu bẩn (~15% lỗi: +84, chấm, khoảng trắng, số âm)
  python python/generate_dirty_data.py
  ```
- **Thao tác DBeaver:** Xem dữ liệu bẩn trong bảng staging:
  ```sql
  SELECT raw_id, raw_full_name, raw_phone, raw_balance, source_batch 
  FROM staging.stg_customers LIMIT 10;
  ```
- **Thao tác Terminal:**
  ```powershell
  # Bước 2: Chạy Python làm sạch và nạp lại vào bảng chính
  python python/clean_data.py
  ```
  *(Terminal hiển thị báo cáo: Xử lý 500 dòng -> nạp 476 dòng sạch vào `core.customer`, cách ly 24 dòng lỗi nặng vào `staging.rejected_rows`).*
- **Thao tác DBeaver:** Chứng minh bảng cách ly:
  ```sql
  SELECT rejected_id, source_table, error_reason, raw_record 
  FROM staging.rejected_rows LIMIT 10;
  ```

#### 1.3. Thử nghiệm rủi ro (Bắt buộc) — Xuất hiện thông báo LỖI ĐỎ của PostgreSQL
- **Thao tác DBeaver:** Chạy lần lượt 3 câu lệnh trần trụi:
  1. **Vi phạm SĐT:**
     ```sql
     INSERT INTO core.customer (full_name, phone, username, password_hash)
     VALUES ('Trần Văn Lỗi', '12345', 'user_bad_phone_demo', 'hash');
     ```
     🔴 *PostgreSQL bắn lỗi đỏ:* `ERROR: check constraint "ck_customer_phone"`
  2. **Vi phạm Số âm:**
     ```sql
     UPDATE core.customer SET balance = -50000 WHERE customer_id = 1;
     ```
     🔴 *PostgreSQL bắn lỗi đỏ:* `ERROR: check constraint "ck_customer_balance"`
  3. **Vi phạm Xóa cha bị RESTRICT:**
     ```sql
     DELETE FROM core.room WHERE room_id = 1;
     ```
     🔴 *PostgreSQL bắn lỗi đỏ:* `ERROR: foreign key constraint "fk_computer_room" (RESTRICT)`
- **Thuyết minh:** *"Ràng buộc được thực thi trực tiếp tại Storage Engine của PostgreSQL nên ngăn chặn 100% việc nhập sai dữ liệu ngay từ tầng vật lý."*

---

### PHẦN 2: DEMO BẢO MẬT VÀ RLS (ROLE-PLAYING DEMO)
📁 **File SQL:** `demo/demo_2_security_rls.sql`

#### 2.1. Đóng vai nhân viên cấp thấp (SET ROLE) & Kiểm tra RLS
- **Thao tác DBeaver:**
  ```sql
  SET ROLE nv_thao; -- Thu ngân Thảo (employee_id = 3)
  
  -- RLS tự động lọc chỉ trả về hóa đơn do Thảo lập:
  SELECT invoice_id, customer_id, cashier_id, total_amount, status 
  FROM billing.invoice;
  ```
  *(Toàn bộ các dòng trả về đều có `cashier_id = 3`, tuyệt đối không thấy hóa đơn của thu ngân khác).*

#### 2.2. Kiểm tra Column-Level Privileges (Chặn cột nhạy cảm)
- **Thao tác DBeaver:**
  ```sql
  -- Xem danh sách nhân viên công khai (HỢP LỆ):
  SELECT employee_id, full_name, username, emp_role FROM hr.employee;
  
  -- Cố tình truy vấn cột lương (salary):
  SELECT employee_id, full_name, salary FROM hr.employee;
  ```
  🔴 *PostgreSQL bắn lỗi đỏ:* `ERROR: permission denied for column "salary" of relation "employee"` (Mã lỗi 42501).

#### 2.3. Thử nghiệm Module AI đã tích hợp
- **Thao tác DBeaver:**
  ```sql
  RESET ROLE;
  -- Module AI 3: Phát hiện tài khoản khách hàng gian lận tạo nhiều nick hưởng khuyến mãi
  SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason 
  FROM ai.fn_detect_duplicates(0.65) LIMIT 5;
  ```

---

### PHẦN 3: DEMO TỐI ƯU HÓA HIỆU NĂNG (PERFORMANCE DEMO)
📁 **File SQL:** `demo/demo_3_performance.sql`

#### 3.1. Truy vấn tìm kiếm phức tạp trước khi có Index (Seq Scan)
- **Thao tác DBeaver:**
  ```sql
  DROP INDEX IF EXISTS core.idx_customer_fullname_trgm;
  
  EXPLAIN (ANALYZE, BUFFERS)
  SELECT customer_id, full_name, phone FROM core.customer 
  WHERE full_name ILIKE '%Thành%';
  ```
  *(Chỉ cho thầy dòng: `Seq Scan on customer` và thời gian thực thi).*

#### 3.2. Tạo Index và đo lại hiệu năng (Index Scan)
- **Thao tác DBeaver:**
  ```sql
  CREATE INDEX idx_customer_fullname_trgm 
  ON core.customer USING gin (full_name gin_trgm_ops);
  
  EXPLAIN (ANALYZE, BUFFERS)
  SELECT customer_id, full_name, phone FROM core.customer 
  WHERE full_name ILIKE '%Thành%';
  ```
  *(Chỉ cho thầy dòng: `Bitmap Index Scan on idx_customer_fullname_trgm` - Tốc độ tăng tốc vượt bậc).*

#### 3.3. Thử nghiệm Module AI tích hợp
- **Thao tác DBeaver:**
  ```sql
  -- Module AI 2: In-Database AI tự động phân tích Catalog và khuyến nghị tạo Index
  SELECT * FROM ai.fn_recommend_indexes();
  ```

---

### PHẦN 4: DEMO ĐIỂM NHẤN TÍCH HỢP DỮ LIỆU HOẶC AI (INTEGRATION/AI DEMO)
📁 **File SQL:** `demo/demo_4_highlights.sql`  
🐍 **File Python:** `python/listen_ai_alerts.py`

#### 4.1. Bóc tách dữ liệu bán cấu trúc NoSQL từ cột JSONB (`->>`)
- **Thao tác DBeaver:**
  ```sql
  SELECT 
      computer_code, 
      computer_name,
      specs ->> 'cpu' AS cpu_model,
      specs ->> 'gpu' AS graphics_card,
      specs ->> 'ram' AS memory_size
  FROM core.computer
  WHERE specs ->> 'gpu' ILIKE '%RTX%';
  ```

#### 4.2. (ĐIỂM CỘNG XUẤT SẮC): Luồng Python lắng nghe LISTEN/NOTIFY với Trigger Cảnh báo AI
- **Bước 1 (Terminal):** Khởi động luồng lắng nghe thời gian thực:
  ```powershell
  python python/listen_ai_alerts.py
  ```
  *(Terminal thông báo: `[+] ĐÃ KÍCH HOẠT LẮNG NGHE KÊNH: 'ai_security_alert'`)*
- **Bước 2 (DBeaver):** Chạy câu lệnh giả lập nạp số tiền lớn bất thường:
  ```sql
  INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)
  VALUES ('topup', 1, 'cash', 10000000, 3, 'Demo nạp tiền lớn kích hoạt Trigger AI');
  ```
- **Bước 3 (Quan sát Terminal):** Cửa sổ Python nổ ra cảnh báo thời gian thực ngay lập tức:
  ```
  [🚨 CẢNH BÁO AN NINH PHÁT HIỆN LÚC ...]
  - Kênh (Channel)     : ai_security_alert
  - Mức độ rủi ro      : [HIGH] (Score: 70/100)
  - Nguyên nhân AI     : Thanh toán/nạp tiền lớn bất thường: 10,000,000 VNĐ
  ```
- **Thuyết minh:** *"Dạ thưa Thầy, đây là kiến trúc Real-Time Push Notification trực tiếp từ PostgreSQL Engine qua socket mạng không cần Polling định kỳ, giúp tối ưu băng thông và cảnh báo gian lận ngay tức thì."*
