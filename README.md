# 🎮 NetCafe Management System - Database Administration (QTDL)

> **Môn học:** Quản trị Cơ sở Dữ liệu (QTDL) - Đồ án / Bài tập lớn Cuối kỳ  
> **Hệ quản trị CSDL:** PostgreSQL 17  
> **Ứng dụng quản trị:** C# / .NET 10 (Windows Forms Thin Client)  
> **Hỗ trợ ETL & Phân tích:** Python 3 (psycopg2, pandas, tabulate)  

---

## 📌 Tổng Quan Dự Án

Dự án tập trung vào thiết kế, triển khai, bảo mật và tối ưu hóa hệ thống cơ sở dữ liệu cho phòng Cyber Game / NetCafe quy mô lớn:
- **Chuẩn hóa dữ liệu:** 16 bảng nghiệp vụ đạt chuẩn 3NF, phân tách thành 8 Schemas chuyên biệt.
- **Ràng buộc toàn vẹn nâng cao:** Chống đặt trùng máy bằng `EXCLUDE USING gist` (khoảng thời gian `tstzrange`), kiểm tra dữ liệu bằng `CHECK`, `FOREIGN KEY` chặt chẽ.
- **Bảo mật đa tầng:**
  - Column-Level Security (ẩn lương nhân viên).
  - Row-Level Security (RLS) phân quyền theo từng thu ngân, quản lý, kỹ thuật viên.
  - Phân quyền DCL chuẩn Least Privilege.
- **Tối ưu hóa hiệu năng & DBA:**
  - B-Tree Index, GIN Trigram (`pg_trgm`), GIN `jsonb_path_ops`, Partial Index.
  - Partitioning theo tháng tự động cho bảng `audit.audit_log` kết hợp BRIN Index.
  - Kế hoạch Backup & Disaster Recovery tự động.
- **In-Database Intelligence (AI Modules):**
  - Cảnh báo gian lận thời gian thực qua `LISTEN/NOTIFY`.
  - Gợi ý chỉ mục tự động qua catalog `pg_stat_user_tables`.
  - Phát hiện khách hàng trùng lặp bằng thuật toán Trigram Similarity.
  - Phát hiện bất thường nạp tiền & phiên chơi bằng Z-score.
  - Khai phá dữ liệu gợi ý khuyến mãi Happy Hour & Voucher VIP.

---

## 📂 Cấu Trúc Thư Mục

```text
├── app/                  # Ứng dụng Desktop C# .NET 10 (WinForms Thin Client)
│   ├── NetCafe.App/      # Admin shell + các UserControl Designer
│   ├── NetCafe.Client/   # Station customer theo computer_code
│   ├── NetCafe.Data/     # Lớp truy cập dữ liệu (Npgsql), Admin/Station functions
│   └── NetCafe.Data.Tests/ # Bộ kiểm thử tích hợp xUnit trên netcafe_qa
├── backup/               # Script sao lưu tự động & Disaster Recovery
│   ├── backup.ps1 / .sh  # Script backup định kỳ
│   └── test_disaster_recovery.py # Kịch bản kiểm thử khôi phục thảm họa
├── demo/                 # Bộ script phục vụ trình diễn bảo vệ đồ án
│   ├── demo_1_init_constraints.sql # Demo ràng buộc toàn vẹn
│   ├── demo_2_security_rls.sql     # Demo phân quyền bảo mật & RLS
│   ├── demo_3_performance.sql      # Demo tối ưu hiệu năng & Partition
│   ├── demo_4_highlights.sql       # Demo 5 module In-Database AI
│   └── runbook.md                  # Kịch bản bảo vệ chi tiết (15 phút)
├── docs/                 # Tài liệu thiết kế & báo cáo
│   ├── architecture.md   # Thiết kế kiến trúc 8 Schemas
│   ├── constraints.md    # Đặc tả ràng buộc toàn vẹn
│   ├── erd.md            # Mô tả quan hệ thực thể
│   ├── erd_viewer.html   # Giao diện xem sơ đồ ERD trực quan
│   └── BaoCao_QuanTriCSDL_NetCafe.docx # Báo cáo hoàn chỉnh
├── evidence/             # Bằng chứng thực thi & log kết quả kiểm thử
├── python/               # Bộ công cụ ETL, làm sạch và tạo dữ liệu bẩn
│   ├── clean_data.py     # Quy trình làm sạch dữ liệu bẩn
│   ├── etl.py            # Quy trình ETL tự động
│   ├── generate_dirty_data.py # Tạo dữ liệu lỗi thử nghiệm
│   ├── generate_report.py# Xuất báo cáo tự động
│   └── measure_explain.py# Đo lường chi phí truy vấn trước/sau tối ưu
├── sql/                  # Bộ mã nguồn CSDL hoàn chỉnh
│   ├── 00_create_database.sql
│   ├── 01_schema.sql
│   ├── 02_functions_triggers.sql
│   ├── 03_views.sql
│   ├── 04_security.sql
│   ├── 05_indexes_partition.sql
│   ├── 06_seed_base.sql
│   ├── 07_ai_modules.sql
│   ├── 08_station_customer.sql # heartbeat, role station, support, wallet
│   └── netcafe_full.sql  # Toàn bộ mã nguồn hợp nhất 00–08, 10 và 11
├── scripts/
│   ├── reset-qa.ps1      # Chỉ reset database netcafe_qa
│   ├── run-qa.ps1        # reset + station contract + build + test
│   ├── run-station.ps1   # Chạy station bằng role giới hạn
│   └── run-station.cmd   # Wrapper cho Windows PowerShell 5.1
├── tests/                # Bộ kiểm thử SQL tự động
├── .env.example          # Mẫu cấu hình môi trường
└── .gitignore            # Cấu hình bỏ qua tệp tạm, secrets
```

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy

