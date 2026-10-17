# KỊCH BẢN BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU
## ĐỀ TÀI: XÂY DỰNG VÀ TỐI ƯU HÓA CƠ SỞ DỮ LIỆU CHO HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)

---

## 1. CHUẨN BỊ TRƯỚC BUỔI BẢO VỆ (CHECKLIST 5 PHÚT)

1. **Khởi động PostgreSQL 17/18**: Đảm bảo dịch vụ đang chạy trên cổng `5432`.
2. **Kiểm tra CSDL**: QA dùng `scripts/run-qa.ps1`; database chính dùng `scripts/migrate-main.ps1` sau khi có backup. Không chạy full SQL/reset/seed lên `netcafe_db`.
3. **Bố trí 2 cửa sổ song song trên màn hình chiếu (Dual-Window Layout)**:
   - **Nửa màn hình TRÁI (50%)**: Mở Admin (`NetCafe.App.exe`) và Station (`NetCafe.Client.exe`) ở hai cửa sổ riêng.
   - **Nửa màn hình PHẢI (50%)**: Mở **pgAdmin 4** hoặc **DBeaver**, kết nối đúng database đang hiển thị trong Settings.

---

## 2. KỊCH BẢN TRÌNH DIỄN THEO DÒNG THỜI GIAN (15 PHÚT)

| Thời gian | Nội dung | Thao tác trên màn hình | Điểm nhấn giải thích trước Hội đồng |
|---|---|---|---|
| **00:00 - 03:00** (3 phút) | **Giới thiệu Tổng quan & Kiến trúc CSDL** | Mở sơ đồ ERD (`docs/erd_viewer.html`) và giới thiệu 8 schemas | - 1 instance, 8 schemas chuyên biệt (`core`, `inventory`, `billing`, `hr`, `audit`, `ai`, `reporting`, `staging`).<br>- 16 bảng nghiệp vụ đạt chuẩn 3NF + 6 bảng mở rộng.<br>- Database tạo từ `template0 ENCODING 'UTF8'` không phụ thuộc OS locale. |
| **03:00 - 07:00** (4 phút) | **Demo Song song: Thao tác Desktop -> Bản ghi CSDL** | Thao tác trên Desktop Client (Trái), đồng thời F5 query trên DBeaver (Phải) | - Mở phiên máy PC001 -> Thấy trigger tự động đổi trạng thái máy sang `in_use`.<br>- Gọi món F&B -> Thấy trigger trừ tồn kho ngay lập tức.<br>- Thanh toán phiên -> Hóa đơn tự sinh, thanh toán cập nhật, máy về `available`.<br>- Thanh SQL Strip phía dưới app hiển thị câu lệnh và thời gian thực thi nội bộ CSDL. |
| **07:00 - 09:30** (2.5 phút) | **Demo Tính toàn vẹn & Phân quyền bảo mật** | Chạy `demo/demo_1_init_constraints.sql` và `demo/demo_2_security_rls.sql` | - Thử đặt trùng giờ máy -> Bị chặn bởi `EXCLUDE USING gist`.<br>- Thử tạo hóa đơn âm tiền -> Bị chặn bởi CHECK constraint.<br>- Chuyển vai trò thu ngân `nv_thao` -> RLS chỉ hiển thị đúng hóa đơn của Thảo.<br>- Thử truy vấn cột `salary` -> PostgreSQL trả về lỗi `permission denied` (Column-level security). |
| **09:30 - 11:30** (2 phút) | **Demo Tối ưu hiệu năng & DBA Operations** | Chạy `demo/demo_3_performance.sql` | - Giải thích `EXPLAIN (ANALYZE, BUFFERS)`: GIN Trigram cho họ tên có ký tự đại diện `%Thành%`, GIN `jsonb_path_ops` cho phần cứng GPU, Partial Index cho phiên `active`.<br>- Chạy `audit.fn_create_monthly_partitions(3)` để thấy cơ chế tự động sinh bảng phân vùng hàng tháng. |
| **11:30 - 13:30** (2 phút) | **Điểm sáng: 5 Module In-Database AI** | Chạy `demo/demo_4_highlights.sql` | - **Module 1**: Sửa lương hoặc nạp tiền lớn -> Bắn cảnh báo NOTIFY tức thì qua trigger.<br>- **Module 2**: Quét catalog `pg_stat_user_tables` gợi ý index.<br>- **Module 3**: Dùng `pg_trgm` phát hiện khách trùng tên/SĐT.<br>- **Module 4**: Tính Z-score phát hiện phiên chơi 36 tiếng và tiền nạp 15 triệu.<br>- **Module 5**: Khai phá Materialized Views đề xuất Happy Hour và Voucher giữ chân khách VIP. |
| **13:30 - 15:00** (1.5 phút) | **Hỏi đáp phản biện & Kết luận** | Trả lời câu hỏi của Giảng viên | Xem Bộ câu hỏi phản biện bên dưới. |

---

## 3. BỘ CÂU HỎI PHẢN BIỆN THƯỜNG GẶP VÀ CÂU TRẢ LỜI "VÌ SAO"

### Câu 1: Vì sao lại dùng `TEMPLATE template0` và `LC_COLLATE = 'C'` khi tạo Database?
> **Trả lời:** "Dạ thưa Thầy/Cô, việc dùng `template0` và bảng mã UTF-8 với `LC_COLLATE = 'C'` giúp CSDL hoàn toàn trung lập với hệ điều hành của máy chủ cài đặt (Windows hay Linux). Khi chấm bài trên một máy tính độc lập, hệ thống không bị lỗi xung đột locale tiếng Việt của Windows, đồng thời việc sắp xếp và tìm kiếm không dấu đã được giải quyết bằng extension `unaccent` và `pg_trgm`."

