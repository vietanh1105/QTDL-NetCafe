# Code map NetCafe

Mỗi dòng dưới đây mô tả một file nguồn, cấu hình, script, test hoặc tài liệu đang được dùng trong solution; `bin/` và `obj/` là artefact build, không đưa vào map.

## Solution và Admin

- `app/NetCafe.sln`: solution Visual Studio legacy, gồm Admin, Data, test và Station Client.
- `app/NetCafe.slnx`: solution mới dùng cho restore/build CI.
- `app/NetCafe.App/NetCafe.App.csproj`: project WinForms Admin .NET 10 và dependency Guna/Npgsql.
- `app/NetCafe.App/Program.cs`: entry point khởi tạo WinForms Admin.
- `app/NetCafe.App/AppConfig.cs`: đọc connection config từ environment/.env, không giữ credential thật.
- `app/NetCafe.App/Controls/ComputerCardControl.cs`: card trạng thái từng máy, màu status và event chi tiết.
- `app/NetCafe.App/Controls/ComputerCardControl.Designer.cs`: layout Designer của card máy.
- `app/NetCafe.App/MainForm.cs`: shell lifecycle, navigation, role switch, listener và refresh gate.
- `app/NetCafe.App/MainForm.Designer.cs`: header, sidebar, content, error banner và timer của shell.
- `app/NetCafe.App/MainForm.resx`: resource WinForms hiện có trong checkout, không phải logic dữ liệu.
- `app/NetCafe.App/Views/AdminViewContract.cs`: interface view và context dùng chung.
- `app/NetCafe.App/Views/AdminViewBase.cs`: UserControl nền, màu sắc, grid và error forwarding.
- `app/NetCafe.App/Views/AdminDataViewBase.cs`: toolbar/grid dùng lại cho các view dữ liệu.
- `app/NetCafe.App/Views/ComputersView.cs`: sơ đồ máy, session và refresh theo ComputerId.
- `app/NetCafe.App/Views/ComputersView.Designer.cs`: metadata Designer của ComputersView.
- `app/NetCafe.App/Views/CustomersView.cs`: tìm khách, CRUD, reset mật khẩu, bật/tắt và nạp tiền qua repository.
- `app/NetCafe.App/Views/CustomersView.Designer.cs`: metadata Designer của CustomersView.
- `app/NetCafe.App/Views/OrdersView.cs`: menu gọi món, ảnh SKU, nhập tồn kho và hàng đợi bếp.
- `app/NetCafe.App/Views/OrdersView.Designer.cs`: metadata Designer của OrdersView.
- `app/NetCafe.App/Views/InvoicesView.cs`: master-detail hóa đơn, void có audit và export PDF/Excel.
- `app/NetCafe.App/Views/InvoicesView.Designer.cs`: metadata Designer của InvoicesView.
- `app/NetCafe.App/Views/ReportsView.cs`: dashboard doanh thu/hiệu suất/top hội viên và refresh snapshot.
- `app/NetCafe.App/Views/ReportsView.Designer.cs`: metadata Designer của ReportsView.
- `app/NetCafe.App/Views/EmployeesView.cs`: CRUD nhân viên, mapping db_username và hiển thị lương theo quyền.
- `app/NetCafe.App/Views/EmployeesView.Designer.cs`: metadata Designer của EmployeesView.
- `app/NetCafe.App/Views/MaintenanceView.cs`: lập/hoàn tất phiếu bảo trì và đồng bộ trạng thái máy.
- `app/NetCafe.App/Views/MaintenanceView.Designer.cs`: metadata Designer của MaintenanceView.
- `app/NetCafe.App/Views/AiCenterView.cs`: bảng điều khiển 5 module AI nội bộ PostgreSQL.
- `app/NetCafe.App/Views/AiCenterView.Designer.cs`: metadata Designer của AiCenterView.
- `app/NetCafe.App/Views/SupportQueueView.cs`: queue request từ station.
- `app/NetCafe.App/Views/SupportQueueView.Designer.cs`: metadata Designer của SupportQueueView.
- `app/NetCafe.App/Views/SettingsView.cs`: hiển thị role, employee context và QA mode.
- `app/NetCafe.App/Views/SettingsView.Designer.cs`: metadata Designer của SettingsView.

