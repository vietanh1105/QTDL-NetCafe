# Báo cáo tối ưu refresh và data access

## Thay đổi đã thực hiện

| Khu vực | Trước | Sau | Ý nghĩa |
|---|---|---|---|
| Computer refresh | Timer có thể reload toàn bộ grid dày và làm UI giật. | Chỉ refresh khi Computers đang visible, 15 giây, reconcile stale trước, gate một query tại một thời điểm. | Giảm query thừa và tránh chồng tác vụ. |
| View lifecycle | Shell lớn chứa toàn bộ logic màn hình. | `MainForm` điều phối, từng màn hình là `UserControl` + Designer metadata. | Giảm coupling và cho phép thay view độc lập. |
| SQL ownership | Query mới có nguy cơ nằm trong UI. | `DbRepository` partial giữ Admin/Station query; UI chỉ gọi method. | Giữ SQL logger và role boundary. |
| Station heartbeat | Không có timestamp heartbeat/stale policy. | Cột partial index, heartbeat 30 giây, stale reconcile 90 giây. | Dọn session mất điện và trả máy về available qua function DB. |
| Wallet | Có nguy cơ trừ payment mỗi heartbeat. | Heartbeat chỉ tính projected amount; logout/auto-close mới tạo invoice/payment. | Tránh payment trùng và giữ balance không âm. |
| AI index | Gợi ý `created_at` dù bảng không có cột. | Tra catalog và chỉ sinh SQL cho cột hiện hữu. | SQL đề xuất có thể chạy kiểm tra được. |
| Credential | Một số script có password mặc định. | Password rỗng nếu không được cấp từ environment/secret store. | Không commit credential thật. |
| Database preflight | Admin không biết đang trỏ database nào và timer gọi function thiếu. | Preflight hiển thị host/database/user và capability migration trước khi timer chạy. | Không phát sinh truy vấn reconcile lỗi lặp lại trên DB legacy. |
| Card máy | Grid dựng lại datasource toàn bộ sau mỗi lần refresh. | Dictionary `ComputerId -> ComputerCardControl`, chỉ bind thêm/sửa/xóa card thay đổi. | Giảm tạo control và giữ trạng thái chọn/hover của card. |
| AI promotion | Mở AI Center có thể gọi function kèm refresh materialized view. | Snapshot đọc bảng; chạy function chỉ khi bấm nút. | Tránh side effect và query nặng ngoài ý muốn. |
| Daily revenue snapshot | Admin đọc trực tiếp `vw_daily_revenue` trong khi báo cáo yêu cầu materialized view ngày. | Thêm `reporting.mv_revenue_daily`, unique key theo ngày và đưa vào refresh contract. | Lọc khoảng ngày vẫn do Admin thực hiện sau khi đọc snapshot. |

## Số liệu và cách đo

- Build hiện tại kiểm chứng được: `dotnet build app/NetCafe.slnx --no-restore` → 0 warning, 0 error.
- QA ngày 2026-10-18: reset `netcafe_qa`, station contract PASS, login/heartbeat/order/support/logout PASS và xUnit 6/6 PASS.
- Chưa ghi p50/p95 latency, memory hoặc số query headed vì chưa chạy fixture 100.000 dòng và chưa mở soak UI 60 giây.
- Khi đo performance, chạy `python/measure_explain.py` trên `netcafe_qa` với `EXPLAIN (ANALYZE, BUFFERS)` thực tế.
- Không dùng `enable_seqscan = off`; kết quả chỉ chấp nhận khi planner tự chọn index với fixture thật.
- UI acceptance cần chạy headed trên Windows: mở Admin, đổi role, mở Computers, tạo station session và quan sát không có refresh chồng. Build không thay thế visual/runtime evidence.

## Checklist đo QA

1. Reset đúng `netcafe_qa` bằng marker `core.qa_reset_marker`.
2. Chạy test SQL station và xUnit integration.
3. Nạp fixture tối thiểu 100.000 dòng theo bảng truy vấn.
4. Ghi `EXPLAIN (ANALYZE, BUFFERS)` trước/sau cho active session, trigram, JSONB và FK invoice.
5. Đo số lần gọi query khi mở Computers 60 giây: chỉ refresh visible, không có task chạy chồng.
6. Ghi latency p50/p95 và working set process từ cùng một máy QA.

## Giới hạn hiện tại

Migration, code và QA contract đã được kiểm chứng trên `netcafe_qa`. Performance fixture lớn, headed UI và số liệu trước/sau vẫn chưa có bằng chứng mới nên không kết luận thay cho các cổng đó.
