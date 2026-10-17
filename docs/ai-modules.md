# Contract 5 AI PostgreSQL

Tất cả module chạy trong PostgreSQL, không gọi LLM/API bên ngoài. `AiCenterView` gọi qua `DbRepository`; SQL đề xuất chỉ hiển thị readonly và duplicate không tự merge.

| Module | Input | Output/function | Trigger/UI | Giới hạn |
|---|---|---|---|---|
| Security Alert | Thay đổi employee, invoice, payment và thời điểm thao tác. | `ai.ai_alert`, risk score/level/reason và `pg_notify('ai_security_alert', payload)`. | Trigger audit; Admin LISTEN realtime và refresh AiCenter. | Điểm rủi ro là rule-based, không phải mô hình học máy. |
| Index Recommendation | `pg_stat_user_tables`, foreign key catalog và cột thật trong `pg_attribute`. | `ai.fn_recommend_indexes()` trả table, column, reason, SQL readonly. | Nút Chạy 5 module; Admin phải review trước khi áp dụng. | Phụ thuộc thống kê planner; không tự tạo index. |
| Duplicate Customer | Customer name/phone và threshold similarity 0.65. | `ai.fn_detect_duplicates(0.65)` trả candidate pair/reason. | AiCenter hiển thị trạng thái review. | Không tự merge; cần Admin xác nhận nghiệp vụ. |
| Anomaly Detection | Session/payment history và z-score threshold 2.0. | `ai.fn_detect_anomalies(2.0)` trả subject, metric, z-score, status. | AiCenter refresh thủ công hoặc sau notification. | Cần đủ fixture/history để stddev có ý nghĩa. |
| Promotion Suggestion | Materialized usage/top customer và dữ liệu doanh thu. | `ai.fn_suggest_promotions()` trả Happy Hour/Win-back details. | AiCenter hiển thị suggestion; không tự phát hành khuyến mãi. | Materialized view cần refresh theo runbook. |

## Quyền và notification

- Admin/Manager được đọc AI theo role hiện hữu; station không được đọc bảng AI.
- Station chỉ execute function cần thiết: login, heartbeat, logout, menu, order và service request.
- Channel duy nhất của security alert là `ai_security_alert`; channel cũ `audit_alert` không còn được listener dùng.
- Payload notification chỉ là tín hiệu refresh; dữ liệu chi tiết được query lại qua repository.