## Data access và DTO

- `app/NetCafe.Data/NetCafe.Data.csproj`: project thư viện Npgsql dùng chung.
- `app/NetCafe.Data/Class1.cs`: file khung cũ của project, không chứa luồng nghiệp vụ mới.
- `app/NetCafe.Data/DbRepository.cs`: query Admin hiện hữu, SQL logger và LISTEN `ai_security_alert`.
- `app/NetCafe.Data/DatabaseMetadataRepository.cs`: preflight host/database/user, migration 08 và function capability.
- `app/NetCafe.Data/CustomerAdminRepository.cs`: CRUD/reset/kích hoạt khách hàng, hash mật khẩu trong DB.
- `app/NetCafe.Data/AiRepository.cs`: đọc snapshot AI và chạy module chỉ theo nút Admin.
- `app/NetCafe.Data/AdminQueries.cs`: support status và ba reporting query.
- `app/NetCafe.Data/AdminOperationsRepository.cs`: void, export data, refresh snapshot, stock, maintenance và employee operations.
- `app/NetCafe.Data/StationRepository.cs`: function calls cho station, reconcile và mapping kết quả station.
- `app/NetCafe.Data/Models.cs`: DTO Admin và reporting.
- `app/NetCafe.Data/StationModels.cs`: DTO login, heartbeat, order, support và AI.

## Station Client

- `app/NetCafe.Client/NetCafe.Client.csproj`: WinForms station .NET 10 tham chiếu NetCafe.Data.
- `app/NetCafe.Client/Program.cs`: entry point station.
- `app/NetCafe.Client/MainForm.cs`: điều phối Login → Session → Order/Support → Logout.
- `app/NetCafe.Client/MainForm.Designer.cs`: shell station và vùng nội dung.
- `app/NetCafe.Client/StationConfig.cs`: đọc `computer_code`, connection và heartbeat config.
- `app/NetCafe.Client/StationSessionController.cs`: nơi duy nhất chạy heartbeat 30 giây.
- `app/NetCafe.Client/Views/LoginView.cs`: nhập customer username/password.
- `app/NetCafe.Client/Views/LoginView.Designer.cs`: layout login.
- `app/NetCafe.Client/Views/SessionView.cs`: thời gian chơi, số dư và chi phí dự kiến.
- `app/NetCafe.Client/Views/SessionView.Designer.cs`: layout session.
- `app/NetCafe.Client/Views/FoodOrderView.cs`: menu và gửi đơn F&B.
- `app/NetCafe.Client/Views/FoodOrderView.Designer.cs`: layout F&B.
- `app/NetCafe.Client/Views/SupportRequestView.cs`: gửi request hỗ trợ.
- `app/NetCafe.Client/Views/SupportRequestView.Designer.cs`: layout support.
- `app/NetCafe.Client/Views/LogoutView.cs`: contract mở rộng cho xác nhận logout.
- `app/NetCafe.Client/Views/LogoutView.Designer.cs`: metadata Designer logout.
- `app/NetCafe.Client/station.json.example`: mẫu cấu hình station không có password.

## SQL và Python

