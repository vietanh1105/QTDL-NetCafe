import os
import sys
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

NAVY_BLUE = RGBColor(27, 54, 93)      # #1B365D
DEEP_BLUE = RGBColor(15, 76, 129)     # #0F4C81
SLATE_GRAY = RGBColor(71, 85, 105)    # #475569
CODE_TEXT = RGBColor(30, 41, 59)      # #1E293B

def set_cell_background(cell, hex_color):
    shading_xml = f'<w:shd {nsdecls("w")} w:fill="{hex_color}"/>'
    cell._tc.get_or_add_tcPr().append(parse_xml(shading_xml))

def set_cell_margins(cell, top=100, bottom=100, start=160, end=160):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(
        f'<w:tcMar {nsdecls("w")}>'
        f'<w:top w:w="{top}" w:type="dxa"/>'
        f'<w:bottom w:w="{bottom}" w:type="dxa"/>'
        f'<w:left w:w="{start}" w:type="dxa"/>'
        f'<w:right w:w="{end}" w:type="dxa"/>'
        f'</w:tcMar>'
    )
    tcPr.append(tcMar)

def add_heading_1(doc, text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = 'Calibri'
    run.font.size = Pt(14)
    run.font.bold = True
    run.font.color.rgb = NAVY_BLUE
    return p

def add_heading_2(doc, text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(10)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = 'Calibri'
    run.font.size = Pt(12)
    run.font.bold = True
    run.font.color.rgb = DEEP_BLUE
    return p

def add_code_block(doc, window_title, code_content, note=None):
    table = doc.add_table(rows=2, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    # Dòng 1: Header cửa sổ thao tác
    header_cell = table.rows[0].cells[0]
    header_cell.width = Inches(6.5)
    if 'POWERSHELL' in window_title.upper() or 'TERMINAL' in window_title.upper():
        set_cell_background(header_cell, '1E293B')  # Xám đen Slate
        h_color = RGBColor(255, 255, 255)
    else:
        set_cell_background(header_cell, '0A2540')  # Xanh đậm SQL
        h_color = RGBColor(255, 255, 255)
        
    set_cell_margins(header_cell, top=70, bottom=70, start=140, end=140)
    hp = header_cell.paragraphs[0]
    hp.paragraph_format.space_before = Pt(0)
    hp.paragraph_format.space_after = Pt(0)
    hrun = hp.add_run(window_title)
    hrun.font.name = 'Consolas'
    hrun.font.size = Pt(9.5)
    hrun.font.bold = True
    hrun.font.color.rgb = h_color
    
    # Dòng 2: Nội dung câu lệnh
    body_cell = table.rows[1].cells[0]
    body_cell.width = Inches(6.5)
    set_cell_background(body_cell, 'F8FAFC')
    set_cell_margins(body_cell, top=120, bottom=120, start=160, end=160)
    
    bp = body_cell.paragraphs[0]
    bp.paragraph_format.space_before = Pt(0)
    bp.paragraph_format.space_after = Pt(0)
    bp.paragraph_format.line_spacing = 1.15
    brun = bp.add_run(code_content)
    brun.font.name = 'Consolas'
    brun.font.size = Pt(9.5)
    brun.font.color.rgb = CODE_TEXT
    
    if note:
        np = doc.add_paragraph()
        np.paragraph_format.space_before = Pt(2)
        np.paragraph_format.space_after = Pt(4)
        nrun = np.add_run(f"💡 {note}")
        nrun.font.name = 'Calibri'
        nrun.font.size = Pt(9.5)
        nrun.font.italic = True
        nrun.font.color.rgb = RGBColor(100, 116, 139)
    else:
        spacer = doc.add_paragraph()
        spacer.paragraph_format.space_before = Pt(0)
        spacer.paragraph_format.space_after = Pt(3)

def build_code_only_document(output_path):
    print(f"Building code-only Word document at: {output_path}")
    doc = docx.Document()
    
    for section in doc.sections:
        section.top_margin = Inches(0.8)
        section.bottom_margin = Inches(0.8)
        section.left_margin = Inches(1.0)
        section.right_margin = Inches(1.0)
        
    # Header
    p_univ = doc.add_paragraph()
    p_univ.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_univ.paragraph_format.space_before = Pt(0)
    p_univ.paragraph_format.space_after = Pt(2)
    run_u = p_univ.add_run("TRƯỜNG ĐẠI HỌC BÁCH KHOA - ĐẠI HỌC ĐÀ NẴNG\nKHOA CÔNG NGHỆ THÔNG TIN")
    run_u.font.name = "Calibri"
    run_u.font.size = Pt(11)
    run_u.font.bold = True
    run_u.font.color.rgb = SLATE_GRAY
    
    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_title.paragraph_format.space_before = Pt(6)
    p_title.paragraph_format.space_after = Pt(4)
    run_t = p_title.add_run("TỔNG HỢP TOÀN BỘ CÂU LỆNH THAO TÁC MÁY BẢO VỆ TIỂU LUẬN\nQUẢN TRỊ CƠ SỞ DỮ LIỆU")
    run_t.font.name = "Calibri"
    run_t.font.size = Pt(16)
    run_t.font.bold = True
    run_t.font.color.rgb = NAVY_BLUE
    
    p_sub = doc.add_paragraph()
    p_sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_sub.paragraph_format.space_before = Pt(2)
    p_sub.paragraph_format.space_after = Pt(10)
    run_s = p_sub.add_run("Dành riêng cho bạn thao tác máy: Copy - Dán trực tiếp vào Terminal và Neon SQL Editor\n"
                          "Sinh viên: Nguyễn Việt Anh | Nền tảng: PostgreSQL 16 (Neon Cloud) | 5 Module AI")
    run_s.font.name = "Calibri"
    run_s.font.size = Pt(10.5)
    run_s.font.italic = True
    run_s.font.color.rgb = DEEP_BLUE

    # PHẦN 1
    add_heading_1(doc, "PHẦN 1: KHỞI TẠO CSDL, PIPELINE ETL VÀ RÀNG BUỘC TOÀN VẸN FAIL-SAFE")
    
    add_heading_2(doc, "Bước 1.1: Khởi tạo CSDL Cloud sạch sẽ tự động bằng Python")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ PHẢI: TERMINAL POWERSHELL",
        "# 1. Xóa sạch toàn bộ CSDL trên Cloud trong 0.5 giây (khi cần làm mới hoàn toàn):\n"
        "python python/reset_and_init_cloud.py --wipe\n\n"
        "# 2. Khởi tạo toàn bộ 8 Schemas, Bảng, Ràng buộc và Nạp dữ liệu gốc trong 8 giây:\n"
        "python python/reset_and_init_cloud.py --init",
        "Chỉ chạy lệnh --wipe khi Thầy Cô yêu cầu reset sạch CSDL từ đầu."
    )
    
    add_heading_2(doc, "Bước 1.2: Sinh dữ liệu bẩn thô và Chạy luồng ETL chuẩn hóa")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ PHẢI: TERMINAL POWERSHELL",
        "# 1. Sinh 20 dòng dữ liệu bẩn vào bảng tạm staging:\n"
        "python python/generate_dirty_data.py\n\n"
        "# 2. Chạy luồng ETL làm sạch, chuẩn hóa và nạp vào CSDL chính:\n"
        "python python/clean_data.py"
    )
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- 1. Xem toàn bộ các dòng dữ liệu thô bị lỗi (+84, dấu chấm, tiền âm, in hoa):\n"
        "SELECT raw_id, raw_full_name, raw_phone, raw_balance, source_batch \n"
        "FROM staging.stg_customers \n"
        "ORDER BY raw_id;\n\n"
        "-- 2. Xem bảng cách ly dữ liệu bẩn (Rejected Rows) với nguyên nhân cụ thể:\n"
        "SELECT rejected_id, error_reason, raw_record \n"
        "FROM staging.rejected_rows;\n\n"
        "-- 3. Xem toàn bộ khách hàng sạch đã được chuẩn hóa và nạp thành công vào CSDL chính:\n"
        "SELECT customer_id, full_name, phone, email, username, balance\n"
        "FROM core.customer\n"
        "WHERE username LIKE 'user_%'\n"
        "ORDER BY customer_id;"
    )
    
    add_heading_2(doc, "Bước 1.3: Thử nghiệm Fail-Safe chặn lỗi ràng buộc ở tầng nhân CSDL")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- 1. Thử chèn khách hàng số dư âm -> Vi phạm CHECK (balance >= 0):\n"
        "INSERT INTO core.customer (full_name, phone, email, username, balance)\n"
        "VALUES ('Test Check Error', '0988888888', 'test@gmail.com', 'test_fail', -50000);\n\n"
        "-- 2. Thử chèn hóa đơn với ID khách không tồn tại -> Vi phạm FOREIGN KEY:\n"
        "INSERT INTO billing.invoice (session_id, customer_id, cashier_id, total_amount, status)\n"
        "VALUES (9999, 9999, 9999, 100000, 'unpaid');\n\n"
        "-- 3. Cố tình xóa bản ghi cha (Phòng máy 1) đang có máy con tham chiếu -> Vi phạm ON DELETE RESTRICT:\n"
        "DELETE FROM core.room WHERE room_id = 1;",
        "Cả 3 lệnh trên đều trả về lỗi đỏ từ chối truy cập -> Đúng kịch bản chứng minh CSDL bảo vệ toàn vẹn!"
    )

    # PHẦN 2
    add_heading_1(doc, "PHẦN 2: BẢO MẬT PHÂN QUYỀN ĐA TẦNG (RBAC, CLS, RLS) VÀ MODULE AI 3")
    
    add_heading_2(doc, "Bước 2.1: Đóng vai Nhân viên Thu ngân Thảo (cashier_id = 3)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n"
        "SELECT \n"
        "    'ĐÃ CHUYỂN THÀNH CÔNG SANG VAI TRÒ THU NGÂN (THẢO)' AS thong_bao_he_thong,\n"
        "    CURRENT_USER AS vai_tro_hien_tai, \n"
        "    SESSION_USER AS tai_khoan_ket_noi;"
    )
    
    add_heading_2(doc, "Bước 2.2: Chứng minh Bảo mật cấp hàng (Row-Level Security - RLS)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- Thu ngân Thảo chỉ thấy 6 hóa đơn do chính mình lập, hoàn toàn không thấy hóa đơn của Nam:\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice;"
    )
    
    add_heading_2(doc, "Bước 2.3: Đổi vai sang Quản lý để đối chiếu toàn quyền giám sát")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "RESET ROLE;\n"
        "SET ROLE nv_manager;\n\n"
        "-- 1. Thông báo xác nhận chuyển vai trò Quản lý:\n"
        "SELECT 'ĐÃ CHUYỂN THÀNH CÔNG SANG VAI TRÒ QUẢN LÝ (MANAGER)' AS thong_bao_he_thong;\n\n"
        "-- 2. Quản lý xem toàn bộ chi tiết 9 hóa đơn của cả Thảo và Nam (Nhờ BYPASSRLS):\n"
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
        "-- 3. Bảng tổng hợp doanh thu theo từng nhân viên ca trực (Thảo 840k, Nam 72k):\n"
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
    
    add_heading_2(doc, "Bước 2.4: Chứng minh Bảo mật cấp cột (Column-Level Security - CLS)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n\n"
        "-- Thu ngân cố tình xem cột lương salary -> Bị từ chối lỗi đỏ:\n"
        "SELECT employee_id, full_name, salary \n"
        "FROM hr.employee;\n\n"
        "RESET ROLE;"
    )
    
    add_heading_2(doc, "Bước 2.5: Thu ngân truy cập an toàn qua View công khai (Đã che giấu lương)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "SET ROLE nv_thao;\n\n"
        "-- Xem an toàn 4 nhân sự qua View (Cột salary đã bị loại bỏ hoàn toàn):\n"
        "SELECT * FROM hr.vw_employee_public;\n\n"
        "RESET ROLE;"
    )
    
    add_heading_2(doc, "Bước 2.6: Module AI 3 - Bắt trùng lặp tài khoản khách hàng bằng pg_trgm")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- AI quét độ tương đồng chuỗi và phát hiện 4 cặp nghi vấn gian lận bào khuyến mãi:\n"
        "SELECT candidate_id, customer_name_a, customer_name_b, similarity, reason \n"
        "FROM ai.fn_detect_duplicates(0.65);"
    )

    # PHẦN 3
    add_heading_1(doc, "PHẦN 3: TỐI ƯU HÓA HIỆU NĂNG VÀ MODULE AI 2 (PERFORMANCE DEMO)")
    
    add_heading_2(doc, "Bước 3.1: Đo tự động hiệu năng thực tế trên 20.000 dòng (Khuyên dùng)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- Benchmark tự động 20.000 dòng (B-Tree Index Scan nhanh gấp 5.3 lần Seq Scan):\n"
        "SELECT \n"
        "    phuong_thuc AS phuong_thuc_quet,\n"
        "    so_dong_du_lieu AS quy_mo_du_lieu,\n"
        "    thoi_gian_ms AS thoi_gian_thuc_thi_ms,\n"
        "    so_block_bo_nho_doc AS bo_nho_da_doc,\n"
        "    danh_gia_hieu_nang AS ket_luan_hieu_nang\n"
        "FROM ai.fn_compare_index_benchmark();"
    )
    
    add_heading_2(doc, "Bước 3.2: Thao tác thủ công đối chiếu EXPLAIN ANALYZE trên bảng lịch sử 25.000 dòng")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "RESET ROLE;\n\n"
        "-- 1. Xóa index cũ để đưa bảng về trạng thái nguyên bản:\n"
        "DROP INDEX IF EXISTS billing.idx_invoice_history_amount;\n\n"
        "-- 2. Chạy EXPLAIN tìm hóa đơn 630.000 VNĐ khi CHƯA CÓ INDEX -> Kết quả: Seq Scan (Chậm ~1.48 ms, 160 blocks):\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice_history \n"
        "WHERE total_amount = 630000;\n\n"
        "-- 3. Tạo chỉ mục B-Tree trên cột số tiền hóa đơn:\n"
        "CREATE INDEX idx_invoice_history_amount ON billing.invoice_history (total_amount);\n\n"
        "-- 4. Chạy lại EXPLAIN sau khi có INDEX -> Kết quả: Index Scan (Nhanh siêu tốc ~0.037 ms, tối ưu gấp 40 lần!):\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice_history \n"
        "WHERE total_amount = 630000;"
    )
    
    add_heading_2(doc, "Bước 3.3: Module AI 2 - Tự động Khuyến nghị Chỉ mục (Index Recommendation Engine)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- AI quét runtime thống kê CSDL và tự động khuyến nghị 3 bảng cần đánh index:\n"
        "SELECT \n"
        "    recommendation_id AS stt, \n"
        "    table_name AS bang_can_toi_uu, \n"
        "    column_list AS cot_can_danh_index, \n"
        "    reason AS ly_do_ai_khuyen_nghi, \n"
        "    suggested_sql AS lenh_tao_index_tu_dong \n"
        "FROM ai.fn_recommend_indexes() \n"
        "LIMIT 3;"
    )

    # PHẦN 4
    add_heading_1(doc, "PHẦN 4: ĐIỂM NHẤN TÍCH HỢP & 3 MODULE AI THỜI GIAN THỰC (AI HIGHLIGHTS)")
    
    add_heading_2(doc, "Bước 4.1: Bóc tách dữ liệu bán cấu trúc NoSQL từ cột JSONB")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- 1. Bóc tách linh hoạt các trường JSON và lọc đồng thời {CPU i7, GPU RTX, RAM 32GB}:\n"
        "SELECT \n"
        "    computer_code AS ma_may, \n"
        "    computer_name AS ten_may,\n"
        "    specs ->> 'cpu'     AS chip_cpu,\n"
        "    specs ->> 'gpu'     AS card_man_hinh,\n"
        "    specs ->> 'ram'     AS dung_luong_ram\n"
        "FROM core.computer\n"
        "WHERE specs ->> 'gpu' ILIKE '%RTX%'\n"
        "  AND specs ->> 'cpu' ILIKE '%i7%'\n"
        "  AND specs ->> 'ram' = '32GB'\n"
        "ORDER BY computer_code;\n\n"
        "-- 2. Tìm kiếm siêu tốc bằng toán tử tập con JSONB Containment (@>):\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT computer_code, computer_name, specs \n"
        "FROM core.computer \n"
        "WHERE specs @> '{\"cpu\": \"i7-13700F\", \"gpu\": \"RTX 4070\", \"ram\": \"32GB\"}'::jsonb;"
    )
    
    add_heading_2(doc, "Bước 4.2: [ĐIỂM CỘNG XUẤT SẮC] Module AI 1 - Real-time Cảnh báo rủi ro (LISTEN/NOTIFY) & SMS")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ PHẢI: TERMINAL 1 (KÍCH HOẠT LẮNG NGHE REAL-TIME)",
        "python python/listen_ai_alerts.py\n"
        "# (Màn hình hiện: [+] ĐÃ KÍCH HOẠT LẮNG NGHE KÊNH: 'ai_security_alert' và đứng chờ sự kiện...)"
    )
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR (NẠP TIỀN BẤT THƯỜNG 10 TRIỆU ĐỒNG)",
        "INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)\n"
        "VALUES ('topup', 1, 'bank_transfer', 10000000, 1, 'TOPUP-VIP-10M');"
    )
    add_code_block(
        doc,
        "🖥️ CỬA SỔ PHẢI: TERMINAL 2 (BẮN SMS BRANDNAME VỀ ĐIỆN THOẠI)",
        "python python/send_brandname_sms.py --phone \"0327469105\" --name \"Nguyen Viet Anh\" --amount 10000000 --balance 10050000"
    )
    
    add_heading_2(doc, "Bước 4.3: Module AI 4 - Thuật toán thống kê Z-Score phát hiện ngoại lai (Outliers)")
    add_code_block(
        doc,
        "🖥️ CỬA SỔ TRÁI: NEON SQL EDITOR / DBEAVER",
        "-- AI Z-Score phát hiện phiên chơi 35 tiếng (Z=2.65) và nạp 10 triệu (Z=2.13):\n"
        "SELECT \n"
        "    flag_id AS ma_canh_bao, \n"
        "    anomaly_type AS loai_bat_thuong, \n"
        "    metric_value AS gia_tri_thuc_te, \n"
        "    z_score AS he_so_z_score,\n"
        "    (detail_info->>'mean')::numeric AS trung_binh_phong_may,\n"
        "    (detail_info->>'stddev')::numeric AS do_lech_chuan,\n"
        "    CASE \n"
        "        WHEN anomaly_type = 'SESSION_DURATION_OUTLIER' \n"
        "            THEN 'Phiên chơi quá dài (Khách chơi tới 2.100 phút ~ 35 tiếng, trung bình quán chỉ 373 phút -> Cần kiểm tra khách ngủ quên hoặc máy bị treo)'\n"
        "        WHEN anomaly_type = 'PAYMENT_AMOUNT_OUTLIER' \n"
        "            THEN 'Nạp tiền quá lớn (Khách nạp tới 10 triệu VNĐ, trung bình quán chỉ 1 triệu VNĐ -> Cần kiểm tra giao dịch bất thường hoặc rửa tiền)'\n"
        "        ELSE 'Hành vi lệch chuẩn thống kê (Vượt ngoài khoảng tin cậy 95% của phân phối chuẩn Gauss)'\n"
        "    END AS mo_ta_de_hieu_cho_hoi_dong\n"
        "FROM ai.fn_detect_anomalies(1.5);"
    )
    
    add_heading_2(doc, "Bước 4.4: Module AI 5 - Kích hoạt Khuyến mãi & Gửi SMS Discount thời gian thực")
    add_code_block(
        doc,
        "🖥️ BƯỚC A: CỬA SỔ PHẢI (TERMINAL) - CHẠY TOOL PYTHON LẮNG NGHE CỔNG SMS GATEWAY",
        "python python/sms_gateway_listener.py\n"
        "# (Hoặc công cụ gửi SMS trực tiếp: python python/send_brandname_sms.py --promo --phone \"0327469105\" --name \"Nguyen Viet Anh\")"
    )
    add_code_block(
        doc,
        "🖥️ BƯỚC B: CỬA SỔ TRÁI (NEON SQL EDITOR) - CHẠY LỆNH SQL KÍCH HOẠT KHUYẾN MÃI AI",
        "SELECT applied_code, applied_name, target_info, discount_val, status_msg \n"
        "FROM ai.fn_apply_promotions();"
    )
    add_code_block(
        doc,
        "🖥️ BƯỚC D: CỬA SỔ TRÁI (NEON SQL EDITOR) - ĐỐI CHIẾU BẢNG GIÁ THỰC TẾ (TỪ 10K CÒN 7.5K)",
        "-- Đối chiếu bảng giá tính tiền thực tế sau khi áp dụng Khuyến mãi AI (Giảm 25%):\n"
        "SELECT \n"
        "    p.code AS ma_khuyen_mai,\n"
        "    p.promotion_name AS ten_chuong_trinh,\n"
        "    10000 AS gia_goc_moi_gio,\n"
        "    p.value || '%' AS phan_tram_giam,\n"
        "    ROUND(10000 * (p.value / 100)) AS so_tien_duoc_giam,\n"
        "    ROUND(10000 * (1 - p.value / 100)) AS gia_thuc_te_phai_tra,\n"
        "    'ĐÃ ÁP DỤNG THÀNH CÔNG VÀO HỆ THỐNG TÍNH TIỀN' AS trang_thai_he_thong\n"
        "FROM billing.promotion p\n"
        "WHERE p.code = 'HAPPY_AI_16H';"
    )
    add_code_block(
        doc,
        "🖥️ BƯỚC E: CỬA SỔ PHẢI (TERMINAL) - RÀ SOÁT TẦNG LÕI NHÂN CSDL (ĐỐI CHIẾU KHÁCH VIP VS KHÁCH THƯỜNG)",
        "python python/verify_promotion_engine.py",
        "Kết quả rà soát phân định rõ: Khách thường 7.500đ/h, Khách VIP Nguyễn Việt Anh nhận ưu đãi kép (7.500đ/h + Voucher 30k, chơi 4h = 0đ)!"
    )
    add_code_block(
        doc,
        "🖥️ BƯỚC F [TÙY CHỌN BẢO VỆ]: HÀM HOÀN TÁC CÁC CHƯƠNG TRÌNH KHUYẾN MÃI DO AI TỰ ĐỘNG SINH",
        "-- 1. Chạy trên Cửa sổ Neon SQL Editor để hoàn tác/thu hồi sạch sẽ toàn bộ:\n"
        "SELECT * FROM ai.fn_rollback_promotions();\n\n"
        "-- 2. Chạy trên Terminal PowerShell để kiểm chứng giá lập tức quay về nguyên bản 10.000 VNĐ/giờ:\n"
        "python python/verify_promotion_engine.py",
        "Xác nhận hoàn tác thành công, toàn bộ khuyến mãi Happy Hour và Voucher đã được dọn sạch khỏi Database!"
    )
    
    doc.save(output_path)
    print(f"Successfully generated code-only Word document: {output_path}")

if __name__ == "__main__":
    out_file = os.path.join("docs", "DanhSach_CauLenh_ThaoTac_BaoVe.docx")
    build_code_only_document(out_file)
