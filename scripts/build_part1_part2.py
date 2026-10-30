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
        "Dữ liệu từ người dùng bên ngoài như nước sông chưa qua xử lý. Tầng Staging đóng vai trò như bể lắng "
        "và hệ thống lọc (ETL Pipeline): rác thải và tạp chất bị giữ lại ở bể cách ly (rejected_rows), "
        "chỉ nước sạch đạt chuẩn mới được bơm vào bể cấp sinh hoạt (core.customer). Đồng thời, các Check Constraint "
        "và Foreign Key hoạt động như cửa an ninh sân bay: bất kỳ hành lý quá cước hay vé giả mạo đều bị chặn đứng "
        "ngay tại cửa, bảo đảm tính toàn vẹn 100% cho hệ thống."
    )
    
    # Bước 1.1
    add_heading_2(doc, "Bước 1.1: Khởi tạo CSDL Cloud sạch sẽ tự động bằng Python")
    add_speech_box(
        doc,
        "Kính thưa Thầy Cô và Hội đồng, để chứng minh hệ thống hoàn toàn tự động và có thể tái lập tức thì "
        "trên môi trường Cloud, nhóm bắt đầu bằng lệnh dọn dẹp sạch sẽ và khởi tạo tự động toàn bộ 8 Schemas, "
        "các bảng dữ liệu, ràng buộc, View và Module AI trên Neon PostgreSQL qua script Python."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: TERMINAL POWERSHELL",
        "python python/reset_and_init_cloud.py --init"
    )
    
    # Bước 1.2
    add_heading_2(doc, "Bước 1.2: Sinh dữ liệu thô (Dirty Data) và Chạy luồng ETL chuẩn hóa")
    add_speech_box(
        doc,
        "Tiếp theo, nhóm dùng Python sinh ra 20 dòng dữ liệu khách hàng thô mô phỏng các lỗi thực tế: "
        "số điện thoại có mã quốc tế +84, lẫn dấu chấm, viết hoa thường lộn xộn, và số dư tiền âm. "
        "Sau đó luồng ETL tự động phân loại: 16 dòng được làm sạch nạp vào CSDL chính, 4 dòng lỗi bị cách ly."
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
        "-- 1. Xem 10 dòng dữ liệu thô bị lỗi (+84, dấu chấm, tiền âm, chữ hoa thường):\n"
        "SELECT raw_id, raw_full_name, raw_phone, raw_balance, source_batch \n"
        "FROM staging.stg_customers ORDER BY raw_id LIMIT 10;\n\n"
        "-- 2. Xem bảng cách ly dữ liệu bẩn (Rejected Rows - Lưu vết kiểm toán):\n"
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
        ["rejected_id", "error_reason", "raw_record (Dữ liệu bị cách ly)"],
        [
            ["1", "INVALID_PHONE", "{'name': 'Hoàng Văn E', 'phone': '09012', 'balance': '50000'}"],
            ["2", "INVALID_PHONE", "{'name': 'Bùi Thị F', 'phone': '123456', 'balance': '120000'}"],
            ["3", "NEGATIVE_BALANCE", "{'name': 'Đỗ Thị H', 'phone': '0933333333', 'balance': '-50000'}"],
            ["4", "NEGATIVE_BALANCE", "{'name': 'Dương Văn M', 'phone': '0977777777', 'balance': '-100000'}"]
        ]
    )
    
    # Bước 1.3
    add_heading_2(doc, "Bước 1.3: Thử nghiệm rủi ro Fail-Safe (Chặn lỗi ràng buộc ở tầng nhân CSDL)")
    add_speech_box(
        doc,
        "Để chứng minh cơ chế Fail-Safe hoạt động ở tầng nhân CSDL chứ không chỉ ở tầng ứng dụng, "
        "nhóm cố tình chèn trực tiếp 2 câu lệnh vi phạm ràng buộc CHECK số dư âm và FOREIGN KEY sai mã. "
        "CSDL từ chối ngay lập tức, ngăn ngừa rủi ro dữ liệu rác."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "-- 1. Thử chèn khách hàng có số dư âm -> Vi phạm CHECK (balance >= 0):\n"
        "INSERT INTO core.customer (full_name, phone, email, username, balance)\n"
        "VALUES ('Test Check Error', '0988888888', 'test@gmail.com', 'test_fail', -50000);\n\n"
        "-- 2. Thử chèn hóa đơn với mã khách hàng không tồn tại -> Vi phạm FOREIGN KEY:\n"
        "INSERT INTO billing.invoice (session_id, customer_id, cashier_id, total_amount, status)\n"
        "VALUES (9999, 9999, 9999, 100000, 'unpaid');"
    )

