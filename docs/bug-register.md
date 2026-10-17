# Bug register và trạng thái sửa

Phạm vi kiểm chứng: source review và build solution đã chạy; kiểm thử tích hợp cần connection tới `netcafe_qa`. Các mục ghi `Chưa chạy QA` không được xem là runtime acceptance.

| Bug | Triệu chứng/nguyên nhân | File liên quan | Cách sửa | Bằng chứng |
|---|---|---|---|---|
| Notification lệch channel | Trigger gửi `ai_security_alert` nhưng Admin từng LISTEN `audit_alert`. | `sql/07_ai_modules.sql`, `app/NetCafe.Data/DbRepository.cs`, `app/NetCafe.App/MainForm.cs` | Đồng nhất channel `ai_security_alert`, listener restart theo role. | `rg` chỉ còn channel mới; build pass. |
| Hard-code employee 1 | Role switch không ánh xạ employee và fallback ghi payment/top-up vào employee cố định. | `MainForm.cs`, `AdminViewContract.cs`, `CustomersView.cs` | Employee lấy từ `db_username`; không có mapping thì ID = 0 và chặn top-up. | Source review; build pass. |
| Direct query rải trong UI | View tự mở connection hoặc viết SQL làm bypass logger. | `Views/*.cs`, `DbRepository.cs`, `AdminQueries.cs` | View chỉ gọi repository; query mới tập trung ở partial repository. | `rg` không thấy Npgsql trong Views; build pass. |
| Role/listener không đồng bộ | Đổi role nhưng listener và context vẫn dùng repository cũ. | `MainForm.cs` | Tạo repository/context mới, cancel listener cũ, khởi động lại LISTEN. | Source review; build pass. |
| Reload grid gây giật | Timer cũ reload toàn bộ UI liên tục. | `MainForm.cs`, `ComputersView.cs` | Chỉ reconcile/refresh khi Computers đang hiển thị, gate bằng `SemaphoreSlim`, chu kỳ 15 giây. | Source review; build pass. |
| Refresh query chồng nhau | Nút refresh và timer có thể chạy đồng thời. | `MainForm.cs` | `_refreshGate.WaitAsync(0)` và release trong `finally`. | Source review; build pass. |
| ComboBox dynamic/anonymous | Mapping item không rõ kiểu dễ lỗi khi chưa chọn. | Admin views mới | Dùng DTO typed trong Data layer và kiểm tra row/null trước thao tác. | Build pass; chưa có UI headed test. |
| Credential hard-code | Python/backup có password mặc định trong source. | `python/*.py`, `backup/*.ps1`, `backup/*.sh`, `AppConfig.cs` | Default rỗng/local QA; lấy credential từ environment hoặc station config. | `rg` không còn các password cũ trong code; build pass. |
| Không có heartbeat/stale policy | Session không lưu heartbeat, watcher không biết station mất điện. | `sql/08_station_customer.sql`, `StationSessionController.cs` | Thêm heartbeat 30 giây, `last_heartbeat_at`, reconcile stale 90 giây và close reason. | Migration/source review; chưa chạy QA do thiếu password local. |
| Customer chưa có login/session flow | Admin-only API không có function giới hạn cho station. | `NetCafe.Client`, `StationRepository.cs`, `sql/08_station_customer.sql` | Thêm login, heartbeat, logout, menu/order/support function và role station. | Solution build pass; chưa chạy DB function. |
| AI Center không có UI | 5 function có trong SQL nhưng shell không có màn hình. | `AiCenterView.cs`, `StationRepository.cs` | AiCenter gọi đủ alert/index/duplicate/anomaly/promotion và hiển thị readonly/review status. | Build pass; DB invocation chưa chạy QA. |
| Index recommendation sinh cột không tồn tại | Function luôn tạo SQL cho `created_at`. | `sql/07_ai_modules.sql` | Chọn cột có thật từ catalog theo thứ tự ưu tiên; bỏ qua bảng không có cột phù hợp. | Full SQL regenerated; chưa EXPLAIN trên QA. |
| Tài liệu/seed/evidence lệch | README/runbook cũ chỉ nói 00–07 và `netcafe_db`. | `README.md`, `demo/runbook.md`, `docs/*.md` | Tạo code map, bug register, optimization, AI docs và runbook QA mới. | File docs có trong commit; nội dung runtime cần cập nhật evidence sau QA. |