### Câu 2: Vì sao chống đặt trùng máy lại dùng `EXCLUDE USING gist` mà không dùng Trigger hay Unique Index?
> **Trả lời:** "Dạ thưa Thầy/Cô, Unique Index chỉ kiểm tra được sự trùng lặp giá trị rời rạc đơn lẻ (discrete value). Trong nghiệp vụ đặt phòng net, thời gian đặt là một khoảng liên tục (`tstzrange(from, to)`). Ràng buộc `EXCLUDE USING gist` sử dụng toán tử giao nhau (`&&`) để khóa phạm vi ở mức tầng lưu trữ vật lý của PostgreSQL, giúp chống race condition (xung đột đồng thời khi 2 khách đặt cùng 1 micro-giây) triệt để hơn nhiều so với việc kiểm tra bằng trigger ở mức ứng dụng."

### Câu 3: Vì sao số tiền dùng `NUMERIC(12,0)` mà không dùng `BIGINT` hay `FLOAT/DOUBLE`?
> **Trả lời:** "Dạ thưa Thầy/Cô, kiểu `FLOAT/DOUBLE` sử dụng dấu phẩy động chuẩn IEEE 754 có sai số làm tròn nhị phân, tuyệt đối không được dùng trong tài chính kế toán. Còn `NUMERIC(12,0)` trong PostgreSQL là kiểu fixed-point số học chính xác tuyệt đối, thang đo 12 chữ số đủ biểu diễn số tiền tới hàng trăm tỷ đồng Việt Nam (vốn không có phần thập phân xu/hào), vừa chính xác vừa tiết kiệm không gian lưu trữ."

### Câu 4: Vì sao hàm hỗ trợ RLS `fn_get_current_employee_id()` lại phải đặt `SECURITY DEFINER` và `SET search_path = hr, pg_temp`?
> **Trả lời:** "Dạ thưa Thầy/Cô, hàm cần `SECURITY DEFINER` để chạy dưới quyền của chủ sở hữu hàm (postgres) nhằm tra cứu ID nhân viên trong bảng `hr.employee` ngay cả khi nhân viên đang đăng nhập không có quyền xem toàn bảng. Việc cố định `SET search_path = hr, pg_temp` là tiêu chuẩn an ninh bắt buộc của PostgreSQL để ngăn chặn kỹ thuật tấn công chèn mã độc (search_path hijacking)."

### Câu 5: Vì sao chọn BRIN Index cho bảng `audit.audit_log` thay vì B-Tree?
> **Trả lời:** "Dạ thưa Thầy/Cô, bảng `audit_log` là bảng ghi nhật ký thời gian tăng dần liên tục (append-only). Các block dữ liệu trên đĩa vật lý được sắp xếp hoàn toàn theo thứ tự thời gian. BRIN (Block Range Index) chỉ lưu tóm tắt min/max của từng cụm trang đĩa, dung lượng chỉ chiếm dưới 1% so với B-Tree nhưng cho tốc độ quét theo khoảng ngày tháng tương đương, giúp tiết kiệm bộ nhớ RAM đệm và dung lượng đĩa cực kỳ lớn."

### Câu 6: Thuật toán AI trong đề tài này có gọi API bên ngoài (OpenAI/ChatGPT) không?
> **Trả lời:** "Dạ thưa Thầy/Cô, đề tài tuân thủ nghiêm ngặt nguyên tắc In-Database Intelligence: 100% thuật toán chạy nội bộ bằng toán học, thống kê và hàm PL/pgSQL có sẵn của PostgreSQL 17 (Z-score với `stddev_samp`, khoảng cách chuỗi Trigram với `similarity`, thống kê cửa sổ `NTILE`, Moving Average trên Materialized Views). Hệ thống hoàn toàn không gọi API bên ngoài và có thể chạy độc lập không cần kết nối Internet."

## 4. KỊCH BẢN QA STATION VÀ SUPPORT

1. Chạy `scripts/reset-qa.ps1`; xác nhận marker và chỉ có database `netcafe_qa` được reset.
2. Chạy `tests/station_customer.sql`; kiểm tra cột heartbeat, function, role station, notification channel và service request.
3. Mở `NetCafe.Client` tại PC001, đăng nhập customer có balance dương; kiểm tra session active và máy chuyển `in_use`.
4. Chờ heartbeat theo cấu hình 30 giây; projected play/service chỉ hiển thị, chưa tạo payment.
5. Gọi món trong hạn mức; thử vượt ví để chứng minh database từ chối đơn.
6. Gửi support request và xử lý `new → acknowledged → resolved` trong Support queue Admin.
7. Logout station; kiểm tra invoice/payment, session `finished`, close reason `station_logout`, máy `available`.
8. Dừng station không logout, đợi hơn 90 giây rồi mở Computers; watcher reconcile với close reason `stale_timeout`.
9. Mở AiCenter, chạy đủ 5 module; index SQL readonly, duplicate không tự merge.

## 5. Lệnh kiểm tra nhanh

```powershell
$env:DB_NAME = "netcafe_qa"
$env:DB_PASSWORD = "<cấp ngoài source>"
powershell -ExecutionPolicy Bypass -File .\scripts\run-qa.ps1
dotnet run --project .\app\NetCafe.App
dotnet run --project .\app\NetCafe.Client
```

Nếu không có credential QA, chỉ báo cáo build/static check; không dùng database production hoặc evidence cũ để kết luận live demo.
