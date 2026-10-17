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

## Số liệu và cách đo

- Build baseline kiểm chứng được: `dotnet build app/NetCafe.slnx --no-restore` → 0 warning, 0 error.
- Chưa ghi số query/latency/memory runtime vì local PostgreSQL yêu cầu password chưa được cung cấp trong môi trường chạy này.
- Khi có quyền QA, chạy `scripts/run-qa.ps1`; dùng `EXPLAIN (ANALYZE, BUFFERS)` trong `python/measure_explain.py` trên `netcafe_qa`.
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

Migration và code đã được compile/static review nhưng chưa được chạy trên `netcafe_qa` do thiếu credential PostgreSQL local. Không dùng log cũ trong `evidence/` để kết luận runtime cho phiên bản mới.