def add_part_2(doc):
    """Xây dựng Phần 2: Bảo mật phân quyền & Module AI 3."""
    add_heading_1(doc, "PHẦN 2: BẢO MẬT PHÂN QUYỀN ĐA TẦNG (RBAC, CLS, RLS) VÀ MODULE AI 3")
    
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Két tiền ca trực và Bảng lương giám đốc)",
        "Thu ngân ca sáng chỉ được xem ngăn két của chính mình trong ca, không được mở ngăn két của thu ngân ca tối "
        "(đó là RLS - Row-Level Security). Đồng thời nhân viên thu ngân chỉ có quyền xem danh bạ nội bộ để liên lạc "
        "công việc chứ không thể xem cột lương thưởng của đồng nghiệp và quản lý (đó là CLS - Column-Level Security)."
    )
    
    # Bước 2.1
    add_heading_2(doc, "Bước 2.1: Đóng vai Nhân viên Thu ngân Thảo (cashier_id = 3)")
    add_speech_box(
        doc,
        "Bây giờ, nhóm đóng vai nhân viên thu ngân Thảo (cashier_id = 3) bằng lệnh SET ROLE để kiểm chứng "
        "cơ chế phân quyền RBAC và chính sách an ninh cấp hàng RLS trên thực tế."
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
    
    # Bước 2.2
    add_heading_2(doc, "Bước 2.2: Chứng minh Bảo mật cấp hàng (Row-Level Security - RLS)")
    add_speech_box(
        doc,
        "Mặc dù thu ngân Thảo chạy lệnh SELECT * trên toàn bảng hóa đơn, nhờ chính sách RLS "
        "(chỉ cho phép thấy hóa đơn do chính cashier_id của mình tạo), Thảo chỉ thấy duy nhất 5 hóa đơn của mình, "
        "hoàn toàn không thấy hóa đơn của nhân viên Nam."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice;"
    )
    add_result_table(
        doc,
        ["invoice_id", "customer_id", "cashier_id", "total_amount", "status"],
        [
            ["1", "1", "3 (Phạm Thu Thảo)", "16.000", "paid"],
            ["2", "2", "3 (Phạm Thu Thảo)", "16.000", "paid"],
            ["3", "3", "3 (Phạm Thu Thảo)", "16.000", "paid"],
            ["7", "1", "3 (Phạm Thu Thảo)", "72.000", "paid"],
            ["8", "2", "3 (Phạm Thu Thảo)", "630.000", "paid"]
        ]
    )
    
    # Bước 2.3
    add_heading_2(doc, "Bước 2.3: Đổi vai sang Quản lý (Manager) để đối chiếu toàn quyền giám sát")
    add_speech_box(
        doc,
        "Để đối chiếu, nhóm chuyển sang vai trò Quản lý (nv_manager). Quản lý được phân quyền Bypass RLS "
        "nên thấy toàn bộ 9 hóa đơn của cả nhân viên Thảo và Nam, đồng thời xem được báo cáo tổng hợp doanh thu."
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
    
    # Bước 2.4
    add_heading_2(doc, "Bước 2.4: Chứng minh Bảo mật cấp cột (Column-Level Security - CLS)")
    add_speech_box(
        doc,
        "Tiếp theo, thu ngân Thảo cố tình truy cập vào cột salary (tiền lương) của bảng nhân viên. "
        "Hệ thống lập tức kích hoạt cơ chế CLS và trả về lỗi từ chối truy cập (Permission Denied)."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n\n"
        "SELECT employee_id, full_name, salary \n"
        "FROM hr.employee;\n\n"
        "RESET ROLE;"
    )
    
    # Bước 2.5
    add_heading_2(doc, "Bước 2.5: Thu ngân truy cập an toàn qua View công khai (Đã che giấu lương)")
    add_speech_box(
        doc,
        "Thay vào đó, thu ngân được cấp quyền truy vấn qua View công khai hr.vw_employee_public "
        "để tra cứu đồng nghiệp và số điện thoại phục vụ công việc mà tuyệt đối không lộ thông tin lương nhạy cảm."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n\n"
        "SELECT * FROM hr.vw_employee_public;\n\n"
        "RESET ROLE;"
    )
    
    # Bước 2.6
    add_heading_2(doc, "Bước 2.6: Module AI 3 - Bắt trùng lặp tài khoản gian lận bằng pg_trgm")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Bào mã khuyến mãi tân thủ)",
        "Nhiều khách cố tình tạo nhiều nick như 'Nguyễn Văn An' và 'Nguyễn V. An' để bào mã giảm giá tân thủ. "
        "Module AI 3 áp dụng giải thuật Fuzzy String Matching (pg_trgm) để đo độ tương đồng N-gram "
        "và gắn cờ gian lận danh tính tự động."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason \n"
        "FROM ai.fn_detect_duplicates(0.65);"
    )
    add_result_table(
        doc,
        ["candidate_id", "customer_name_a", "customer_name_b", "similarity", "reason"],
        [
            ["1", "Nguyễn Văn An", "Nguyễn V. An", "0.78", "Trùng họ tên độ tin cậy cao (Gian lận tân thủ)"],
            ["2", "Trần Thị Bích", "Trần Thị Bích Ngọc", "0.71", "Trùng họ tên tiền tố (Nghi vấn trùng lặp)"]
        ]
    )

