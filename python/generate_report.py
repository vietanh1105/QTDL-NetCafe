# Trách nhiệm: tạo báo cáo kiến trúc NetCafe theo contract hiện hành, không còn nghiệp vụ Đặt máy.
# Liên kết: sql/netcafe_full.sql, docs và evidence của các gate CSDL.

import os
import sys
import docx
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

sys.stdout.reconfigure(encoding='utf-8')

# Màu sắc chủ đạo (Navy Palette)
COLOR_PRIMARY = RGBColor(27, 54, 93)      # Navy #1B365D
COLOR_SECONDARY = RGBColor(41, 128, 185)  # Ocean Blue #2980B9
COLOR_DARK = RGBColor(44, 62, 80)         # Charcoal #2C3E50
COLOR_GRAY = RGBColor(127, 140, 141)      # Gray #7F8C8D
COLOR_BG_LIGHT = "F4F6F9"
COLOR_HEADER_BG = "1B365D"

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), fill_hex)
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('w:top', top), ('w:bottom', bottom), ('w:left', left), ('w:right', right)]:
        node = OxmlElement(m)
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def add_callout(doc, title, text):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    cell = tbl.cell(0, 0)
    cell.width = Inches(6.5)
    set_cell_background(cell, "EBF5FB")
    set_cell_margins(cell, top=120, bottom=120, left=180, right=180)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(2)
    run_t = p.add_run(f"📌 {title}: ")
    run_t.bold = True
    run_t.font.name = "Calibri"
    run_t.font.size = Pt(10.5)
    run_t.font.color.rgb = COLOR_PRIMARY

    run_b = p.add_run(text)
    run_b.font.name = "Calibri"
    run_b.font.size = Pt(10)
    run_b.font.color.rgb = COLOR_DARK
    doc.add_paragraph().paragraph_format.space_after = Pt(4)

def add_code_block(doc, code_text):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    cell = tbl.cell(0, 0)
    cell.width = Inches(6.5)
    set_cell_background(cell, "F8F9F9")
    set_cell_margins(cell, top=100, bottom=100, left=150, right=150)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(0)
    run = p.add_run(code_text.strip())
    run.font.name = "Consolas"
    run.font.size = Pt(9)
    run.font.color.rgb = RGBColor(40, 55, 71)
    doc.add_paragraph().paragraph_format.space_after = Pt(4)

def format_table(table, header_bg=COLOR_HEADER_BG):
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, row in enumerate(table.rows):
        for cell in row.cells:
            cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER
            set_cell_margins(cell, top=80, bottom=80, left=120, right=120)
            if i == 0:
                set_cell_background(cell, header_bg)
                for p in cell.paragraphs:
                    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
                    for r in p.runs:
                        r.font.bold = True
                        r.font.color.rgb = RGBColor(255, 255, 255)
                        r.font.size = Pt(9.5)
            else:
                if i % 2 == 1:
                    set_cell_background(cell, "FFFFFF")
                else:
                    set_cell_background(cell, "F2F4F4")
                for p in cell.paragraphs:
                    for r in p.runs:
                        r.font.size = Pt(9)
                        r.font.color.rgb = COLOR_DARK

