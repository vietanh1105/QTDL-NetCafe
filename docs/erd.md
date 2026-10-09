# SƠ ĐỒ THỰC THỂ QUAN HỆ (ERD - ENTITY RELATIONSHIP DIAGRAM)

> **Dự án:** Quản trị Cơ sở Dữ liệu Phòng Game/Net (`netcafe_db`)  
> **Chuẩn mô hình hóa:** ERD Crow's Foot Notation (Mermaid)  
> **Quy mô:** 22 bảng (16 bảng nghiệp vụ, 6 bảng giám sát & AI)

---

## 1. SƠ ĐỒ ERD TỔNG THỂ (MERMAID)

```mermaid
erDiagram
    %% Core Schema
    ROOM ||--o{ COMPUTER : "contains"
    COMPUTER_TYPE ||--o{ COMPUTER : "categorizes"
    ROOM ||--o{ COMPUTER : "placed_in"
    CUSTOMER ||--o{ GAMING_SESSION : "plays"
    COMPUTER ||--o{ GAMING_SESSION : "hosts"
    EMPLOYEE ||--o{ GAMING_SESSION : "opened_by"
    CUSTOMER ||--o{ BOOKING : "books"
    COMPUTER ||--o{ BOOKING : "reserved_for"

    %% Inventory Schema
    PRODUCT_CATEGORY ||--o{ PRODUCT : "classifies"
    GAMING_SESSION ||--o{ FOOD_ORDER : "orders_food"
    EMPLOYEE ||--o{ FOOD_ORDER : "served_by"
    FOOD_ORDER ||--|{ FOOD_ORDER_DETAIL : "has_items"
    PRODUCT ||--o{ FOOD_ORDER_DETAIL : "ordered_in"

    %% Billing Schema
    GAMING_SESSION ||--|| INVOICE : "generates (1-1)"
    CUSTOMER ||--o{ INVOICE : "billed_to"
    EMPLOYEE ||--o{ INVOICE : "collected_by"
    INVOICE ||--o{ INVOICE_PROMOTION : "applies"
    PROMOTION ||--o{ INVOICE_PROMOTION : "discount_applied"
    INVOICE ||--o{ PAYMENT : "settled_by"
    CUSTOMER ||--o{ PAYMENT : "transacts"
    EMPLOYEE ||--o{ PAYMENT : "processed_by"

    %% HR Schema
    COMPUTER ||--o{ MAINTENANCE : "maintained"
    EMPLOYEE ||--o{ MAINTENANCE : "performed_by"

    %% Audit & AI Schema
    AUDIT_LOG ||--o{ AI_ALERT : "triggers_alert"
    CUSTOMER ||--o{ DUPLICATE_CANDIDATE : "customer_a"
    CUSTOMER ||--o{ DUPLICATE_CANDIDATE : "customer_b"

    ROOM {
        int room_id PK
        varchar room_name UK
        text description
    }

    COMPUTER_TYPE {
        int type_id PK
        varchar type_name UK
        numeric hourly_rate
    }

    COMPUTER {
        int computer_id PK
        varchar computer_code UK
        varchar computer_name
        int room_id FK
        int type_id FK
        jsonb specs
        inet ip_address UK
        macaddr mac_address UK
        varchar status
        date purchased_at
        timestamptz created_at
    }

    CUSTOMER {
        int customer_id PK
        varchar full_name
        varchar phone UK
        varchar email UK
        varchar username UK
        varchar password_hash
        numeric balance
        int loyalty_points
        varchar membership_tier
        boolean is_active
        timestamptz created_at
    }

    GAMING_SESSION {
        bigint session_id PK
        int customer_id FK
        int computer_id FK
        int opened_by FK
        timestamptz start_time
        timestamptz end_time
        varchar status
        numeric hourly_rate_applied
        numeric play_amount
        jsonb game_info
    }

    BOOKING {
        int booking_id PK
        int customer_id FK
        int computer_id FK
        timestamptz booked_from
        timestamptz booked_to
        varchar status
        timestamptz created_at
    }

    PRODUCT_CATEGORY {
        int category_id PK
        varchar category_name UK
    }

    PRODUCT {
        int product_id PK
        varchar sku UK
        varchar product_name
        int category_id FK
        numeric unit_price
        int stock_qty
        jsonb attributes
        boolean is_active
    }

    FOOD_ORDER {
        bigint order_id PK
        bigint session_id FK
        int employee_id FK
        timestamptz ordered_at
        varchar status
    }

    FOOD_ORDER_DETAIL {
        bigint order_id PK_FK
        int product_id PK_FK
        int quantity
        numeric unit_price
    }

    PROMOTION {
        int promotion_id PK
        varchar code UK
        varchar promotion_name
        varchar promo_type
        numeric value
        timestamptz valid_from
        timestamptz valid_to
        jsonb conditions
        boolean is_active
    }

    INVOICE {
        bigint invoice_id PK
        bigint session_id UK_FK
        int customer_id FK
        int cashier_id FK
        numeric play_amount
        numeric service_amount
        numeric surcharge
        numeric discount_amount
        numeric total_amount
        varchar status
        timestamptz issued_at
        timestamptz paid_at
    }

    INVOICE_PROMOTION {
        bigint invoice_id PK_FK
        int promotion_id PK_FK
        numeric discount_amount
    }

    PAYMENT {
        bigint payment_id PK
        varchar kind
        bigint invoice_id FK
        int customer_id FK
        varchar method
        numeric amount
        int employee_id FK
        timestamptz paid_at
        varchar reference
    }

    EMPLOYEE {
        int employee_id PK
        varchar full_name
        varchar phone
        varchar username UK
        varchar db_username UK
        varchar password_hash
        varchar emp_role
        varchar default_shift
        numeric salary
        date hired_date
        boolean is_active
    }

    MAINTENANCE {
        int maintenance_id PK
        int computer_id FK
        int technician_id FK
        text issue_description
        timestamptz started_at
        timestamptz finished_at
        numeric cost
        jsonb parts_replaced
        varchar status
    }

    AUDIT_LOG {
        bigint audit_id PK
        timestamptz logged_at PK
        varchar actor
        varchar table_name
        varchar action
        varchar row_pk
        jsonb old_data
        jsonb new_data
    }

    AI_ALERT {
        int alert_id PK
        bigint audit_id
        timestamptz audit_logged_at
        int risk_score
        varchar risk_level
        text reason
        varchar status
        timestamptz created_at
    }

    INDEX_RECOMMENDATION {
        int rec_id PK
        varchar table_name
        varchar column_list
        text reason
        text suggested_sql
        timestamptz created_at
    }

    DUPLICATE_CANDIDATE {
        int cand_id PK
        int customer_id_a FK
        int customer_id_b FK
        numeric similarity
        text reason
        varchar status
        timestamptz created_at
    }

    ANOMALY_FLAG {
        int flag_id PK
        varchar anomaly_type
        varchar subject_table
        varchar subject_pk
        numeric metric_value
        jsonb threshold_info
        timestamptz detected_at
        varchar status
    }

    PROMOTION_SUGGESTION {
        int sug_id PK
        varchar suggestion_type
        varchar target
        jsonb details
        timestamptz created_at
    }
```