### 1. Chuẩn Bị Cơ Sở Dữ Liệu QA PostgreSQL

1. Sao chép cấu hình môi trường và tự điền credential ngoài source:
   ```powershell
   Copy-Item .env.example .env
   ```
   Chỉnh sửa thông tin kết nối trong file `.env` cho phù hợp với máy của bạn.

2. Reset duy nhất database QA, không chạy trên Neon chính:
   ```powershell
   $env:DB_NAME = "netcafe_qa"
   $env:DB_PASSWORD = "<cấp ngoài source>"
   powershell -ExecutionPolicy Bypass -File .\scripts\reset-qa.ps1
   ```
   Script áp dụng schema/migration/seed 01–08, 10 và 11 rồi ghi marker QA.

### 2. Chạy Admin và Station (C# .NET 10)

```powershell
dotnet run --project app/NetCafe.App
# Windows PowerShell 5.1 hoặc PowerShell 7 đều dùng được
.\scripts\run-station.cmd -ComputerCode PC001
```

`run-station.cmd` nạp `STATION_DB_USER`, `STATION_DB_PASSWORD` và `NETCAFE_COMPUTER_CODE` từ `.env`/environment trước khi chạy Client. Có thể dùng `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-station.ps1 -ComputerCode PC001` nếu không muốn dùng wrapper. Không chạy station bằng `DB_USER` owner.

Chạy bộ kiểm thử tích hợp:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run-qa.ps1
```

Lệnh QA reset duy nhất `netcafe_qa`, kiểm tra role station, login/heartbeat/order/support/logout và chạy xUnit. Không dùng lệnh này cho database chính.

Station đọc `computer_code` từ `station.json` hoặc `NETCAFE_COMPUTER_CODE`; role `station_runtime` và password phải được cấp ngoài source.

### 3. Chạy Quy Trình Python (ETL & Báo Cáo)

```powershell
cd python
pip install -r requirements.txt
python etl.py
python generate_report.py
```

### 4. Kịch Bản Demo Trình Diễn

Xem chi tiết từng bước trong [`demo/runbook.md`](file:///c:/Users/Administrator/Documents/QTDL%20-%20Cuối%20kỳ/demo/runbook.md) với 4 kịch bản SQL có sẵn trong [`demo/`](file:///c:/Users/Administrator/Documents/QTDL%20-%20Cuối%20kỳ/demo/).

---

## 🧭 Tài liệu kiến trúc và QA

- [`docs/code-map.md`](docs/code-map.md): map file nguồn/config/script/test/doc.
- [`docs/bug-register.md`](docs/bug-register.md): triệu chứng, nguyên nhân, sửa và bằng chứng.
- [`docs/optimization-report.md`](docs/optimization-report.md): refresh gate, heartbeat và cách đo EXPLAIN.
- [`docs/ai-modules.md`](docs/ai-modules.md): contract 5 module AI PostgreSQL.
- [`tests/station_customer.sql`](tests/station_customer.sql): assertion contract station chỉ đọc trên QA.
- [`demo/runbook.md`](demo/runbook.md): live demonstration Admin, Station, Support và AI.

## 🛡️ Tác Quyền & Đóng Góp

Dự án thực hiện cho môn học **Quản trị Cơ sở Dữ liệu**. Mọi mã nguồn được tổ chức theo tiêu chuẩn thực hành tốt (Best Practices) của PostgreSQL và kỹ thuật phần mềm.