- `sql/00_create_database.sql`: database demo legacy `netcafe_db`; không dùng trong QA reset.
- `sql/01_schema.sql`: schema, bảng và extension nền.
- `sql/02_functions_triggers.sql`: function nghiệp vụ Admin và trigger.
- `sql/03_views.sql`: reporting views/materialized views.
- `sql/04_security.sql`: role, RLS và column privilege Admin.
- `sql/05_indexes_partition.sql`: index, partition và tối ưu catalog.
- `sql/06_seed_base.sql`: seed dữ liệu chuẩn.
- `sql/07_ai_modules.sql`: 5 module AI PL/pgSQL và notification.
- `sql/08_station_customer.sql`: migration idempotent cho station, heartbeat, wallet, support và role giới hạn.
- `sql/netcafe_full.sql`: file tổng hợp 00–08 được tạo bởi build_full_sql.py.
- `python/build_full_sql.py`: ghép SQL theo thứ tự, gồm migration 08.
- `python/apply_sql_neon.py`: áp dụng file SQL theo connection do environment cấp.
- `python/etl.py`: ETL dữ liệu bẩn và activity fixture.
- `python/generate_dirty_data.py`: sinh dữ liệu bẩn cho staging.
- `python/clean_data.py`: chuẩn hóa và nạp dữ liệu hợp lệ.
- `python/generate_report.py`: sinh báo cáo đồ án hiện hữu.
- `python/measure_explain.py`: chạy EXPLAIN thực tế trên DB đã cấu hình.
- `python/requirements.txt`: dependency Python ETL/report.

## QA, backup và tài liệu

- `scripts/reset-qa.ps1`: chỉ drop/create `netcafe_qa`, strip psql connect directive và apply 01–08.
- `scripts/run-qa.ps1`: reset QA, build solution và chạy xUnit.
- `scripts/run-station.cmd`: wrapper Windows PowerShell 5.1 để chạy station khi máy không có `pwsh`.
- `scripts/migrate-main.ps1`: backup/preflight và chỉ áp dụng migration 08 trên `netcafe_db`.
- `scripts/run-station.ps1`: nạp station credential từ `.env` và chạy Client theo `computer_code`.
- `tests/station_customer.sql`: assertion SQL cho contract station/RLS/AI, chỉ chạy trên QA.
- `tests/verify_qa_station_prepare.sql`: tạo fixture station `test_station_qa` trên QA.
- `tests/verify_qa_station.sql`: kiểm tra station role, login, heartbeat, F&B, support và logout.
- `tests/verify_qa_station_assert.sql`: owner kiểm tra máy available và payment sau logout.
- `tests/verify_qa_ai.sql`: kiểm chứng Security Alert và bốn function AI còn lại trên QA.
- `app/NetCafe.Data.Tests/NetCafe.Data.Tests.csproj`: project xUnit integration.
- `app/NetCafe.Data.Tests/IntegrationTests.cs`: integration test có guard database `netcafe_qa`.
- `app/NetCafe.Data.Tests/AssemblyInfo.cs`: tắt chạy song song để fixture test_xunit_* không tranh chấp máy.
- `tests/verify_main_station.py`: test main có fixture `test_station_*`, privilege station và login/heartbeat/logout.
- `scripts/migrate-main.ps1`: backup/preflight và chỉ áp dụng migration 08 trên `netcafe_db`.
- `backup/backup.ps1`: backup PostgreSQL Windows, password lấy từ PGPASSWORD.
- `backup/backup.sh`: backup PostgreSQL Linux, password lấy từ PGPASSWORD.
- `backup/test_disaster_recovery.py`: kiểm thử restore với DB do environment cấp.
- `demo/runbook.md`: kịch bản live demo QA/Admin/Station/AI.
- `docs/architecture.md`: kiến trúc database hiện hữu.
- `docs/constraints.md`: đặc tả ràng buộc dữ liệu.
- `docs/erd.md`: mô tả ERD.
- `docs/erd_viewer.html`: viewer ERD.
- `docs/bug-register.md`: bug register và bằng chứng.
- `docs/optimization-report.md`: báo cáo tối ưu trước/sau và giới hạn bằng chứng.
- `docs/ai-modules.md`: contract 5 module AI.
- `docs/BaoCao_QuanTriCSDL_NetCafe.docx`: báo cáo học phần hiện hữu.
- `README.md`: hướng dẫn tổng quan repository.
- `.env.example`: mẫu biến môi trường không chứa credential thật.
