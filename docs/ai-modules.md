# Contract 5 AI PostgreSQL

Tất cả module chạy trong PostgreSQL, không gọi LLM/API bên ngoài. `AiCenterView` gọi qua `DbRepository`; SQL đề xuất chỉ hiển thị readonly và duplicate không tự merge.

| Module | Input | Output/function | Trigger/UI | Giới hạn |
|---|---|---|---|---|
| Security Alert | Thay đổi employee, invoice, payment và thời điểm thao tác. | `ai.ai_alert`, risk score/level/reason và `pg_notify('ai_security_alert', payload)`. | Trigger audit; Admin LISTEN realtime và refresh AiCenter. | Điểm rủi ro là rule-based, không phải mô hình học máy. |
| Index Recommendation | `pg_stat_user_tables`, foreign key catalog và cột thật trong `pg_attribute`. | `ai.fn_recommend_indexes()` (hoặc `ai.fn_auto_optimize_indexes()`) trả recommendation_id, table, column, reason, SQL readonly. Chuẩn Idempotent (luôn trả về đề xuất). | Nút Chạy 5 module; Admin review trước khi áp dụng. | Phụ thuộc thống kê planner; không tự tạo index nếu chưa duyệt. |
| Duplicate Customer | Customer name/phone và threshold similarity 0.65. | `ai.fn_detect_duplicates(0.65)` trả candidate pair/similarity/reason. Chuẩn Idempotent. | AiCenter hiển thị trạng thái review. | Không tự merge; cần Admin xác nhận nghiệp vụ. |
| Anomaly Detection | Session/payment history và z-score threshold 2.0. | `ai.fn_detect_anomalies(2.0)` trả subject, metric, z-score, status. Chuẩn Idempotent. | AiCenter refresh thủ công hoặc sau notification. | Cần đủ fixture/history để stddev có ý nghĩa. |
| Promotion Suggestion | Materialized usage/top customer và dữ liệu doanh thu. | `ai.fn_suggest_promotions()` ghi Happy Hour/Win-back vào snapshot, luôn trả đề xuất. `ai.fn_apply_promotions()` kích hoạt khuyến mãi và gửi NOTIFY `ai_sms_dispatch`. `ai.fn_rollback_promotions()` hoàn tác. | AiCenter đọc snapshot; tích hợp Python SMS Gateway Listener (ntfy push). | Materialized view được refresh khi chạy module. |

## Quyền và notification

- Admin/Manager được đọc AI theo role hiện hữu; station không được đọc bảng AI.
- Station chỉ execute function cần thiết: login, heartbeat, logout, menu, order và service request.
- Channel duy nhất của security alert là `ai_security_alert`; channel cũ `audit_alert` không còn được listener dùng.
- Contract đọc cảnh báo là `ai.fn_security_guard()`; cảnh báo mới vẫn được sinh tự động bởi audit trigger `audit.fn_audit_security_alert_trigger()`.
- Payload notification chỉ là tín hiệu refresh; dữ liệu chi tiết được query lại qua repository.
- Index SQL được hiển thị readonly; catalog check trong function chỉ đề xuất cột tồn tại.
- Duplicate candidate có trạng thái `pending/merged/ignored`; Admin không được tự merge từ UI hiện tại.
