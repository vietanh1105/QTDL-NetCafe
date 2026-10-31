import docx
from scripts.docx_styles import (
    add_heading_1, add_heading_2, add_speech_box, 
    add_code_table, add_result_table, add_explanation_box
)

def add_part_1(doc):
    """Xây dựng Phần 1: Khởi tạo, ETL & Ràng buộc toàn vẹn."""
    add_heading_1(doc, "PHẦN 1: KHỞI TẠO, ETL VÀ RÀNG BUỘC TOÀN VẸN (FAIL-SAFE ARCHITECTURE)")
    
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Nhà máy lọc nước và Cửa an ninh sân bay)",
        "Dữ liệu người dùng nạp vào như nguồn nước thô chưa lọc. Tầng Staging đóng vai trò như bể lắng "
        "và hệ thống lọc (ETL Pipeline): các bản ghi lỗi hoặc có dấu hiệu gian lận bị giữ lại ở bể cách ly "
        "(staging.rejected_rows), chỉ dữ liệu sạch đạt chuẩn 3NF mới được nạp vào bể chính (core.customer). "
        "Đồng thời, Check Constraint và Foreign Key đóng vai trò như cửa an ninh sân bay: hành lý sai quy cách "
        "hoặc vé không hợp lệ đều bị chặn đứng ngay lập tức tại tầng nhân CSDL."
    )
    
    # Bước 1.1
    add_heading_2(doc, "Bước 1.1: Khởi tạo CSDL Cloud sạch sẽ tự động bằng Python")
    add_speech_box(
        doc,
        "Kính thưa Thầy Cô và Hội đồng, trước tiên trên cửa sổ Terminal PowerShell bên phải, nhóm chạy lệnh: "
        "python python/reset_and_init_cloud.py --init. Lệnh này kết nối trực tiếp lên Neon PostgreSQL Cloud, "
        "xóa sạch các bảng cũ và tự động tái lập hoàn chỉnh 8 Schemas, toàn bộ các bảng, ràng buộc và dữ liệu gốc "
        "trong vòng chưa đầy 10 giây. Trên màn hình Terminal, Thầy Cô có thể thấy các dòng thông báo xanh xác nhận "
        "kết nối thành công, 8 Schemas và dữ liệu gốc đã sẵn sàng."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: TERMINAL POWERSHELL",
        "python python/reset_and_init_cloud.py --init"
    )
    add_result_table(
        doc,
        ["Trạng thái thực thi trên Terminal", "Chi tiết đối tượng CSDL được khởi tạo"],
        [
            ["[+] KẾT NỐI NEON CLOUD THÀNH CÔNG", "Host: ep-blue-cherry-b8mrp18t.c-14.us-east-1.aws.neon.tech"],
            ["[+] HOÀN TẤT KHỞI TẠO 8 SCHEMAS", "core, inventory, billing, hr, reporting, staging, audit, ai"],
            ["[+] NẠP BẢN GHI DỮ LIỆU GỐC (SEED)", "25+ Bảng, Triggers, Views, RLS Policies và 5 Module AI sẵn sàng"]
        ]
    )
    
    # Bước 1.2
    add_heading_2(doc, "Bước 1.2: Sinh dữ liệu thô (Dirty Data) và Chạy luồng ETL chuẩn hóa")
    add_speech_box(
        doc,
        "Tiếp theo trên Terminal, nhóm chạy lệnh python python/generate_dirty_data.py để sinh 20 dòng dữ liệu bẩn "
        "mô phỏng thực tế (viết hoa thường lộn xộn, dính khoảng trắng, sai đầu số +84, số dư tiền âm). "
        "Sau đó, nhóm chạy python python/clean_data.py để kích hoạt tiến trình ETL làm sạch. "
        "Bây giờ, nhóm chuyển sang cửa sổ Neon SQL bên trái để đối chiếu 3 bước: "
        "Thứ nhất: Xem 10 dòng thô ở Staging có đầy đủ các lỗi thực tế; "
        "Thứ hai: Xem bảng cách ly staging.rejected_rows đã chặn đúng 4 dòng lỗi; "
        "Thứ ba: Xem 16 khách hàng sạch đã được chuẩn hóa đạt chuẩn 3NF và nạp thành công vào bảng core.customer."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: TERMINAL POWERSHELL",
        "# 1. Sinh 20 dòng dữ liệu bẩn vào bảng tạm staging:\n"
        "python python/generate_dirty_data.py\n\n"
        "# 2. Chạy luồng ETL làm sạch và nạp dữ liệu:\n"
        "python python/clean_data.py"
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "-- 1. Xem 10 dòng dữ liệu thô bị lỗi (+84, dấu chấm, tiền âm, in hoa):\n"
        "SELECT raw_id, raw_full_name, raw_phone, raw_balance, source_batch \n"
        "FROM staging.stg_customers ORDER BY raw_id LIMIT 10;\n\n"
        "-- 2. Xem bảng cách ly dữ liệu bẩn (Rejected Rows):\n"
        "SELECT rejected_id, error_reason, raw_record FROM staging.rejected_rows;\n\n"
        "-- 3. Xem 16 khách hàng sạch đã được chuẩn hóa và nạp thành công vào CSDL chính:\n"
        "SELECT customer_id, full_name, phone, email, username, balance\n"
        "FROM core.customer\n"
        "WHERE username LIKE 'user_%'\n"
        "ORDER BY customer_id\n"
        "LIMIT 16;"
    )
    add_result_table(
        doc,
        ["raw_id", "raw_full_name (Tên thô)", "raw_phone (SĐT lỗi)", "raw_balance", "Ghi chú lỗi thực tế trên Neon"],
        [
            ["1", "Phạm Văn Khánh", "+84977100001", "50000", "Lỗi đầu số quốc tế +84"],
            ["2", "   lê   thanh   phong   ", "0977.100.002", "20000", "Lỗi chữ thường, thừa khoảng trắng, dính dấu chấm"],
            ["3", "   VŨ NGỌC VINH   ", "0977100003", "500000", "Lỗi viết HOA toàn bộ"],
            ["4", "Vũ Tuấn Tùng", "0977100004", "-50000", "Lỗi nghiêm trọng: Số dư tiền âm (-50.000)"],
            ["5", "Ngô Minh Khánh", "12345", "50000", "Lỗi độ dài SĐT không hợp lệ (12345)"],
            ["6", "  pHạM   tHị   nAm  ", "0977100006", "100000", "Lỗi chữ hoa thường xen kẽ, thừa khoảng trắng"],
            ["7", "võ xuân bình", "0977.100.007", "500000", "Lỗi dính dấu chấm trong SĐT, chữ thường"],
            ["8", "Lê Ngọc Linh", "0977100008", "-100000", "Lỗi nghiêm trọng: Số dư tiền âm (-100.000)"],
            ["9", "Phan Hải Cường", "8888", "50000", "Lỗi độ dài SĐT không hợp lệ (8888)"],
            ["10", "   LÊ THÀNH HUY   ", "0977100010", "200000", "Lỗi viết HOA và thừa khoảng trắng"]
        ]
    )
    add_result_table(
        doc,
        ["rejected_id", "error_reason", "raw_record (Bản ghi lỗi bị cách ly thực tế trên Neon)"],
        [
            ["1", "NEGATIVE_BALANCE", "{'name': 'Vũ Tuấn Tùng', 'phone': '0977100004', 'balance': '-50000'}"],
            ["2", "PHONE_FORMAT_INVALID", "{'name': 'Ngô Minh Khánh', 'phone': '12345', 'balance': '50000'}"],
            ["3", "NEGATIVE_BALANCE", "{'name': 'Lê Ngọc Linh', 'phone': '0977100008', 'balance': '-100000'}"],
            ["4", "PHONE_FORMAT_INVALID", "{'name': 'Phan Hải Cường', 'phone': '8888', 'balance': '50000'}"]
        ]
    )
    add_result_table(
        doc,
        ["customer_id", "full_name (Đã chuẩn hóa)", "phone", "email", "username", "balance"],
        [
            ["15", "Phạm Văn Khánh", "0977100001", "phmvnkhn41@gmail.com", "user_1_0001", "50.000 VNĐ"],
            ["16", "Lê Thanh Phong", "0977100002", "lthanhph14@gmail.com", "user_2_0002", "20.000 VNĐ"],
            ["17", "Vũ Ngọc Vinh", "0977100003", "vngcvinh13@gmail.com", "user_3_0003", "500.000 VNĐ"],
            ["18", "Phạm Thị Nam", "0977100006", "phamthinam@gmail.com", "user_6_0006", "100.000 VNĐ"],
            ["19", "Võ Xuân Bình", "0977100007", "vxunbnh68@gmail.com", "user_7_0007", "500.000 VNĐ"],
            ["20", "Lê Thành Huy", "0977100010", "lthnhhuy22@gmail.com", "user_10_0010", "200.000 VNĐ"],
            ["...", "16 Khách hàng sạch", "Đạt chuẩn 3NF", "Email hợp lệ", "Tạo tài khoản", "Số dư dương"]
        ]
    )
    
    # Bước 1.3
    add_heading_2(doc, "Bước 1.3: Thử nghiệm rủi ro Fail-Safe (Chặn lỗi ràng buộc ở tầng nhân CSDL)")
    add_speech_box(
        doc,
        "Để chứng minh cơ chế Fail-Safe bảo vệ ở tầng nhân CSDL chứ không chỉ phụ thuộc vào code ứng dụng, "
        "trên Neon SQL nhóm cố tình chạy trực tiếp 2 câu lệnh INSERT vi phạm: "
        "Dòng lệnh 1 chèn khách hàng số dư âm -50.000 VNĐ, CSDL bắn lỗi vi phạm ràng buộc CHECK ck_customer_balance. "
        "Dòng lệnh 2 chèn hóa đơn với mã khách 9999 không tồn tại, CSDL từ chối và báo lỗi vi phạm khóa ngoại "
        "FOREIGN KEY invoice_customer_id_fkey. CSDL bảo vệ toàn vẹn tuyệt đối dữ liệu."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "-- 1. Thử chèn khách hàng số dư âm -> Vi phạm CHECK (balance >= 0):\n"
        "INSERT INTO core.customer (full_name, phone, email, username, balance)\n"
        "VALUES ('Test Check Error', '0988888888', 'test@gmail.com', 'test_fail', -50000);\n\n"
        "-- 2. Thử chèn hóa đơn với ID khách không tồn tại -> Vi phạm FOREIGN KEY:\n"
        "INSERT INTO billing.invoice (session_id, customer_id, cashier_id, total_amount, status)\n"
        "VALUES (9999, 9999, 9999, 100000, 'unpaid');"
    )
    add_result_table(
        doc,
        ["Lệnh thử nghiệm vi phạm", "Thông báo lỗi thực tế trả về từ PostgreSQL (Neon Cloud)"],
        [
            ["INSERT số dư -50.000 VNĐ", "ERROR: new row for relation \"customer\" violates check constraint \"ck_customer_balance\" (Failing row: balance = -50000)"],
            ["INSERT hóa đơn khách 9999", "ERROR: insert or update on table \"invoice\" violates foreign key constraint \"invoice_customer_id_fkey\" (customer_id=9999 not present)"]
        ]
    )