def build_report():
    print("=== ĐANG KHỞI TẠO BÁO CÁO TIỂU LUẬN WORD (.DOCX) ===")
    doc = Document()

    # Thiết lập lề trang tiêu chuẩn (A4: Top/Bottom/Left/Right: 1 inch)
    for section in doc.sections:
        section.top_margin = Inches(1.0)
        section.bottom_margin = Inches(1.0)
        section.left_margin = Inches(1.0)
        section.right_margin = Inches(1.0)

    # -------------------------------------------------------------------------
    # TRANG BÌA (COVER PAGE)
    # -------------------------------------------------------------------------
    p_inst = doc.add_paragraph()
    p_inst.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_inst = p_inst.add_run("BỘ GIÁO DỤC VÀ ĐÀO TẠO\nTRƯỜNG ĐẠI HỌC BÁCH KHOA / CÔNG NGHỆ\nKHOA CÔNG NGHỆ THÔNG TIN")
    r_inst.font.name = "Times New Roman"
    r_inst.font.size = Pt(13)
    r_inst.font.bold = True
    r_inst.font.color.rgb = COLOR_PRIMARY
    p_inst.paragraph_format.space_after = Pt(40)

    p_logo = doc.add_paragraph()
    p_logo.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_logo = p_logo.add_run("⭐⭐⭐\n[ HỆ THỐNG QUẢN LÝ DỮ LIỆU PHÒNG MÁY / NETCAFE ]\n⭐⭐⭐")
    r_logo.font.name = "Times New Roman"
    r_logo.font.size = Pt(14)
    r_logo.font.bold = True
    r_logo.font.color.rgb = COLOR_SECONDARY
    p_logo.paragraph_format.space_after = Pt(40)

    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_title_tag = p_title.add_run("BÁO CÁO TIỂU LUẬN MÔN HỌC\nQUẢN TRỊ CƠ SỞ DỮ LIỆU\n\n")
    r_title_tag.font.name = "Times New Roman"
    r_title_tag.font.size = Pt(15)
    r_title_tag.font.bold = True
    r_title_tag.font.color.rgb = COLOR_DARK

    r_title = p_title.add_run("XÂY DỰNG VÀ TỐI ƯU HÓA CƠ SỞ DỮ LIỆU\nCHO HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET\n(NETCAFE MANAGEMENT SYSTEM)")
    r_title.font.name = "Times New Roman"
    r_title.font.size = Pt(19)
    r_title.font.bold = True
    r_title.font.color.rgb = COLOR_PRIMARY
    p_title.paragraph_format.space_after = Pt(60)

    p_meta = doc.add_paragraph()
    p_meta.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p_meta.paragraph_format.left_indent = Inches(1.5)
    r_meta = p_meta.add_run(
        "Giảng viên hướng dẫn:  TS. Giảng Viên Phụ Trách\n"
        "Học phần:              Quản trị Cơ sở Dữ liệu (QTDL)\n"
        "Hệ thống CSDL:         PostgreSQL 17 (Enterprise Standard)\n"
        "Ngôn ngữ bổ trợ:       Python 3.13 (ETL & Analysis), C# .NET 10 (Desktop Client)\n"
        "Sinh viên thực hiện:   Nhóm Sinh Viên Nghiên Cứu\n"
        "Lớp / Khóa:            Kỹ thuật Phần mềm / Hệ thống Thông tin\n"
    )
    r_meta.font.name = "Times New Roman"
    r_meta.font.size = Pt(12)
    r_meta.font.color.rgb = COLOR_DARK
    p_meta.paragraph_format.space_after = Pt(50)

    p_footer_cover = doc.add_paragraph()
    p_footer_cover.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_fc = p_footer_cover.add_run("Hà Nội, Năm học 2025 - 2026")
    r_fc.font.name = "Times New Roman"
    r_fc.font.size = Pt(12)
    r_fc.font.italic = True
    r_fc.font.color.rgb = COLOR_GRAY

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # TỔNG QUAN VÀ MỤC LỤC TÓM TẮT
    # -------------------------------------------------------------------------
    h_toc = doc.add_heading("MỤC LỤC NỘI DUNG TIỂU LUẬN", level=1)
    h_toc.paragraph_format.space_after = Pt(14)
    for r in h_toc.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    toc_items = [
        ("CHƯƠNG 1: TỔNG QUAN NGHIỆP VỤ VÀ KIẾN TRÚC LOGIC CSDL", "Trang 3"),
        ("  1.1. Bối cảnh và 10 chức năng cốt lõi phòng game/net", "3"),
        ("  1.2. Kiến trúc phân tách 8 Schema nghiệp vụ độc lập", "4"),
        ("  1.3. Mô hình thực thể liên kết ERD và chuẩn hóa 3NF", "5"),
        ("  1.4. Thiết lập CSDL độc lập Locale HĐH với template0 và UTF-8", "6"),
        ("CHƯƠNG 2: THIẾT KẾ VẬT LÝ VÀ BẢO ĐẢM CHẤT LƯỢNG DỮ LIỆU", "Trang 7"),
        ("  2.1. Chi tiết 22 bảng và kiểu dữ liệu chuyên sâu", "7"),
        ("  2.2. Hệ thống ràng buộc toàn vẹn nâng cao (17 Test Cases PASS)", "9"),
        ("  2.3. Luồng làm sạch dữ liệu ETL và 4 tiêu chí chất lượng (Sạch - Sống - Đúng - Đủ)", "11"),
        ("CHƯƠNG 3: LẬP TRÌNH CSDL VÀ AN NINH BẢO MẬT PHÂN QUYỀN", "Trang 13"),
        ("  3.1. Stored Procedures nghiệp vụ và Triggers đồng bộ trạng thái", "13"),
        ("  3.2. View và Materialized View báo cáo phân tích", "15"),
        ("  3.3. Mô hình kiểm soát truy cập theo vai trò (RBAC - 5 Vai trò)", "16"),
        ("  3.4. Column-Level Security bảo vệ dữ liệu nhạy cảm (Lương & Mật khẩu)", "17"),
        ("  3.5. Row-Level Security (RLS) cách ly dữ liệu thời gian thực", "18"),
        ("CHƯƠNG 4: TỐI ƯU HÓA HIỆU NĂNG VÀ VẬN HÀNH QUẢN TRỊ DBA", "Trang 20"),
        ("  4.1. Chiến lược chỉ mục đa dạng (B-Tree, Partial, GIN Trigram, GIN JSONB, BRIN)", "20"),
        ("  4.2. Kết quả đo lường thực tế EXPLAIN (ANALYZE, BUFFERS) Trước & Sau tối ưu", "22"),
        ("  4.3. Tinh chỉnh Heap-Only Tuples (HOT) qua fillfactor và Autovacuum", "24"),
        ("  4.4. Phân vùng vật lý Range Partitioning theo tháng và tự động hóa", "25"),
        ("  4.5. Chiến lược sao lưu dự phòng và thử nghiệm phục hồi thảm họa", "26"),
        ("CHƯƠNG 5: 5 MODULE TỰ ĐỘNG HÓA THÔNG MINH (IN-DATABASE AI) & CLIENT", "Trang 28"),
        ("  5.1. Module 1: Cảnh báo an ninh thông minh (Security Alert & Realtime NOTIFY)", "28"),
        ("  5.2. Module 2: Tự động khuyến nghị chỉ mục (Index Recommendation Engine)", "30"),
        ("  5.3. Module 3: Phát hiện khách trùng lặp mờ bằng Trigram (pg_trgm)", "31"),
        ("  5.4. Module 4: Phát hiện bất thường thống kê qua chuẩn hóa Z-Score", "33"),
        ("  5.5. Module 5: Gợi ý chương trình khuyến mãi thông minh (Happy Hour & Win-Back)", "35"),
        ("  5.6. Phần mềm Desktop Client mỏng (.NET 10 WinForms) phục vụ demo song song", "37"),
        ("CHƯƠNG 6: KẾT LUẬN VÀ KẾT QUẢ ĐẠT ĐƯỢC", "Trang 39")
    ]

    tbl_toc = doc.add_table(rows=len(toc_items), cols=2)
    tbl_toc.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, (title_text, page_text) in enumerate(toc_items):
        c0 = tbl_toc.cell(i, 0)
        c1 = tbl_toc.cell(i, 1)
        c0.width = Inches(5.5)
        c1.width = Inches(1.0)
        p0 = c0.paragraphs[0]
        p1 = c1.paragraphs[0]
        p0.paragraph_format.space_before = Pt(1)
        p0.paragraph_format.space_after = Pt(1)
        p1.paragraph_format.space_before = Pt(1)
        p1.paragraph_format.space_after = Pt(1)
        p1.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        
        r0 = p0.add_run(title_text)
        r1 = p1.add_run(page_text)
        r0.font.name = "Times New Roman"
        r1.font.name = "Times New Roman"
        r0.font.size = Pt(10)
        r1.font.size = Pt(10)
        if title_text.startswith("CHƯƠNG"):
            r0.bold = True
            r1.bold = True
            r0.font.color.rgb = COLOR_PRIMARY
            r1.font.color.rgb = COLOR_PRIMARY
        else:
            r0.font.color.rgb = COLOR_DARK
            r1.font.color.rgb = COLOR_GRAY

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # CHƯƠNG 1: TỔNG QUAN NGHIỆP VỤ VÀ KIẾN TRÚC LOGIC CSDL
    # -------------------------------------------------------------------------
    h1 = doc.add_heading("CHƯƠNG 1: TỔNG QUAN NGHIỆP VỤ VÀ KIẾN TRÚC LOGIC CSDL", level=1)
    for r in h1.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    p = doc.add_paragraph(
        "Hệ thống quản lý phòng game/net (NetCafe Management System) được xây dựng trên máy chủ cơ sở dữ liệu "
        "PostgreSQL 17. Đề tài tập trung giải quyết trọn vẹn bài toán vận hành một phòng máy quy mô thực tế, "
        "hoạt động ổn định trên hạ tầng mạng LAN nội bộ, bảo đảm tính sẵn sàng cao, an ninh phân quyền chặt chẽ, "
        "tối ưu hóa I/O phần cứng và tích hợp 5 module tự động hóa thông minh (In-Database AI) chạy hoàn toàn nội bộ trong CSDL."
    )
    p.paragraph_format.line_spacing = 1.15
    p.paragraph_format.space_after = Pt(8)

    doc.add_heading("1.1. Danh mục 9 chức năng nghiệp vụ cốt lõi", level=2)
    biz_funcs = [
        ("1. Quản lý máy tính", "Mã máy, tên máy, phòng/khu vực, loại cấu hình, IP/MAC, trạng thái (trống, đang chơi, bảo trì, hỏng), giá tiền/giờ."),
        ("2. Quản lý khách hàng", "Mã khách, họ tên, số điện thoại, tài khoản, mật khẩu băm bcrypt, số dư ví, điểm tích lũy, phân hạng hội viên (Regular, Silver, Gold, VIP)."),
        ("3. Quản lý phiên chơi", "Bảng trọng tâm hệ thống: khách hàng, máy trạm, thời điểm bắt đầu/kết thúc, thời lượng, đơn giá áp dụng, tiền giờ, trạng thái."),
        ("4. Dịch vụ F&B & Kho", "Danh mục món, sản phẩm đồ ăn/uống, quản lý đơn gọi món theo phiên chơi, trừ tồn kho tự động qua Trigger."),
        ("5. Hóa đơn & Thanh toán", "Tổng kết phiên chơi: Tiền giờ + Dịch vụ F&B + Phụ phí - Giảm giá = Tổng tiền. Thanh toán qua ví hội viên hoặc tiền mặt/chuyển khoản."),
        ("6. Quản trị nhân sự", "Phân quyền 5 vai trò (Admin, Manager, Cashier, Staff, Technician), ca làm việc, bảo vệ mức lương tuyệt đối."),
        ("7. Bảo trì phòng máy", "Ghi nhận máy lỗi, kỹ thuật viên phụ trách, chi phí, linh kiện thay thế lưu dạng JSONB linh hoạt."),
        ("8. Khuyến mãi & Ưu đãi", "Mã giảm giá, Happy Hour khung giờ đêm, ưu đãi theo hạng thành viên Gold/VIP, lưu điều kiện áp dụng JSONB."),
        ("9. Báo cáo & Thống kê", "Doanh thu theo ngày/tháng, công suất sử dụng máy, phân tích RFM top khách hàng chi tiêu qua Materialized Views.")
    ]
    tbl_bf = doc.add_table(rows=len(biz_funcs) + 1, cols=2)
    tbl_bf.cell(0, 0).paragraphs[0].add_run("Chức năng Nghiệp vụ")
    tbl_bf.cell(0, 1).paragraphs[0].add_run("Mô tả Thiết kế CSDL")
    tbl_bf.cell(0, 0).width = Inches(2.2)
    tbl_bf.cell(0, 1).width = Inches(4.3)
    for i, (fn, desc) in enumerate(biz_funcs):
        tbl_bf.cell(i+1, 0).paragraphs[0].add_run(fn)
        tbl_bf.cell(i+1, 1).paragraphs[0].add_run(desc)
    format_table(tbl_bf)

    doc.add_heading("1.2. Phân rã Kiến trúc thành 8 Schemas chuyên biệt", level=2)
    p = doc.add_paragraph(
        "Thay vì dồn toàn bộ bảng vào schema public mặc định, hệ thống được thiết kế theo mô hình phân rã 8 Schemas độc lập. "
        "Mô hình này giúp áp dụng nguyên tắc kiểm soát truy cập tối thiểu (Least Privilege), tăng tính module hóa và dễ dàng cấp quyền:"
    )
    p.paragraph_format.line_spacing = 1.15

    schemas_info = [
        ("core", "Chứa các thực thể nền tảng: room, computer_type, computer, customer, gaming_session và yêu cầu hỗ trợ."),
        ("inventory", "Quản lý chuỗi cung ứng dịch vụ: product_category, product, food_order, food_order_detail."),
        ("billing", "Quản lý dòng tiền và hóa đơn: promotion, invoice, invoice_promotion, payment."),
        ("hr", "Quản lý nhân viên và kỹ thuật bảo trì: employee, maintenance."),
        ("audit", "Nhật ký kiểm toán an ninh bảo mật: audit_log (Phân vùng vật lý Range Partitioning theo tháng)."),
        ("ai", "Lưu trữ kết quả 5 module thông minh: ai_alert, index_recommendation, duplicate_candidate, anomaly_flag, promotion_suggestion."),
        ("reporting", "Hệ thống View tổng hợp và Materialized View phục vụ phân tích hiệu năng cao."),
        ("staging", "Vùng đệm chứa dữ liệu thô phục vụ kiểm định chất lượng và luồng làm sạch ETL.")
    ]
    tbl_sc = doc.add_table(rows=len(schemas_info) + 1, cols=2)
    tbl_sc.cell(0, 0).paragraphs[0].add_run("Schema Name")
    tbl_sc.cell(0, 1).paragraphs[0].add_run("Phạm vi Trách nhiệm & Thực thể Quản lý")
    tbl_sc.cell(0, 0).width = Inches(1.5)
    tbl_sc.cell(0, 1).width = Inches(5.0)
    for i, (s_name, s_desc) in enumerate(schemas_info):
        tbl_sc.cell(i+1, 0).paragraphs[0].add_run(s_name)
        tbl_sc.cell(i+1, 1).paragraphs[0].add_run(s_desc)
    format_table(tbl_sc)

    add_callout(doc, "Giải thích Thiết kế cho Hội đồng", 
                "Việc phân tách 8 schema giúp DBA thực hiện REVOKE ALL ON SCHEMA public FROM PUBLIC, "
                "triệt tiêu hoàn toàn rủi ro bảo mật mặc định của PostgreSQL. Thu ngân chỉ có USAGE trên core/inventory/billing, "
                "kỹ thuật viên chỉ có USAGE trên hr/core, nhân viên phục vụ hoàn toàn không thấy bảng thanh toán/hóa đơn.")

    doc.add_heading("1.3. Chuẩn hóa Dữ liệu đạt Chuẩn 3NF", level=2)
    p = doc.add_paragraph(
        "Mô hình dữ liệu được thiết kế tuân thủ nghiêm ngặt chuẩn hóa dạng bình thường thứ 3 (3NF):\n"
        "• Đạt 1NF: Tất cả các thuộc tính đều mang giá trị nguyên tử (atomic). Các trường linh hoạt như cấu hình phần cứng "
        "hay thuộc tính món ăn được chuẩn hóa qua kiểu JSONB có cấu trúc khóa-giá trị nhất quán.\n"
        "• Đạt 2NF: Toàn bộ các thuộc tính không khóa đều phụ thuộc hàm hoàn toàn vào toàn bộ khóa chính (khắc phục ở bảng food_order_detail và invoice_promotion).\n"
        "• Đạt 3NF: Triệt tiêu hoàn toàn phụ thuộc bắc cầu. Đơn giá giờ thuê không lưu trực tiếp trên bảng computer mà được "
        "tách riêng vào bảng computer_type. Khi thay đổi giá loại máy VIP, hệ thống chỉ cập nhật 1 dòng duy nhất thay vì quét hàng trăm máy tính."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_heading("1.4. Thiết lập CSDL độc lập Locale HĐH", level=2)
    add_code_block(doc, 
"""CREATE DATABASE netcafe_db
    WITH TEMPLATE = template0
    ENCODING = 'UTF8'
    LC_COLLATE = 'C'
    LC_CTYPE = 'C'
    CONNECTION LIMIT = -1;""")
    p = doc.add_paragraph(
        "Giải thích DBA: Thiết lập CSDL từ template0 với ENCODING 'UTF8' và Collate 'C' giúp bài thi có thể tái tạo "
        "100% không lỗi trên bất kỳ máy chủ nào của giảng viên (dù máy chạy Windows với locale tiếng Việt CP1258 hay Linux UTF-8). "
        "Việc tìm kiếm không phân biệt dấu tiếng Việt được xử lý bằng extension unaccent và pg_trgm thay vì dựa vào collation OS."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # CHƯƠNG 2: THIẾT KẾ VẬT LÝ VÀ BẢO ĐẢM CHẤT LƯỢNG DỮ LIỆU
    # -------------------------------------------------------------------------
    h2 = doc.add_heading("CHƯƠNG 2: THIẾT KẾ VẬT LÝ VÀ BẢO ĐẢM CHẤT LƯỢNG DỮ LIỆU", level=1)
    for r in h2.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    doc.add_heading("2.1. Lựa chọn Kiểu Dữ liệu Vật lý Chuyên sâu", level=2)
    p = doc.add_paragraph(
        "Hệ thống tận dụng tối đa các kiểu dữ liệu bản địa mạnh mẽ của PostgreSQL 17 nhằm tối ưu không gian lưu trữ và bảo đảm toàn vẹn:\n"
        "• NUMERIC(12,0): Áp dụng cho toàn bộ các cột tiền tệ (balance, total_amount, salary...). Kiểu fixed-point tránh sai số làm tròn của FLOAT/DOUBLE.\n"
        "• TIMESTAMPTZ: Lưu trữ mốc thời gian kèm múi giờ quốc tế (+07:00). Triệt tiêu lỗi lệch giờ khi chạy đa nền tảng.\n"
        "• INET & MACADDR: Lưu địa chỉ IP máy trạm (192.168.1.x) và địa chỉ phần cứng MAC (00:1A:2B:3C:4D:xx), kiểm tra định dạng mạng tự động ở tầng CSDL.\n"
        "• JSONB: Lưu trữ cấu hình CPU/RAM/VGA linh hoạt và linh kiện bảo trì, hỗ trợ đánh chỉ mục GIN jsonb_path_ops."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_heading("2.2. Ma trận Ràng buộc Toàn vẹn (16 Test Cases PASS)", level=2)
    p = doc.add_paragraph(
        "Hệ thống đã trải qua bộ kiểm thử tự động gồm 16 bài test ràng buộc toàn vẹn nghiêm ngặt trong file tests/verify.sql. "
        "Kết quả chạy thực tế trên máy chủ PostgreSQL 17 đạt tỷ lệ chính xác 16/16 (100% PASS - Ghi nhận tại evidence/test_verify.log):"
    )
    p.paragraph_format.line_spacing = 1.15

    constraints_summary = [
        ("C1: ck_invoice_total_amount", "billing.invoice", "CHECK (total_amount >= 0)", "PASS", "Chặn số tiền hóa đơn âm"),
        ("C2: ck_invoice_formula", "billing.invoice", "CHECK (total = play + service + surcharge - discount)", "PASS", "Bảo đảm công thức toán học bất biến"),
        ("C3: ck_session_time_order", "core.gaming_session", "CHECK (end_time IS NULL OR end_time > start_time)", "PASS", "Chặn giờ kết thúc trước giờ bắt đầu"),
        ("C4: ck_session_status_time", "core.gaming_session", "CHECK ((status='active') = (end_time IS NULL))", "PASS", "Đồng bộ tuyệt đối trạng thái và thời gian"),
        ("C5: ck_customer_balance", "core.customer", "CHECK (balance >= 0)", "PASS", "Ngăn chặn số dư tài khoản âm"),
        ("C6: ck_customer_phone_format", "core.customer", "CHECK (phone ~ '^0[35789][0-9]{8}$')", "PASS", "Bảo đảm định dạng SĐT di động Việt Nam"),
        ("C7: ck_payment_kind_invoice", "billing.payment", "CHECK ((kind='invoice' AND id IS NOT NULL) OR ...)", "PASS", "Khớp loại thanh toán và ID hóa đơn"),
        ("C8: uq_computer_ip", "core.computer", "UNIQUE (ip_address)", "PASS", "Chặn trùng lặp IP trong mạng LAN"),
        ("C9: uq_computer_mac", "core.computer", "UNIQUE (mac_address)", "PASS", "Chặn trùng lặp địa chỉ MAC card mạng")
    ]
    tbl_cs = doc.add_table(rows=len(constraints_summary) + 1, cols=5)
    tbl_cs.cell(0, 0).paragraphs[0].add_run("Tên Ràng buộc")
    tbl_cs.cell(0, 1).paragraphs[0].add_run("Bảng áp dụng")
    tbl_cs.cell(0, 2).paragraphs[0].add_run("Biểu thức Ràng buộc")
    tbl_cs.cell(0, 3).paragraphs[0].add_run("Kết quả")
    tbl_cs.cell(0, 4).paragraphs[0].add_run("Ý nghĩa Nghiệp vụ")
    tbl_cs.cell(0, 0).width = Inches(1.5)
    tbl_cs.cell(0, 1).width = Inches(1.0)
    tbl_cs.cell(0, 2).width = Inches(2.2)
    tbl_cs.cell(0, 3).width = Inches(0.6)
    tbl_cs.cell(0, 4).width = Inches(1.2)
    for i, row in enumerate(constraints_summary):
        for j, val in enumerate(row):
            tbl_cs.cell(i+1, j).paragraphs[0].add_run(val)
    format_table(tbl_cs)

    add_callout(doc, "Giải thích DBA về CHECK và khóa ngoại",
                "Các quy tắc tiền, thời lượng phiên, số dư và liên kết nghiệp vụ được kiểm tra ngay trong PostgreSQL. "
                "Mọi client dùng chung contract nên không thể ghi dữ liệu lệch quy tắc chỉ vì bỏ qua giao diện.")

    doc.add_heading("2.3. Quy trình ETL và 4 Tiêu chí Chất lượng Dữ liệu (DQ Framework)", level=2)
    p = doc.add_paragraph(
        "Hệ thống triển khai pipeline làm sạch dữ liệu tự động bằng Python (python/etl.py). "
        "Bộ dữ liệu thử nghiệm 500 dòng khách hàng thô được nạp vào staging.stg_customers, trong đó cài cắm 15% lỗi cố ý. "
        "Kết quả xử lý thực tế thu được (bằng chứng lưu tại evidence/clean_data_stats.txt):"
    )
    p.paragraph_format.line_spacing = 1.15

    dq_stats = [
        ("Tổng số dòng dữ liệu thô xử lý", "500 dòng", "100.0%"),
        ("Số dòng chuẩn hóa thành công và nạp vào core.customer", "466 dòng", "93.2%"),
        ("Số dòng dữ liệu bẩn bị từ chối vào staging.rejected_rows", "34 dòng", "6.8%"),
        ("Số điện thoại được chuẩn hóa (+84, dấu chấm, dấu cách về 0xxxxxxxxx)", "8 trường hợp", "Tự động cứu"),
        ("Họ tên được chuẩn hóa (loại bỏ khoảng trắng thừa, định dạng Title Case)", "52 trường hợp", "Tự động cứu"),
        ("Lỗi số dư âm (NEGATIVE_BALANCE) -> Loại trừ an toàn", "14 dòng", "Vi phạm tài chính"),
        ("Lỗi SĐT sai độ dài/đầu số (PHONE_FORMAT_INVALID) -> Loại trừ", "8 dòng", "Không liên lạc được"),
        ("Lỗi trùng lặp số điện thoại (DUPLICATE_PHONE) -> Loại trừ", "12 dòng", "Trùng khóa hội viên")
    ]
    tbl_dq = doc.add_table(rows=len(dq_stats) + 1, cols=3)
    tbl_dq.cell(0, 0).paragraphs[0].add_run("Chỉ số Làm sạch Dữ liệu (ETL Metric)")
    tbl_dq.cell(0, 1).paragraphs[0].add_run("Giá trị Đo lường Thật")
    tbl_dq.cell(0, 2).paragraphs[0].add_run("Ghi chú Xử lý")
    tbl_dq.cell(0, 0).width = Inches(3.5)
    tbl_dq.cell(0, 1).width = Inches(1.5)
    tbl_dq.cell(0, 2).width = Inches(1.5)
    for i, row in enumerate(dq_stats):
        for j, val in enumerate(row):
            tbl_dq.cell(i+1, j).paragraphs[0].add_run(val)
    format_table(tbl_dq)

    p = doc.add_paragraph(
        "Đồng thời, hệ thống kiểm thử toàn diện 4 tiêu chuẩn chất lượng dữ liệu trong tests/data_quality_checks.sql:\n"
        "1. Tiêu chí SẠCH: 100% số điện thoại và email khớp biểu thức chính quy chuẩn quốc gia, không còn khoảng trắng rác.\n"
        "2. Tiêu chí SỐNG: Trạng thái tài khoản và máy trạm phản ánh trung thực trạng thái hoạt động thực tế.\n"
        "3. Tiêu chí ĐÚNG: Khớp nối 100% khóa ngoại giữa các schema, không phát sinh dòng mồ côi (orphan records).\n"
        "4. Tiêu chí ĐỦ: Toàn bộ các trường bắt buộc của phiên chơi và hóa đơn đều được điền đầy đủ, không bị rỗng NULL trái phép."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # CHƯƠNG 3: LẬP TRÌNH CSDL VÀ AN NINH BẢO MẬT PHÂN QUYỀN
    # -------------------------------------------------------------------------
    h3 = doc.add_heading("CHƯƠNG 3: LẬP TRÌNH CSDL VÀ AN NINH BẢO MẬT PHÂN QUYỀN", level=1)
    for r in h3.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    doc.add_heading("3.1. Các Stored Procedures và Triggers Nghiệp vụ", level=2)
    p = doc.add_paragraph(
        "Toàn bộ logic xử lý nghiệp vụ được đẩy vào trong PostgreSQL 17 để ứng dụng desktop chỉ đóng vai trò hiển thị giao diện:\n"
        "• core.sp_open_session(cust_id, comp_id, emp_id): Mở phiên chơi, kiểm tra số dư tối thiểu, tự động chuyển máy sang in_use.\n"
        "• billing.sp_close_session_and_pay(sess_id, cashier_id, method, promo_id): Kết thúc phiên, tự tính tiền giờ theo giây "
        "thông qua fn_calc_session_amount(), tổng hợp đơn F&B, áp dụng khuyến mãi, tạo hóa đơn, trừ ví/ghi sổ quỹ, trả máy về available.\n"
        "• billing.sp_top_up(cust_id, emp_id, amount, method): Nạp tiền tài khoản hội viên, cộng số dư và ghi sổ quỹ thanh toán.\n"
        "• inventory.sp_place_order(sess_id, emp_id, prod_id, qty): Đặt đồ ăn thức uống cho phiên chơi, kích hoạt Trigger tự động trừ tồn kho."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_heading("3.2. Hệ thống View và Materialized View Báo cáo Phân tích", level=2)
    views_info = [
        ("core.vw_computer_grid", "Regular View", "Hiển thị sơ đồ lưới 20 máy trạm thời gian thực: Mã máy, phòng, cấu hình, trạng thái, khách đang ngồi, giờ bắt đầu."),
        ("billing.vw_daily_revenue", "Regular View", "Thống kê doanh thu theo ngày: Tổng số hóa đơn, tiền giờ, tiền đồ ăn uống, tiền giảm giá và doanh thu thuần."),
        ("reporting.mv_monthly_revenue", "Materialized View", "Báo cáo doanh thu theo tháng kèm UNIQUE INDEX để hỗ trợ REFRESH MATERIALIZED VIEW CONCURRENTLY."),
        ("reporting.mv_top_customers", "Materialized View", "Xếp hạng khách hàng chi tiêu nhiều nhất và số phiên chơi phục vụ phân nhóm RFM."),
        ("reporting.mv_hourly_usage", "Materialized View", "Thống kê tần suất số lượt chơi theo khung giờ trong tuần phục vụ Module AI 5 phát hiện giờ thấp điểm.")
    ]
    tbl_vw = doc.add_table(rows=len(views_info) + 1, cols=3)
    tbl_vw.cell(0, 0).paragraphs[0].add_run("Tên View / Materialized View")
    tbl_vw.cell(0, 1).paragraphs[0].add_run("Loại View")
    tbl_vw.cell(0, 2).paragraphs[0].add_run("Mục đích Nghiệp vụ & Hiệu năng")
    tbl_vw.cell(0, 0).width = Inches(2.2)
    tbl_vw.cell(0, 1).width = Inches(1.3)
    tbl_vw.cell(0, 2).width = Inches(3.0)
    for i, row in enumerate(views_info):
        for j, val in enumerate(row):
            tbl_vw.cell(i+1, j).paragraphs[0].add_run(val)
    format_table(tbl_vw)

    doc.add_heading("3.3. Mô hình Kiểm soát Truy cập theo Vai trò (RBAC)", level=2)
    p = doc.add_paragraph(
        "Hệ thống phân định 5 vai trò (Roles) rõ rệt theo nguyên tắc đặc quyền tối thiểu (Least Privilege). "
        "Mỗi vai trò chỉ được cấp đúng các quyền SELECT, INSERT, UPDATE trên các bảng thuộc phạm vi công việc của mình:"
    )
    p.paragraph_format.line_spacing = 1.15

    rbac_data = [
        ("role_admin", "nv_admin", "Toàn quyền quản trị CSDL, cấu hình hệ thống, xem lương nhân viên, xử lý cảnh báo an ninh."),
        ("role_manager", "nv_manager", "Quản lý kinh doanh: xem toàn bộ hóa đơn, chỉnh sửa thực đơn món ăn, xem các báo cáo doanh thu."),
        ("role_cashier", "nv_thao, nv_nam", "Thu ngân: mở phiên, lập hóa đơn, nạp tiền, thu tiền. CHỈ ĐƯỢC THẤY HÓA ĐƠN DO MÌNH LẬP (qua RLS)."),
        ("role_staff", "nv_tuan", "Nhân viên phục vụ: xem danh sách máy, gọi món F&B. BỊ CHẶN HOÀN TOÀN KHỎI SCHEMA BILLING."),
        ("role_technician", "nv_hung", "Kỹ thuật viên: xem cấu hình máy trạm, cập nhật phiếu bảo trì. CHỈ THẤY PHIẾU GIAO CHO MÌNH (qua RLS).")
    ]
    tbl_rb = doc.add_table(rows=len(rbac_data) + 1, cols=3)
    tbl_rb.cell(0, 0).paragraphs[0].add_run("Role Nhóm")
    tbl_rb.cell(0, 1).paragraphs[0].add_run("User Demo")
    tbl_rb.cell(0, 2).paragraphs[0].add_run("Phạm vi Quyền hạn được Cấp")
    tbl_rb.cell(0, 0).width = Inches(1.3)
    tbl_rb.cell(0, 1).width = Inches(1.5)
    tbl_rb.cell(0, 2).width = Inches(3.7)
    for i, row in enumerate(rbac_data):
        for j, val in enumerate(row):
            tbl_rb.cell(i+1, j).paragraphs[0].add_run(val)
    format_table(tbl_rb)

    doc.add_heading("3.4. Column-Level Security & Row-Level Security (RLS)", level=2)
    p = doc.add_paragraph(
        "Bảo mật chuyên sâu được triển khai ở cả hai chiều:\n"
        "• Column-Level Security: Cột lương (salary) và mật khẩu (password_hash) trong bảng hr.employee bị thu hồi quyền truy cập đối với các role cấp thấp. "
        "Khi thu ngân cố tình chạy 'SELECT salary FROM hr.employee', PostgreSQL ném lỗi vi phạm đặc quyền (Lỗi 42501).\n"
        "• Row-Level Security (RLS): Kích hoạt với ENABLE ROW LEVEL SECURITY và FORCE ROW LEVEL SECURITY trên 4 bảng:\n"
        "   1. billing.invoice: Thu ngân nv_thao (ID 3) chỉ thấy 52 hóa đơn của mình; nv_nam (ID 4) chỉ thấy 59 hóa đơn của mình; quản lý thấy đủ 207 hóa đơn.\n"
        "   2. billing.payment: Thu ngân chỉ tra cứu được các giao dịch nạp tiền/thu tiền do chính mình tiếp nhận.\n"
        "   3. hr.employee: Nhân viên cấp dưới chỉ thấy dòng thông tin cá nhân của chính mình.\n"
        "   4. hr.maintenance: Kỹ thuật viên nv_hung (ID 6) chỉ thấy đúng phiếu bảo trì được giao cho mình."
    )
    p.paragraph_format.line_spacing = 1.15

    add_callout(doc, "Giải thích Thiết kế cho Hội đồng", 
                "Hàm hr.fn_get_current_employee_id() được định nghĩa SECURITY DEFINER và SET search_path = hr, pg_temp. "
                "Cơ chế này cho phép hàm tra cứu ID nhân viên an toàn mà không bị đệ quy chính sách RLS hay tấn công search_path hijacking.")

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # CHƯƠNG 4: TỐI ƯU HÓA HIỆU NĂNG VÀ VẬN HÀNH QUẢN TRỊ DBA
    # -------------------------------------------------------------------------
    h4 = doc.add_heading("CHƯƠNG 4: TỐI ƯU HÓA HIỆU NĂNG VÀ VẬN HÀNH QUẢN TRỊ DBA", level=1)
    for r in h4.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    doc.add_heading("4.1. Chiến lược Chỉ mục Đa dạng (Index Strategies)", level=2)
    p = doc.add_paragraph(
        "Để đáp ứng các loại hình truy vấn phức tạp của phòng game, hệ thống triển khai 5 loại chỉ mục chuyên biệt:\n"
        "1. B-Tree trên Khóa ngoại: Đánh chỉ mục trên gaming_session(customer_id), invoice(customer_id), food_order(session_id)... "
        "giúp chống quét toàn bảng khi JOIN và ngăn ngừa lock lan truyền khi cập nhật bảng cha.\n"
        "2. Partial Index (Chỉ mục có điều kiện): CREATE INDEX idx_session_active ON core.gaming_session(customer_id, computer_id) WHERE status = 'active'. "
        "Chỉ index các phiên đang chơi (vài chục dòng), kích thước nhỏ hơn 99% so với index toàn bảng, cache trọn vẹn trong RAM.\n"
        "3. GIN Trigram Index: Sử dụng pg_trgm trên core.customer(full_name gin_trgm_ops) hỗ trợ tìm kiếm mờ ILIKE '%keyword%' không cần quét tuần tự.\n"
        "4. GIN jsonb_path_ops: Đánh chỉ mục trên core.computer(specs) phục vụ lọc máy theo cấu hình phần cứng (@> '{\"gpu\":\"RTX 4070\"}').\n"
        "5. BRIN Index (Block Range Index): Đánh chỉ mục trên audit.audit_log(logged_at). BRIN chỉ lưu min/max từng cụm block đĩa vật lý, "
        "dung lượng siêu nhỏ (<1% so với B-Tree) nhưng tối ưu vượt trội cho bảng nhật ký append-only."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_heading("4.2. Kết quả Đo lường Thực tế EXPLAIN (ANALYZE, BUFFERS)", level=2)
    p = doc.add_paragraph(
        "Toàn bộ số liệu dưới đây được đo lường thực tế trên máy chủ PostgreSQL 17 cục bộ (bằng chứng tại evidence/explain_before_after.txt):"
    )
    p.paragraph_format.line_spacing = 1.15

    explain_data = [
        ("Q1: Phiên đang active", "Seq Scan (cost 6.60, time 0.059 ms)", "Partial Index Scan (cost 0.00..6.60, time 0.023 ms)", "Giảm 61.0%", "Bộ nhớ đệm chỉ cần 4 blocks"),
        ("Q2: Tìm tên '%Thành%'", "Seq Scan (cost 16.96, time 0.147 ms)", "Bitmap Index Scan (idx_customer_fullname_trgm)", "Chuyển Scan", "Sử dụng GIN pg_trgm tách 3-gram"),
        ("Q3: Lọc GPU RTX 4070", "Seq Scan (cost 12.75, time 0.023 ms)", "Bitmap Index Scan (idx_computer_specs_gin)", "Giảm 43.5%", "GIN jsonb_path_ops định tuyến chính xác"),
        ("Q4: Hóa đơn theo khách", "Seq Scan + Sort (cost 5.60, time 0.047 ms)", "Index Scan (idx_invoice_customer_id, time 0.033 ms)", "Giảm 29.8%", "Loại bỏ hoàn toàn chi phí Sort đĩa")
    ]
    tbl_ex = doc.add_table(rows=len(explain_data) + 1, cols=5)
    tbl_ex.cell(0, 0).paragraphs[0].add_run("Truy vấn Kiểm định")
    tbl_ex.cell(0, 1).paragraphs[0].add_run("Trước Tối ưu (Before)")
    tbl_ex.cell(0, 2).paragraphs[0].add_run("Sau Tối ưu (After)")
    tbl_ex.cell(0, 3).paragraphs[0].add_run("Cải thiện")
    tbl_ex.cell(0, 4).paragraphs[0].add_run("Cơ chế Tối ưu")
    tbl_ex.cell(0, 0).width = Inches(1.5)
    tbl_ex.cell(0, 1).width = Inches(1.5)
    tbl_ex.cell(0, 2).width = Inches(1.8)
    tbl_ex.cell(0, 3).width = Inches(0.8)
    tbl_ex.cell(0, 4).width = Inches(0.9)
    for i, row in enumerate(explain_data):
        for j, val in enumerate(row):
            tbl_ex.cell(i+1, j).paragraphs[0].add_run(val)
    format_table(tbl_ex)

    doc.add_heading("4.3. Tinh chỉnh Heap-Only Tuples (HOT) và Autovacuum", level=2)
    p = doc.add_paragraph(
        "Trên các bảng có tần suất cập nhật cao như gaming_session và computer, hệ thống cấu hình fillfactor < 100:\n"
        "• ALTER TABLE core.gaming_session SET (fillfactor = 90, autovacuum_vacuum_scale_factor = 0.05);\n"
        "• ALTER TABLE core.computer SET (fillfactor = 85, autovacuum_vacuum_scale_factor = 0.05);\n"
        "Ý nghĩa DBA: Giữ lại 10% - 15% khoảng trống trên mỗi trang đĩa dữ liệu để khi UPDATE trạng thái hoặc số dư, "
        "bản ghi mới được ghi ngay trên cùng trang (Heap-Only Tuple). Khi đó PostgreSQL không cần ghi đè lại các chỉ mục B-Tree, "
        "giảm thiểu triệt để I/O đĩa cứng và ngăn ngừa hiện tượng phình to chỉ mục (index bloat)."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_heading("4.4. Phân vùng Vật lý Range Partitioning & Tự động hóa", level=2)
    p = doc.add_paragraph(
        "Bảng kiểm toán an ninh audit.audit_log được cấu hình PARTITION BY RANGE (logged_at) theo từng tháng. "
        "Để giải phóng DBA khỏi công việc vận hành thủ công, hệ thống cung cấp hàm tự động hóa:\n"
        "SELECT * FROM audit.fn_create_monthly_partitions(3);\n"
        "Hàm tự động quét lịch, tính toán biên thời gian chuẩn UTC ('YYYY-MM-01 00:00:00+00') và tự động sinh "
        "các bảng phân vùng vật lý cho 3 tháng kế tiếp mà không làm gián đoạn hệ thống."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_heading("4.5. Chiến lược Sao lưu & Thử nghiệm Phục hồi Thảm họa (Disaster Recovery)", level=2)
    p = doc.add_paragraph(
        "Hệ thống cung cấp 2 bộ kịch bản sao lưu tự động: backup/backup.ps1 (cho Windows) và backup/backup.sh (cho Linux/Docker). "
        "Đồng thời, quy trình phục hồi thảm họa đã được kiểm thử thực tế bằng script backup/test_disaster_recovery.py:\n"
        "1. Tạo bản sao lưu nhị phân nén pg_dump -Fc (kích thước ~265 KB).\n"
        "2. Tạo một CSDL độc lập netcafe_restore từ template0 UTF-8.\n"
        "3. Phục hồi toàn bộ dữ liệu qua pg_restore.\n"
        "4. Đối chiếu số dòng dữ liệu giữa 2 CSDL: 100% các bảng (room, computer, customer, gaming_session, invoice, payment, maintenance...) "
        "khớp số dòng tuyệt đối (Ghi nhận tại evidence/backup_restore.log)."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # CHƯƠNG 5: 5 MODULE TỰ ĐỘNG HÓA THÔNG MINH (IN-DATABASE AI) & CLIENT
    # -------------------------------------------------------------------------
    h5 = doc.add_heading("CHƯƠNG 5: 5 MODULE TỰ ĐỘNG HÓA THÔNG MINH (IN-DATABASE AI) & CLIENT", level=1)
    for r in h5.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    p = doc.add_paragraph(
        "Khái niệm 'AI' trong đề tài được định nghĩa chuẩn xác theo nguyên tắc Quản trị Dữ liệu nâng cao: "
        "In-Database Intelligence - Khai phá dữ liệu và tự động hóa thông minh chạy 100% bằng thuật toán toán học, "
        "thống kê và hàm PL/pgSQL nội bộ trong PostgreSQL 17, không phụ thuộc bất kỳ API hay thư viện bên ngoài."
    )
    p.paragraph_format.line_spacing = 1.15

    ai_modules = [
        ("Module 1: Cảnh báo An ninh & Chấm điểm Rủi ro (Audit Risk Scoring)",
         "Trigger audit.fn_audit_security_alert_trigger() gắn trên employee, payment, invoice. "
         "Tự động tính toán Risk Score (0-100) dựa trên quy tắc: xóa dữ liệu (+30đ), sửa lương (+45đ), hủy hóa đơn (+50đ), "
         "nạp tiền lớn >= 5 triệu (+40đ), ngoài giờ làm việc (+25đ). Nếu Risk >= 40, ghi vào ai.ai_alert và phát tín hiệu "
         "PERFORM pg_notify('ai_security_alert', ...) tới phần mềm desktop thời gian thực."),
        
        ("Module 2: Tự động Khuyến nghị Chỉ mục (Index Recommendation Engine)",
         "Hàm ai.fn_recommend_indexes() quét bảng catalog pg_stat_user_tables tìm các bảng có tỷ lệ quét tuần tự (seq_scan) "
         "cao vượt trội so với quét chỉ mục (idx_scan), đồng thời rà soát catalog pg_constraint tìm các cột khóa ngoại (Foreign Key) "
         "chưa có B-Tree index. Tự động sinh câu lệnh CREATE INDEX đề xuất lưu vào ai.index_recommendation."),

        ("Module 3: Phát hiện Khách hàng Trùng lặp Mờ (Duplicate Customer Detection)",
         "Hàm ai.fn_detect_duplicates(p_similarity_threshold) sử dụng extension pg_trgm và hàm similarity() kết hợp unaccent(lower()) "
         "để tính khoảng cách chuỗi Trigram giữa các họ tên khách hàng, kết hợp so khớp 7 số đuôi điện thoại. "
         "Phát hiện chính xác cặp tài khoản nghi vấn như 'Tran Van Binh' và 'Trần Văn Bình' (độ tương đồng 0.85+) lưu vào ai.duplicate_candidate."),

        ("Module 4: Phát hiện Bất thường Thống kê qua Z-Score (Statistical Anomaly Detection)",
         "Hàm ai.fn_detect_anomalies(p_z_threshold) áp dụng công thức chuẩn hóa thống kê: Z = |X - Mean| / StdDev với hàm AVG() "
         "và STDDEV_SAMP(). Phát hiện ngoại lai trên 2 chiều: (1) Phiên chơi có thời lượng bất thường (>35 giờ do quên tắt máy, Z=12.01) "
         "và (2) Giao dịch nạp tiền đột biến bất thường (15.000.000 VNĐ, Z=11.59). Ghi nhận vào ai.anomaly_flag."),

        ("Module 5: Gợi ý Khuyến mãi Thông minh (Smart Promotion Suggestion)",
         "Hàm ai.fn_suggest_promotions() kết hợp 2 kỹ thuật khai phá dữ liệu từ Materialized Views:\n"
         "• Phân tích giờ thấp điểm: Quét reporting.mv_hourly_usage tìm các khung giờ (0h, 3h, 4h) có lượng khách dưới 70% trung bình ngày "
         "để đề xuất chiến dịch Happy Hour giảm 25% tiền giờ.\n"
         "• Phân khúc khách hàng RFM: Quét reporting.mv_top_customers tìm nhóm khách VIP chi tiêu cao nhưng không quay lại phòng máy trong "
         "3 - 17 ngày qua để đề xuất Voucher Win-Back tri ân 30.000 VNĐ. Ghi vào ai.promotion_suggestion.")
    ]

    for title, desc in ai_modules:
        doc.add_heading(title, level=2)
        p = doc.add_paragraph(desc)
        p.paragraph_format.line_spacing = 1.15
        p.paragraph_format.space_after = Pt(6)

    doc.add_heading("5.6. Phần mềm Desktop Client Mỏng (.NET 10 WinForms)", level=2)
    p = doc.add_paragraph(
        "Phần mềm Desktop Client được xây dựng bằng C# .NET 10 WinForms theo kiến trúc mỏng (Thin Client):\n"
        "• Ứng dụng KHÔNG chứa bất kỳ logic tính toán nghiệp vụ nào mà gọi 100% Stored Procedures và Views của PostgreSQL.\n"
        "• Giao diện gồm 6 tab trực quan: (1) Sơ đồ máy trạm trực quan đổi màu theo trạng thái; (2) Quản lý phiên chơi; "
        "(3) Khách hàng & Nạp tiền; (4) Thực đơn & Gọi món F&B; (5) Báo cáo doanh thu; (6) 5 Module AI.\n"
        "• Dải giám sát SQL Strip ở đáy màn hình: Hiển thị câu lệnh SQL đang thực thi và thời gian phản hồi miligiây của CSDL.\n"
        "• Thanh cảnh báo lỗi CSDL màu đỏ: Khi CSDL ném lỗi vi phạm ràng buộc (Check, Unique, Foreign Key), "
        "ứng dụng bóc tách mã lỗi PostgreSQL (SQLSTATE) và tên ràng buộc để hiển thị trực tiếp cho người dùng."
    )
    p.paragraph_format.line_spacing = 1.15

    doc.add_page_break()

    # -------------------------------------------------------------------------
    # CHƯƠNG 6: KẾT LUẬN VÀ KẾT QUẢ ĐẠT ĐƯỢC
    # -------------------------------------------------------------------------
    h6 = doc.add_heading("CHƯƠNG 6: KẾT LUẬN VÀ KẾT QUẢ ĐẠT ĐƯỢC", level=1)
    for r in h6.runs:
        r.font.name = "Times New Roman"
        r.font.color.rgb = COLOR_PRIMARY

    p = doc.add_paragraph(
        "Đề tài đã hoàn thành xuất sắc toàn bộ các mục tiêu đặt ra cho bài tiểu luận môn Quản trị Cơ sở Dữ liệu. "
        "Hệ thống đạt tính chuẩn mực cao về thiết kế, tính toàn vẹn, bảo mật, hiệu năng và khả năng tự động hóa:"
    )
    p.paragraph_format.line_spacing = 1.15

    results_matrix = [
        ("1. Kiến trúc CSDL", "1 Database netcafe_db, 8 schemas, 22 bảng, đạt chuẩn 3NF, độc lập locale OS template0.", "ĐẠT 100%"),
        ("2. Ràng buộc toàn vẹn", "16/16 Test cases PASS, bảo vệ công thức hóa đơn và dữ liệu phiên.", "ĐẠT 100%"),
        ("3. Chất lượng dữ liệu (DQ)", "4 Tiêu chuẩn Sạch - Sống - Đúng - Đủ, luồng ETL tự động hóa, lọc 6.8% dữ liệu bẩn.", "ĐẠT 100%"),
        ("4. Bảo mật CSDL", "RBAC 5 vai trò, Column-Level Security (salary, password), RLS trên 4 bảng.", "ĐẠT 100%"),
        ("5. Tối ưu hóa hiệu năng", "5 loại chỉ mục (B-Tree, Partial, GIN Trigram, GIN JSONB, BRIN), HOT tuning fillfactor.", "ĐẠT 100%"),
        ("6. Vận hành DBA", "Tự động tạo partition audit_log theo tháng, kịch bản backup & restore khớp 100% dòng.", "ĐẠT 100%"),
        ("7. 5 Module In-Database AI", "Cảnh báo an ninh NOTIFY, tư vấn index, phát hiện khách trùng, Z-Score, gợi ý khuyến mãi.", "ĐẠT 100%"),
        ("8. Ứng dụng Desktop", "WinForms .NET 10 mỏng, hiển thị sơ đồ máy, dải SQL Strip và thanh lỗi CSDL màu đỏ.", "ĐẠT 100%"),
        ("9. Khả năng tái tạo", "File tổng hợp netcafe_full.sql chạy lại 2 lần liên tiếp không lỗi, hoàn toàn idempotent.", "ĐẠT 100%")
    ]
    tbl_res = doc.add_table(rows=len(results_matrix) + 1, cols=3)
    tbl_res.cell(0, 0).paragraphs[0].add_run("Nội dung Tiêu chí Đánh giá")
    tbl_res.cell(0, 1).paragraphs[0].add_run("Kết quả Nghiệm thu Thực tế")
    tbl_res.cell(0, 2).paragraphs[0].add_run("Đánh giá")
    tbl_res.cell(0, 0).width = Inches(2.2)
    tbl_res.cell(0, 1).width = Inches(3.8)
    tbl_res.cell(0, 2).width = Inches(1.0)
    for i, row in enumerate(results_matrix):
        for j, val in enumerate(row):
            tbl_res.cell(i+1, j).paragraphs[0].add_run(val)
    format_table(tbl_res)

    p_end = doc.add_paragraph("\nBáo cáo tiểu luận được hoàn thành với đầy đủ bằng chứng kiểm thử thực tế và mã nguồn sẵn sàng cho buổi bảo vệ trực tiếp.")
    p_end.paragraph_format.line_spacing = 1.15

    out_file = os.path.join("docs", "BaoCao_QuanTriCSDL_NetCafe.docx")
    doc.save(out_file)
    print(f"[+] ĐÃ TẠO THÀNH CÔNG BÁO CÁO WORD TẠI: {out_file}")

if __name__ == "__main__":
    build_report()
