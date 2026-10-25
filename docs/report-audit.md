# Đối chiếu báo cáo Gemini với repo

Tài liệu này là bản đối chiếu sau khi cập nhật `main` tại commit `05f37f7`.
Một tuyên bố chỉ được xem là đạt khi có contract trong SQL và kiểm tra chạy được; ảnh chụp giao diện không thay thế cho kiểm thử CSDL.

## Các điểm báo cáo trước đây ghi sai hoặc thiếu

| Nội dung trong ảnh | Thực tế trước khi sửa | Cách xử lý |
|---|---|---|
| Lệnh lọc `IntegrationTests` đạt 16/16 | Lệnh đó chỉ chọn 5 test QA và fail nếu `.env` trỏ `netcafe_db`; toàn bộ solution hiện có 29 test case, trong đó 24 test live/unit đã pass và 5 test QA bị fail do chạy sai database. | QA test giữ cơ chế fail-closed khi không ở `netcafe_qa`; dùng `LiveDatabaseVerificationTests` cho database đã cấu hình và `scripts/run-qa.ps1` cho QA. |
| “6 schemas” hoặc “7 schemas” | Repo có 8 schema: `core`, `inventory`, `billing`, `hr`, `audit`, `ai`, `reporting`, `staging`. | Bộ kiểm tra mới xác nhận đủ 8 schema. |
| Chống trùng máy bằng `EXCLUDE USING gist` | README có ghi nhưng schema chưa tạo extension/constraint. | Thêm `btree_gist` và exclusion theo `computer_id` + khoảng thời gian phiên. |
| Login/đóng phiên dùng `FOR UPDATE SKIP LOCKED` | Hai function thực tế chỉ dùng `FOR UPDATE`; chỉ reconcile stale dùng `SKIP LOCKED`. | `core.sp_customer_login` và `billing.sp_close_session_and_pay_at` dùng `FOR UPDATE SKIP LOCKED`; đường Admin `sp_open_session` cũng khóa customer/máy trước khi kiểm tra. |
| Mã `GOLD10`, `CHAOHAY26`, `HAPPY22` luôn được áp dụng đúng điều kiện | Function chỉ kiểm tra mã, trạng thái và thời hạn; chưa kiểm tra hạng Gold, min bill hoặc khung giờ Happy Hour. | Checkout kiểm tra `required_tier`, `min_bill` và khung giờ `22:00–06:00` trước khi ghi `invoice_promotion`. |
| Kỹ thuật viên tạo/hoàn tất phiếu bảo trì | UI cho phép thao tác nhưng role `role_technician` chưa được `GRANT EXECUTE` hai function bảo trì. | Cấp quyền đúng cho `hr.sp_create_maintenance` và `hr.sp_complete_maintenance`; trigger vẫn là nơi đồng bộ trạng thái máy. |
| 5 function AI có tên `fn_auto_optimize_indexes` và `fn_security_guard` | Source cũ dùng `fn_recommend_indexes`; cảnh báo bảo mật là audit trigger, chưa có function contract cùng tên. Nút “chạy cả 5” thực tế gọi 4 function. | Giữ API cũ và bổ sung hai contract tương thích; module security có nút kiểm tra riêng, còn cảnh báo vẫn được sinh bởi trigger. |
| `mv_revenue_daily` được refresh | Source tạo `mv_monthly_revenue`; Admin đọc `vw_daily_revenue`; refresh không có snapshot ngày. | Thêm `reporting.mv_revenue_daily`, unique key theo ngày, đưa vào repository và refresh contract; giữ monthly view để tương thích. |
| Cache hit luôn đạt >99% và VACUUM luôn là quyền của `role_admin` | Telemetry mặc định cache 100% khi thiếu số liệu; UI ghi “Pool hoạt động tốt”; role demo chưa có quyền `MAINTAIN`. | Không còn giả lập 100%; UI chỉ ghi đạt >99% khi đo được; cấp `MAINTAIN`/`pg_read_all_stats` và kiểm tra quyền trước khi bật VACUUM. |
| `role_customer` bị từ chối trực tiếp | Repo không có role `role_customer`; station dùng `station_runtime` thừa kế `role_station`, chỉ được EXECUTE function. | Bằng chứng dùng đúng `station_runtime`/`role_station`; không tạo role giả chỉ để khớp ảnh. |

## Contract hiện tại

- 16 test live được gom trong `LiveDatabaseVerificationTests`; chúng kiểm tra metadata, view/materialized view, bảng, AI, RBAC, telemetry, exclusion constraint, concurrency contract và quyền DBA.
- `IntegrationTests` chỉ dành cho `netcafe_qa`; không được chạy trên database chính.
- `sql/11_report_alignment.sql` là migration bổ sung idempotent cho database đã tồn tại. `scripts/migrate-main.ps1` đã đưa migration này vào sau các file AI, station và Admin.
- Kiểm thử live không chứng minh giao diện headed, hiệu năng 100.000 dòng hoặc triển khai production; các cổng đó phải được ghi bằng bằng chứng riêng.

## Lệnh kiểm chứng chuẩn

```powershell
dotnet test app/NetCafe.Data.Tests/NetCafe.Data.Tests.csproj --filter "FullyQualifiedName~LiveDatabaseVerificationTests"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-qa.ps1
```

Migration chính chỉ được chạy sau khi có backup và credential ngoài source:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\migrate-main.ps1
```
