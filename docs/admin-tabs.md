# Hướng dẫn các tab Admin

File này mô tả mục đích thao tác của các tab hiện có, không chứa credential và không thay đổi contract database.

- **Máy trạm:** sơ đồ 4/3/2 cột, mở chi tiết máy, kết thúc phiên qua `billing.sp_close_session_and_pay` và theo dõi heartbeat.
- **Hội viên:** tìm kiếm, chọn card, nạp ví qua `billing.sp_top_up`, thêm/sửa/khóa và reset mật khẩu khách hàng.
- **Gọi món:** menu dịch vụ có ảnh theo SKU, lọc danh mục, nhập kho qua `inventory.sp_receive_stock` và hàng đợi bếp.
- **Hóa đơn:** lọc paid/unpaid/void, xem chi tiết món, void hóa đơn qua `billing.sp_void_invoice`, xuất PDF/Excel. Void tạo audit và có thể phát `ai_security_alert`.
- **Báo cáo:** chuyển giữa doanh thu ngày, hiệu suất máy và top hội viên; nút cập nhật số liệu gọi `reporting.sp_refresh_materialized_views`.
- **Nhân viên:** quản lý nhân viên qua function `hr.sp_*`; salary được kiểm soát bằng column privilege và nút xem bảng lương có thể bị PostgreSQL từ chối theo role.
- **Bảo trì:** lập/hoàn tất phiếu qua `hr.sp_create_maintenance` và `hr.sp_complete_maintenance`; nút nhanh trên thẻ máy cũng đi qua hai function này để trigger CSDL cập nhật `core.computer.status`.
- **Phân tích dữ liệu:** năm module AI chạy trong PostgreSQL, không gọi chatbot/API ngoài.
- **Yêu cầu hỗ trợ:** nhận request từ Station qua `support_queue_changed`, xử lý theo vòng đời `new → acknowledged → resolved` hoặc `cancelled`.
- **Thông tin hệ thống:** chẩn đoán kết nối, employee mapping, migration contract, LISTEN/NOTIFY và chạy VACUUM ANALYZE có kiểm soát.
