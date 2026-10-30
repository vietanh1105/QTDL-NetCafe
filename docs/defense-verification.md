## Kết quả kiểm tra và phạm vi bằng chứng

Các kết quả dưới đây ghi nhận trong đợt rà soát ngày 29–30/10/2026. Cần phân biệt mã nguồn đã sửa, trạng thái CSDL Neon đang triển khai và các thử nghiệm trên môi trường độc lập.

| Nội dung | Môi trường và cách kiểm tra | Kết quả |
|---|---|---|
| Metadata triển khai | Truy vấn trực tiếp Neon, chế độ chỉ đọc | netcafe_db, PostgreSQL 18.6, 8 schema, 25 bảng logic, 5 partition bảng, 5 view, 4 MV |
| Kiểm tra quyền hiện có | pg_proc và privilege trên Neon | Ba hàm cũ vẫn SECURITY INVOKER; chưa áp migration 14 lên Neon |
| Chỉ mục | Chạy demo_6_index_fixture.sql trực tiếp trên Neon | 20.000 dòng tạm; Seq Scan → Index Scan; 1,142 ms → 0,072 ms trong lần đo; đã ROLLBACK |
| Listener Python đã sửa | Neon, psycopg 3, kênh thử ngẫu nhiên riêng | Đăng ký và nhận payload thành công; không gửi ntfy/SMS |
| Toàn bộ QA sau sửa | scripts/run-qa.ps1 trên PostgreSQL 18.6 độc lập | Exit 0; build 0 warning/error; 29/29 test .NET; các bài SQL Station và AI đạt |
| Quyền thu ngân sau sửa | tests/verify_defense_roles.sql trong QA | Nạp tiền, mở phiên, gọi món, đóng phiên đạt; giả ID nhân viên bị chặn; ROLLBACK dữ liệu thử |
| Khởi tạo từ SQL Cloud | Database thử mới, thực thi bản tổng hợp hai lần | Mỗi lần có 14 khách mẫu, 20 máy, 9 hóa đơn; bốn FK của service_request còn đủ |
| Hàm khởi tạo Python | reset_and_init_cloud.init_cloud trên database thử | Hoàn tất; 8 schema, 25 bảng logic; không xem thời gian local là thời gian Neon |
| ETL | Generator 20 dòng và cleaner trên dữ liệu thử riêng | 16 nạp, 4 loại; hai lỗi số dư âm và hai lỗi điện thoại |
| Sao lưu và khôi phục | pg_dump -Fc, pg_restore --exit-on-error trên dữ liệu thử | Exit 0; đối chiếu số dòng 26 bảng gồm marker QA khớp |

Neon chỉ được đọc metadata, dùng bảng tạm để đo kế hoạch và dùng kênh notification thử riêng. Không thay function nghiệp vụ, không chạy reset/seed, không nạp tiền hoặc chỉnh số dư khách trên Neon trong đợt này. Migration 14 được cung cấp để người vận hành xem và áp dụng sau sao lưu.

Các kết quả QA trên máy độc lập kiểm tra mã nguồn đã sửa. Chúng không có nghĩa những sửa đổi đó tự xuất hiện trên Neon. Cũng chưa có chứng nhận đầy đủ cho từng thao tác giao diện, tải đồng thời lớn, hiệu quả kinh doanh của AI, giao nhận thông báo trên điện thoại, hay phục hồi thảm họa của Neon.

### Những lỗi đã sửa trong mã nguồn bàn giao

Seed tham chiếu khách theo username thay cho ID 31 không tồn tại khi dựng mới. Giá trị mẫu của phiên năm giờ ở mức 18.000 đồng/giờ được đồng bộ thành 90.000 đồng trong phiên, hóa đơn và payment. Migration báo cáo không còn GRANT cho role_technician đã bỏ khỏi thiết kế ứng dụng.

Ba hàm mở phiên, nạp tiền và gọi món được chuyển sang SECURITY DEFINER có search_path cố định, kèm kiểm tra employee actor. Migration cộng thêm 14 cho phép cập nhật các hàm trên database đang có dữ liệu. Listener dùng API phù hợp với psycopg 3. Bản SQL Cloud được tạo lại từ module nguồn, có transaction và tái tạo schema đầy đủ để tránh mất FK khi chạy lại bản demo.

Script QA dùng đường dẫn tương đối cho psql để tránh lỗi đường dẫn tiếng Việt trên Windows. Script migration phân biệt câu lệnh thực thi trực tiếp với thân function, tránh chặn nhầm DELETE nằm trong định nghĩa hàm hoàn tác khuyến mãi.

### Những hạn chế vẫn cần ghi đúng

Quy trình void hóa đơn chưa tự hoàn tiền; hủy đơn món chưa tự hoàn kho. Phần áp dụng ưu đãi còn cố định mã và người nhận, có lệch giữa tên giờ ưu đãi và điều kiện checkout. Những điều kiện target_customer_id chưa được kiểm tra đầy đủ. Gateway là ntfy push và chưa được kiểm tra gửi ra điện thoại trong đợt này.

Các test hiện hữu còn thiên về metadata và trường hợp thông thường. Không dùng kết quả 29/29 để tuyên bố mọi nghiệp vụ và mọi trường hợp tranh chấp đều đúng. Các log cũ trong repository thuộc các phiên bản trước; ưu tiên mã nguồn đang dùng và bằng chứng mới khi có khác biệt.

### Bản đồ nguồn để mở khi hội đồng hỏi

| Nội dung | File hoặc đối tượng nguồn |
|---|---|
| Bảng, kiểu, khóa và CHECK | sql/01_schema.sql; sql/08_station_customer.sql |
| ERD đủ thuộc tính | docs/defense-erd.md |
| Tính tiền và nghiệp vụ Admin | sql/02_functions_triggers.sql; sql/14_defense_demo_fixes.sql |
| Đăng nhập, heartbeat, checkout | sql/08_station_customer.sql |
| Đơn món, bảo trì, void, refresh báo cáo | sql/10_admin_operations.sql |
| Quyền và RLS | sql/04_security.sql; sql/12_security_hardening.sql |
| Chỉ mục và partition | sql/05_indexes_partition.sql |
| Năm module phân tích | sql/07_ai_modules.sql; sql/13_ai_demo_situations.sql là dữ liệu mẫu |
| ETL | python/generate_dirty_data.py; python/clean_data.py |
| Listener | python/listen_ai_alerts.py; python/notification_stream.py |
| Truy vấn của giao diện | app/NetCafe.Data và các Views trong app/NetCafe.App |
| Kiểm thử | scripts/run-qa.ps1; tests/verify_defense_roles.sql; app/NetCafe.Data.Tests |

### Checklist trước khi nộp và lên trình bày

1. Pull đúng commit bàn giao; cấu hình đúng nhánh Neon demo và kiểm tra current_database, current_user, server_version, TimeZone.
2. Nếu dùng database hiện có, xem backup và migration 14; không chạy nhầm file khởi tạo thay cho migration.
3. Tập toàn bộ phần khởi tạo, ETL, lỗi CHECK, SET ROLE, cột lương, EXPLAIN, JSONB và listener trên môi trường sẽ trình chiếu.
4. Chuẩn bị dữ liệu mẫu và tài khoản đủ cho từng tình huống; chỉ ghi nhận số liệu thật của lần chạy.
5. Mở sẵn ERD, mã nguồn liên quan và log; đóng màn hình hiển thị mật khẩu hoặc dữ liệu không cần trình chiếu.
6. Nộp báo cáo tiểu luận, kịch bản, SQL và Python; bảo đảm file .env và các bí mật không nằm trong commit.