---

## 2. GIẢI THÍCH CHI TIẾT CÁC QUAN HỆ ĐẶC BIỆT

### 2.1. Quan hệ 1-1 giữa `gaming_session` và `invoice`
- **Mã cài đặt:** `invoice.session_id BIGINT UNIQUE NOT NULL REFERENCES core.gaming_session(session_id) ON DELETE RESTRICT`
- **Ý nghĩa:** Một phiên chơi khi kết thúc chỉ được phép sinh ra **đúng một** hóa đơn tài chính. Việc gán `UNIQUE` trên khóa ngoại `session_id` biến mối quan hệ 1-N thông thường thành quan hệ **1-1 bắt buộc**.
- **Giải thích trước hội đồng:** Giúp bảo đảm tính toàn vẹn kế toán, ngăn chặn lỗi phần mềm thu ngân gọi hàm tính tiền 2 lần dẫn đến tạo 2 hóa đơn chồng lấn cho cùng một phiên chơi.

### 2.2. Quan hệ N-N: `food_order` và `product` qua `food_order_detail`
- **Khóa chính hợp thành (Composite PK):** `PRIMARY KEY (order_id, product_id)`
- **Ý nghĩa:** Một đơn gọi món có thể có nhiều sản phẩm đồ ăn/uống, và một sản phẩm có thể được nhiều đơn gọi. Cột `unit_price` trong bảng chi tiết này lưu trữ đơn giá thực tế chốt tại thời điểm mua (Snapshot Pricing), bảo vệ dữ liệu lịch sử không bị biến động khi quán cập nhật menu.

### 2.3. Quan hệ N-N: `invoice` và `promotion` qua `invoice_promotion`
- **Khóa chính hợp thành (Composite PK):** `PRIMARY KEY (invoice_id, promotion_id)`
- **Ý nghĩa:** Một hóa đơn có thể áp dụng đồng thời nhiều khuyến mãi hợp lệ (ví dụ: Giảm giá thành viên VIP + Tặng giờ chơi Happy Hour). Cột `discount_amount` ghi nhận chính xác số tiền được giảm tương ứng với từng mã.

### 2.4. Quan hệ Đa hình Logic (Polymorphism) trong `payment`
- Bảng `payment` quản lý 2 loại dòng tiền:
  1. Thanh toán hóa đơn kết thúc phiên (`kind = 'invoice'`, `invoice_id NOT NULL`).
  2. Nạp tiền trả trước vào tài khoản hội viên (`kind = 'topup'`, `invoice_id IS NULL`).
- Được khóa chặt bằng ràng buộc mức bảng:
  `CHECK ((kind = 'invoice' AND invoice_id IS NOT NULL) OR (kind = 'topup' AND invoice_id IS NULL))`