def add_part_2(doc):
    """Xây dựng Phần 2: Bảo mật phân quyền & Module AI 3."""
    add_heading_1(doc, "PHẦN 2: BẢO MẬT PHÂN QUYỀN ĐA TẦNG (RBAC, CLS, RLS) VÀ MODULE AI 3")
    
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Két tiền ca trực và Bảng lương giám đốc)",
        "Thu ngân ca trực chỉ được mở và kiểm đếm ngăn két của chính mình trong ca, tuyệt đối không được nhìn "
        "ngăn két của thu ngân ca khác (đó là RLS - Row-Level Security). Đồng thời thu ngân chỉ có quyền xem danh bạ "
        "nội bộ để liên lạc ca kíp chứ không thể xem lén cột tiền lương của đồng nghiệp và quản lý (đó là CLS - Column-Level Security)."
    )
    
    # Bước 2.1
    add_heading_2(doc, "Bước 2.1: Đóng vai Nhân viên Thu ngân Thảo (cashier_id = 3)")
    add_speech_box(
        doc,
        "Bây giờ trên cửa sổ Neon SQL, nhóm đóng vai nhân viên thu ngân Thảo (cashier_id = 3) bằng lệnh SET ROLE nv_thao. "
        "Hệ thống trả về thông báo xác nhận phiên làm việc đã được chuyển sang vai trò thu ngân Thảo (CURRENT_USER là nv_thao)."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n"
        "SELECT \n"
        "    'ĐÃ CHUYỂN THÀNH CÔNG SANG VAI TRÒ THU NGÂN (THẢO)' AS thong_bao_he_thong,\n"
        "    CURRENT_USER AS vai_tro_hien_tai, \n"
        "    SESSION_USER AS tai_khoan_ket_noi;"
    )
    add_result_table(
        doc,
        ["thong_bao_he_thong", "vai_tro_hien_tai", "tai_khoan_ket_noi"],
        [
            ["ĐÃ CHUYỂN THÀNH CÔNG SANG VAI TRÒ THU NGÂN (THẢO)", "nv_thao", "neondb_owner"]
        ]
    )
    
    # Bước 2.2
    add_heading_2(doc, "Bước 2.2: Chứng minh Bảo mật cấp hàng (Row-Level Security - RLS)")
    add_speech_box(
        doc,
        "Tiếp theo, thu ngân Thảo thực hiện câu lệnh SELECT invoice_id, customer_id, cashier_id, total_amount, status "
        "FROM billing.invoice. Mặc dù câu truy vấn không hề có mệnh đề WHERE, nhờ chính sách RLS ở tầng nhân CSDL, "
        "Thảo chỉ thấy duy nhất 6 hóa đơn (ID: 1, 2, 3, 7, 8, 9) do chính cashier_id = 3 của mình lập, "
        "hoàn toàn bị ẩn các hóa đơn số 4, 5, 6 của nhân viên Nam."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice;"
    )
    add_result_table(
        doc,
        ["invoice_id", "customer_id", "cashier_id", "total_amount", "status", "Nhận định an ninh RLS"],
        [
            ["1", "1", "3 (Phạm Thu Thảo)", "16.000 VNĐ", "paid", "Đúng người lập"],
            ["2", "2", "3 (Phạm Thu Thảo)", "16.000 VNĐ", "paid", "Đúng người lập"],
            ["3", "3", "3 (Phạm Thu Thảo)", "16.000 VNĐ", "paid", "Đúng người lập"],
            ["7", "3", "3 (Phạm Thu Thảo)", "72.000 VNĐ", "paid", "Đúng người lập"],
            ["8", "8", "3 (Phạm Thu Thảo)", "630.000 VNĐ", "paid", "Đúng người lập"],
            ["9", "12", "3 (Phạm Thu Thảo)", "90.000 VNĐ", "paid", "Chỉ thấy đúng 6 hóa đơn của Thảo"]
        ]
    )
    
    # Bước 2.3
    add_heading_2(doc, "Bước 2.3: Đổi vai sang Quản lý (Manager) để đối chiếu toàn quyền giám sát")
    add_speech_box(
        doc,
        "Để đối chiếu, nhóm thực hiện RESET ROLE và chuyển sang vai trò Quản lý bằng SET ROLE nv_manager. "
        "Quản lý có đặc quyền BYPASSRLS nên ở câu truy vấn thứ hai, Quản lý nhìn thấy toàn bộ 9 hóa đơn của cả nhân viên Thảo "
        "(hóa đơn 1, 2, 3, 7, 8, 9) và nhân viên Nam (hóa đơn 4, 5, 6). Ở câu truy vấn thứ ba, Quản lý xem bảng tổng hợp "
        "doanh thu theo từng ca trực: Thảo thu 6 hóa đơn đạt 838.000 VNĐ, Nam thu 3 hóa đơn đạt 72.000 VNĐ."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "RESET ROLE;\n"
        "SET ROLE nv_manager;\n\n"
        "-- 1. Thông báo xác nhận chuyển vai trò Quản lý:\n"
        "SELECT 'ĐÃ CHUYỂN THÀNH CÔNG SANG VAI TRÒ QUẢN LÝ (MANAGER)' AS thong_bao_he_thong;\n\n"
        "-- 2. Quản lý xem toàn bộ chi tiết từng hóa đơn của tất cả nhân viên:\n"
        "SELECT \n"
        "    i.invoice_id AS ma_hoa_don,\n"
        "    i.issued_at AS thoi_gian_lap,\n"
        "    i.total_amount AS tong_tien,\n"
        "    i.status AS trang_thai,\n"
        "    i.cashier_id AS ma_nhan_vien,\n"
        "    e.full_name AS ten_nhan_vien_lap,\n"
        "    e.emp_role AS vai_tro_nhan_vien,\n"
        "    e.default_shift AS ca_lam_viec\n"
        "FROM billing.invoice i\n"
        "JOIN hr.employee e ON i.cashier_id = e.employee_id\n"
        "ORDER BY i.invoice_id;\n\n"
        "-- 3. Bảng tổng hợp doanh thu theo từng nhân viên ca trực:\n"
        "SELECT \n"
        "    i.cashier_id AS ma_nhan_vien, \n"
        "    e.full_name AS ten_nhan_vien, \n"
        "    e.username AS tai_khoan, \n"
        "    e.phone AS so_dien_thoai,\n"
        "    e.emp_role AS vai_tro, \n"
        "    e.default_shift AS ca_lam_viec, \n"
        "    count(i.invoice_id) AS so_luong_hoa_don,\n"
        "    sum(i.total_amount) AS tong_doanh_thu\n"
        "FROM billing.invoice i\n"
        "JOIN hr.employee e ON i.cashier_id = e.employee_id\n"
        "GROUP BY i.cashier_id, e.full_name, e.username, e.phone, e.emp_role, e.default_shift\n"
        "ORDER BY i.cashier_id;\n\n"
        "RESET ROLE;"
    )
    add_result_table(
        doc,
        ["ma_hoa_don", "tong_tien", "trang_thai", "ma_nhan_vien", "ten_nhan_vien_lap", "Quyền hạn BypassRLS"],
        [
            ["1", "16.000 VNĐ", "paid", "3", "Phạm Thu Thảo", "Quản lý thấy toàn bộ"],
            ["2", "16.000 VNĐ", "paid", "3", "Phạm Thu Thảo", "Quản lý thấy toàn bộ"],
            ["3", "16.000 VNĐ", "paid", "3", "Phạm Thu Thảo", "Quản lý thấy toàn bộ"],
            ["4", "24.000 VNĐ", "paid", "4", "Trần Đức Nam", "Thấy hóa đơn của Nam (Thu ngân Thảo bị ẩn)"],
            ["5", "24.000 VNĐ", "paid", "4", "Trần Đức Nam", "Thấy hóa đơn của Nam (Thu ngân Thảo bị ẩn)"],
            ["6", "24.000 VNĐ", "paid", "4", "Trần Đức Nam", "Thấy hóa đơn của Nam (Thu ngân Thảo bị ẩn)"],
            ["7", "72.000 VNĐ", "paid", "3", "Phạm Thu Thảo", "Quản lý thấy toàn bộ"],
            ["8", "630.000 VNĐ", "paid", "3", "Phạm Thu Thảo", "Quản lý thấy toàn bộ"],
            ["9", "90.000 VNĐ", "paid", "3", "Phạm Thu Thảo", "Quản lý thấy toàn bộ (Đủ 9 hóa đơn)"]
        ]
    )
    add_result_table(
        doc,
        ["ma_nhan_vien", "ten_nhan_vien", "tai_khoan", "so_dien_thoai", "vai_tro", "ca_lam_viec", "so_luong_hoa_don", "tong_doanh_thu"],
        [
            ["3", "Phạm Thu Thảo", "cashier_thao", "0903333333", "cashier", "morning", "6 hóa đơn", "838.000 VNĐ"],
            ["4", "Trần Đức Nam", "cashier_nam", "0904444444", "cashier", "afternoon", "3 hóa đơn", "72.000 VNĐ"]
        ]
    )
    
    # Bước 2.4
    add_heading_2(doc, "Bước 2.4: Chứng minh Bảo mật cấp cột (Column-Level Security - CLS)")
    add_speech_box(
        doc,
        "Tiếp theo, thu ngân Thảo chuyển lại vai trò SET ROLE nv_thao và cố tình truy cập trực tiếp vào cột salary "
        "(tiền lương) của bảng nhân viên. Hệ thống lập tức kích hoạt cơ chế CLS và trả về lỗi từ chối truy cập "
        "(ERROR: permission denied for table employee)."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n\n"
        "SELECT employee_id, full_name, salary \n"
        "FROM hr.employee;\n\n"
        "RESET ROLE;"
    )
    add_result_table(
        doc,
        ["Hành vi cố tình truy cập", "Kết quả thực tế trả về từ PostgreSQL (Neon Cloud)"],
        [
            ["SELECT employee_id, full_name, salary FROM hr.employee;", "ERROR: permission denied for table employee (SQLSTATE 42501 - Chặn xem cột lương nhạy cảm)"]
        ]
    )
    
    # Bước 2.5
    add_heading_2(doc, "Bước 2.5: Thu ngân truy cập an toàn qua View công khai (Đã che giấu lương)")
    add_speech_box(
        doc,
        "Thay vào đó, thu ngân Thảo được cấp quyền tra cứu qua View an toàn hr.vw_employee_public. "
        "Lệnh này trả về đầy đủ 5 nhân sự, số điện thoại và ca trực để phối hợp công việc mà cột lương đã bị giấu hoàn toàn."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n\n"
        "SELECT * FROM hr.vw_employee_public;\n\n"
        "RESET ROLE;"
    )
    add_result_table(
        doc,
        ["employee_id", "full_name", "phone", "emp_role", "default_shift", "Trạng thái hiển thị cột salary"],
        [
            ["1", "Nguyễn Văn Quản Lý", "0901111111", "manager", "morning", "ĐÃ ẨN HOÀN TOÀN (Bảo mật)"],
            ["2", "Lê Thị Thu Ngân", "0902222222", "cashier", "morning", "ĐÃ ẨN HOÀN TOÀN (Bảo mật)"],
            ["3", "Phạm Thu Thảo", "0903333333", "cashier", "morning", "ĐÃ ẨN HOÀN TOÀN (Bảo mật)"],
            ["4", "Trần Đức Nam", "0904444444", "cashier", "afternoon", "ĐÃ ẨN HOÀN TOÀN (Bảo mật)"],
            ["5", "Hoàng Văn Kỹ Thuật", "0905555555", "technician", "night", "ĐÃ ẨN HOÀN TOÀN (Bảo mật)"]
        ]
    )
    
    # Bước 2.6
    add_heading_2(doc, "Bước 2.6: Module AI 3 - Bắt trùng lặp tài khoản khách hàng bằng pg_trgm")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Bào mã khuyến mãi tân thủ)",
        "Nhiều khách hàng cố tình tạo nhiều nick như 'Tran Van Binh' và 'Trần Văn Bình' hoặc dùng chung 7 số cuối điện thoại "
        "để bào mã giảm giá tân thủ. Module AI 3 ứng dụng giải thuật Fuzzy Trigram Matching (pg_trgm) để tính toán "
        "độ tương đồng chuỗi và tự động gắn cờ gian lận danh tính."
    )
    add_speech_box(
        doc,
        "Nhóm thực thi hàm ai.fn_detect_duplicates(0.65). AI lập tức rà soát toàn bộ khách hàng và phát hiện chính xác "
        "4 cặp tài khoản nghi vấn trên Neon: cặp trùng 7 số cuối điện thoại và tên tương đồng cao, và cặp họ tên không dấu "
        "Tran Van Binh tương đồng 100% với tên có dấu Trần Văn Bình."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason \n"
        "FROM ai.fn_detect_duplicates(0.65);"
    )
    add_result_table(
        doc,
        ["candidate_id", "customer_name_a", "customer_name_b", "similarity", "Lý do AI gắn cờ (Thực tế trên Neon)"],
        [
            ["1", "Trần Văn Bình", "Trần Văn Bình", "1.000", "Trùng 7 số cuối điện thoại và tên tương đồng cao (> 0.65)"],
            ["2", "Nguyễn Hải Đăng", "Nguyễn Hải Đăng", "1.000", "Trùng 7 số cuối điện thoại và tên tương đồng cao (> 0.65)"],
            ["3", "Trần Văn Bình", "Tran Van Binh", "1.000", "Họ tên tương đồng cao theo thuật toán Trigram (> 0.65)"],
            ["4", "Tran Van Binh", "Trần Văn Bình", "1.000", "Họ tên tương đồng cao theo thuật toán Trigram (> 0.65)"]
        ]
    )
