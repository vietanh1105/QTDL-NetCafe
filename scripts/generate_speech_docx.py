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
DARK_TEXT = RGBColor(15, 23, 42)      # #0F172A
ACTION_COLOR = RGBColor(180, 83, 9)   # #B45309 Amber Dark

def set_cell_background(cell, hex_color):
    shading_xml = f'<w:shd {nsdecls("w")} w:fill="{hex_color}"/>'
    cell._tc.get_or_add_tcPr().append(parse_xml(shading_xml))

def set_cell_margins(cell, top=140, bottom=140, start=200, end=200):
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
    p.paragraph_format.space_before = Pt(16)
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = 'Calibri'
    run.font.size = Pt(15)
    run.font.bold = True
    run.font.color.rgb = NAVY_BLUE
    return p

def add_heading_2(doc, text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = 'Calibri'
    run.font.size = Pt(12.5)
    run.font.bold = True
    run.font.color.rgb = DEEP_BLUE
    return p

def add_metaphor_box(doc, title, content):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.rows[0].cells[0]
    cell.width = Inches(6.5)
    set_cell_background(cell, 'FEF3C7')  # Vàng hổ phách nhạt
    set_cell_margins(cell, top=140, bottom=140, start=200, end=200)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(2)
    run_t = p.add_run(f"💡 VÍ DỤ ĐỜI THƯỜNG GIÚP THẦY CÔ DỄ HÌNH DUNG: {title.upper()}\n")
    run_t.font.name = 'Calibri'
    run_t.font.size = Pt(10.5)
    run_t.font.bold = True
    run_t.font.color.rgb = RGBColor(146, 64, 14)
    
    run_c = p.add_run(content)
    run_c.font.name = 'Calibri'
    run_c.font.size = Pt(10.5)
    run_c.font.italic = True
    run_c.font.color.rgb = RGBColor(120, 53, 15)
    
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(4)

def add_action_box(doc, action_text):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.rows[0].cells[0]
    cell.width = Inches(6.5)
    set_cell_background(cell, 'F1F5F9')  # Xám xanh Slate nhạt
    set_cell_margins(cell, top=100, bottom=100, start=180, end=180)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(0)
    run = p.add_run(f"👉 THAO TÁC TRÊN MÁY: {action_text}")
    run.font.name = 'Calibri'
    run.font.size = Pt(10.5)
    run.font.bold = True
    run.font.color.rgb = ACTION_COLOR

def add_speech_card(doc, speech_text):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.rows[0].cells[0]
    cell.width = Inches(6.5)
    set_cell_background(cell, 'F0FDF4')  # Xanh lục ngọc nhạt tạo cảm giác dễ chịu
    set_cell_margins(cell, top=160, bottom=160, start=220, end=220)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.line_spacing = 1.25
    
    run_lbl = p.add_run("🗣️ LỜI THOẠI THUYẾT TRÌNH (BẠN ĐỌC PHẦN NÀY):\n")
    run_lbl.font.name = 'Calibri'
    run_lbl.font.size = Pt(11)
    run_lbl.font.bold = True
    run_lbl.font.color.rgb = RGBColor(22, 101, 52)  # Xanh lá đậm
    
    run_text = p.add_run(f'"{speech_text}"')
    run_text.font.name = 'Calibri'
    run_text.font.size = Pt(11.5)
    run_text.font.color.rgb = DARK_TEXT
    
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(6)

def build_speech_document(output_path):
    print(f"Building spoken speech Word document at: {output_path}")
    doc = docx.Document()
    
    for section in doc.sections:
        section.top_margin = Inches(1.0)
        section.bottom_margin = Inches(1.0)
        section.left_margin = Inches(1.0)
        section.right_margin = Inches(1.0)
        
    # Header Trường & Đề tài
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
    p_title.paragraph_format.space_before = Pt(8)
    p_title.paragraph_format.space_after = Pt(4)
    run_t = p_title.add_run("KỊCH BẢN LỜI THOẠI THUYẾT TRÌNH BẢO VỆ TIỂU LUẬN\nQUẢN TRỊ CƠ SỞ DỮ LIỆU")
    run_t.font.name = "Calibri"
    run_t.font.size = Pt(17)
    run_t.font.bold = True
    run_t.font.color.rgb = NAVY_BLUE
    
    p_sub = doc.add_paragraph()
    p_sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_sub.paragraph_format.space_before = Pt(2)
    p_sub.paragraph_format.space_after = Pt(12)
    run_s = p_sub.add_run("ĐỀ TÀI: HỆ THỐNG QUẢN LÝ PHÒNG GAME / NET (NETCAFE MANAGEMENT SYSTEM)\n"
                          "Sinh viên báo cáo: Nguyễn Việt Anh | Nền tảng: PostgreSQL 16 (Neon Cloud) | 5 Module AI")
    run_s.font.name = "Calibri"
    run_s.font.size = Pt(11)
    run_s.font.italic = True
    run_s.font.color.rgb = DEEP_BLUE
    
    # Hướng dẫn sử dụng tài liệu
    add_metaphor_box(
        doc,
        "HƯỚNG DẪN DÀNH CHO BẠN KHI LÊN BẢO VỆ",
        "Tài liệu này được thiết kế chuyên biệt phục vụ việc đọc thoại khi đứng trước Hội đồng. "
        "Mỗi bước gồm 2 phần: [THAO TÁC TRÊN MÁY] (chỉ dẫn bạn bấm phím ở cửa sổ nào) và [LỜI THOẠI THUYẾT TRÌNH] "
        "(nội dung nói trôi chảy, chuyên nghiệp, giải thích rõ cơ chế CSDL và chỉ thẳng vào kết quả trên màn hình)."
    )
    
    # LỜI MỞ ĐẦU
    add_heading_1(doc, "LỜI MỞ ĐẦU: XIN CHÀO VÀ GIỚI THIỆU TỔNG QUAN")
    add_action_box(doc, "Đứng trang nghiêm, phong thái tự tin, mắt nhìn bao quát Thầy Cô trong Hội đồng chấm.")
    add_speech_card(
        doc,
        "Kính thưa Thầy Chủ tịch Hội đồng, quý Thầy Cô trong Hội đồng chấm tiểu luận và toàn thể các bạn sinh viên!\n\n"
        "Em tên là Nguyễn Việt Anh, đại diện nhóm thực hiện đề tài 'Hệ thống Quản lý Phòng máy NetCafe chuyên nghiệp'. "
        "Hôm nay, nhóm chúng em rất vinh dự được trình bày và bảo vệ sản phẩm quản trị cơ sở dữ liệu trước Hội đồng.\n\n"
        "Bài toán quản lý phòng máy chơi game đặt ra thách thức rất lớn về độ trễ thời gian thực, tính toàn vẹn tài chính, "
        "bảo mật phân quyền chặt chẽ giữa các ca trực và khả năng tự động hóa tối ưu khi quy mô dữ liệu mở rộng. "
        "Để giải quyết triệt để bài toán này, nhóm đã xây dựng hệ thống trên nền tảng PostgreSQL Cloud của Neon, "
        "phân tách rành mạch thành 8 Schemas độc lập, thiết lập kiến trúc an ninh đa tầng Fail-Safe và tích hợp "
        "5 Module AI hoạt động trực tiếp trong lòng CSDL.\n\n"
        "Sau đây, em xin phép bắt đầu phần báo cáo và thực nghiệm trực tiếp hệ thống qua 5 nội dung trọng tâm!"
    )
    
    # PHẦN 1
    add_heading_1(doc, "PHẦN 1: KHỞI TẠO TỰ ĐỘNG, PIPELINE ETL VÀ RÀNG BUỘC TOÀN VẸN FAIL-SAFE")
    add_metaphor_box(
        doc,
        "Nhà máy lọc nước và Cửa an ninh sân bay",
        "Dữ liệu người dùng nạp vào như nguồn nước thô chưa lọc. Tầng Staging đóng vai trò như bể lắng và hệ thống lọc (ETL): "
        "các bản ghi lỗi hoặc có dấu hiệu gian lận bị giữ lại ở bể cách ly, chỉ dữ liệu sạch đạt chuẩn 3NF mới được nạp vào bể chính. "
        "Đồng thời, Check Constraint và Foreign Key đóng vai trò như cửa an ninh sân bay: hành lý sai quy cách "
        "hoặc vé không hợp lệ đều bị chặn đứng ngay lập tức tại cửa khẩu CSDL."
    )
    
    add_heading_2(doc, "Bước 1.1: Khởi tạo CSDL Cloud sạch sẽ tự động bằng Python")
    add_action_box(doc, "Mở cửa sổ Terminal PowerShell bên phải -> Chạy lệnh reset --wipe rồi chạy tiếp reset --init.")
    add_speech_card(
        doc,
        "Kính thưa Thầy Cô và Hội đồng, trước tiên nhóm khởi tạo môi trường sạch sẽ trên Cloud. "
        "Khi cần làm mới toàn bộ hệ thống, nhóm chạy lệnh reset wipe để xóa sạch các bảng và schemas cũ trong vòng nửa giây. "
        "Tiếp theo, nhóm chạy lệnh reset init để tự động thiết lập hoàn chỉnh 8 Schemas, toàn bộ các bảng, ràng buộc và nạp dữ liệu gốc. "
        "Như Thầy Cô thấy trên màn hình Terminal bên phải, chỉ trong chưa đầy 10 giây, toàn bộ nền tảng CSDL đã sẵn sàng hoạt động."
    )
    
    add_heading_2(doc, "Bước 1.2: Sinh dữ liệu thô (Dirty Data) và Chạy luồng ETL chuẩn hóa")
    add_action_box(doc, "Terminal bên phải: chạy generate_dirty_data.py rồi clean_data.py -> Mở Neon SQL bên trái xem 3 bảng.")
    add_speech_card(
        doc,
        "Để kiểm chứng khả năng xử lý dữ liệu thực tế, nhóm chạy công cụ sinh ra 20 bản ghi dữ liệu bẩn mô phỏng người dùng nhập liệu "
        "lộn xộn: tên viết hoa thường tùy tiện, thừa khoảng trắng, số điện thoại dính đầu số quốc tế hoặc dấu chấm, thậm chí có những bản ghi "
        "số dư tiền bị âm. Sau đó, nhóm kích hoạt tiến trình ETL bằng Python để tự động làm sạch.\n\n"
        "Kính mời Thầy Cô quan sát cửa sổ Neon SQL bên trái:\n"
        "+ Đầu tiên, ở bảng dữ liệu thô Staging, Thầy Cô thấy rõ toàn bộ các dòng chứa đầy đủ các lỗi nhập liệu thực tế.\n"
        "+ Thứ hai, ở bảng cách ly dữ liệu lỗi rejected_rows, hệ thống đã phát hiện và chặn đứng đúng 4 bản ghi rủi ro: "
        "gồm 2 bản ghi tiền âm (-50k và -100k) và 2 bản ghi sai độ dài số điện thoại, kèm nguyên nhân chi tiết.\n"
        "+ Thứ ba, ở bảng khách hàng chính core.customer, toàn bộ các khách hàng sạch đã được chuẩn hóa họ tên chuẩn mực, định dạng số điện thoại, "
        "tự động tạo tài khoản, sinh email hợp lệ và số dư dương đạt chuẩn 3NF."
    )
    
    add_heading_2(doc, "Bước 1.3: Thử nghiệm rủi ro Fail-Safe chặn đứng ở tầng nhân CSDL")
    add_action_box(doc, "Cửa sổ Neon SQL bên trái: Chạy 3 câu lệnh vi phạm (Chèn tiền âm, Chèn khách 9999, Xóa phòng máy RESTRICT).")
    add_speech_card(
        doc,
        "Để chứng minh cơ chế Fail-Safe bảo vệ tuyệt đối ở tầng nhân CSDL chứ không phó mặc cho code ứng dụng:\n"
        "+ Dòng lệnh 1: Nhóm cố tình chèn khách hàng có số dư âm -50.000 VNĐ. CSDL lập tức từ chối và báo lỗi vi phạm ràng buộc CHECK ck_customer_balance.\n"
        "+ Dòng lệnh 2: Nhóm thử tạo hóa đơn với mã khách hàng 9999 không tồn tại. CSDL chặn lại ngay vì vi phạm khóa ngoại FOREIGN KEY.\n"
        "+ Dòng lệnh 3: Nhóm cố tình xóa phòng máy số 1 - nơi đang có các máy trạm thi đấu hoạt động. CSDL từ chối lệnh xóa ngay lập tức "
        "nhờ cơ chế ON DELETE RESTRICT computer_room_id_fkey, bảo vệ dữ liệu con không bị mồ côi."
    )
    
    # PHẦN 2
    add_heading_1(doc, "PHẦN 2: BẢO MẬT PHÂN QUYỀN ĐA TẦNG (RBAC, CLS, RLS) VÀ MODULE AI 3")
    add_metaphor_box(
        doc,
        "Két tiền ca trực và Bảng lương giám đốc",
        "Thu ngân ca trực chỉ được mở và kiểm đếm ngăn két tiền của chính mình trong ca làm việc, tuyệt đối không được nhìn sang "
        "két tiền của thu ngân ca khác (đó là RLS - Row-Level Security). Đồng thời thu ngân chỉ có quyền xem danh bạ nội bộ để liên lạc "
        "ca kíp chứ không thể xem lén cột tiền lương của đồng nghiệp và quản lý (đó là CLS - Column-Level Security)."
    )
    
    add_heading_2(doc, "Bước 2.1 & 2.2: Đóng vai Thu ngân Thảo và Chứng minh Bảo mật cấp hàng (RLS)")
    add_action_box(doc, "Neon SQL bên trái: Chạy SET ROLE nv_thao rồi truy vấn toàn bộ bảng billing.invoice.")
    add_speech_card(
        doc,
        "Bây giờ, nhóm đóng vai nhân viên thu ngân Thảo ở ca sáng bằng lệnh SET ROLE nv_thao. "
        "Hệ thống trả về thông báo xác nhận phiên làm việc đã chuyển sang vai trò thu ngân Thảo.\n\n"
        "Tiếp theo, thu ngân Thảo chạy truy vấn xem tất cả hóa đơn trên toàn bảng. Mặc dù câu lệnh truy vấn hoàn toàn không có mệnh đề WHERE, "
        "nhờ chính sách RLS cài cắm tại tầng nhân CSDL, Thảo chỉ nhìn thấy duy nhất đúng 6 hóa đơn (ID: 1, 2, 3, 7, 8, 9) do chính cashier_id = 3 "
        "của mình lập. Toàn bộ các hóa đơn số 4, 5, 6 của nhân viên Nam ở ca khác đều bị ẩn hoàn toàn, ngăn chặn tuyệt đối nguy cơ tò mò hay gian lận nội bộ."
    )
    
    add_heading_2(doc, "Bước 2.3: Đổi vai Quản lý để đối chiếu toàn quyền giám sát")
    add_action_box(doc, "Neon SQL bên trái: Chạy RESET ROLE, SET ROLE nv_manager rồi xem toàn bộ 9 hóa đơn và bảng tổng hợp.")
    add_speech_card(
        doc,
        "Để đối chứng, nhóm chuyển sang vai trò Quản lý phòng máy bằng SET ROLE nv_manager. "
        "Do có đặc quyền BYPASSRLS, ở câu truy vấn thứ hai Quản lý nhìn thấy đầy đủ toàn bộ 9 hóa đơn của cả nhân viên Thảo (6 hóa đơn) "
        "lẫn nhân viên Nam (3 hóa đơn). Ở câu truy vấn thứ ba, Quản lý xem được báo cáo tổng hợp doanh thu theo từng nhân viên ca trực: "
        "ca của Thảo lập 6 hóa đơn đạt 840 nghìn đồng, ca của Nam lập 3 hóa đơn đạt 72 nghìn đồng."
    )
    
    add_heading_2(doc, "Bước 2.4 & 2.5: Bảo mật cấp cột (CLS) và Truy cập an toàn qua View công khai")
    add_action_box(doc, "Neon SQL bên trái: SET ROLE nv_thao, thử SELECT cột salary từ bảng employee, sau đó SELECT từ View hr.vw_employee_public.")
    add_speech_card(
        doc,
        "Tiếp theo, thu ngân Thảo chuyển lại vai trò và cố tình truy cập vào cột salary để xem trộm tiền lương của đồng nghiệp. "
        "CSDL lập tức kích hoạt cơ chế CLS và trả về lỗi từ chối truy cập ERROR: permission denied for table employee.\n\n"
        "Để nhân viên vẫn làm việc phối hợp được mà không lộ thông tin nhạy cảm, hệ thống cấp quyền cho Thảo tra cứu qua View an toàn "
        "hr.vw_employee_public. Qua View này, Thảo xem được đầy đủ họ tên, số điện thoại, tài khoản, ca trực và trạng thái đang hoạt động (is_active = t) "
        "của 4 nhân viên thực tế để tiện giao ca, nhưng cột tiền lương đã được che giấu an toàn tuyệt đối."
    )
    
    add_heading_2(doc, "Bước 2.6: Module AI 3 - Phát hiện tài khoản trùng lặp gian lận bằng pg_trgm")
    add_action_box(doc, "Neon SQL bên trái: Chạy hàm SELECT * FROM ai.fn_detect_duplicates(0.65).")
    add_speech_card(
        doc,
        "Nhiều khách hàng thường tạo nhiều tài khoản hoặc dùng chung số điện thoại để trục lợi bào mã giảm giá tân thủ. "
        "Nhóm đã tích hợp Module AI 3 ứng dụng giải thuật so khớp mờ Fuzzy Trigram Matching của extension pg_trgm.\n\n"
        "Khi nhóm thực thi hàm với ngưỡng tương đồng 0.65, AI tự động quét toàn bộ cơ sở dữ liệu và phát hiện chính xác 4 cặp tài khoản nghi vấn: "
        "gồm cặp trùng 7 số cuối điện thoại và họ tên tương đồng, cùng cặp tài khoản viết không dấu 'Tran Van Binh' tương đồng 100% "
        "với tài khoản có dấu 'Trần Văn Bình' để gắn cờ gian lận danh tính."
    )
    
    # PHẦN 3
    add_heading_1(doc, "PHẦN 3: TỐI ƯU HÓA HIỆU NĂNG VÀ MODULE AI 2 (PERFORMANCE DEMO)")
    add_metaphor_box(
        doc,
        "Mục lục kho lưu trữ và Bác sĩ khám bệnh CSDL",
        "Khi tìm kiếm một tờ hóa đơn trong kho lưu trữ hàng triệu tài liệu: nếu không có mục lục sắp xếp, ta buộc phải lật từng tờ giấy "
        "từ đầu đến cuối rất tốn thời gian (Seq Scan). Nhưng nếu có mục lục chỉ mục, ta nhảy thẳng tới đúng ngăn tủ chứa hồ sơ chỉ trong 1 tích tắc (Index Scan). "
        "Module AI 2 giống như một bác sĩ khám bệnh CSDL: tự động rà soát nhật ký truy vấn để phát hiện các bảng bị nghẽn và kê đơn tạo chỉ mục phù hợp."
    )
    
    add_heading_2(doc, "Bước 3.1: Benchmark tự động trên 20.000 bản ghi")
    add_action_box(doc, "Neon SQL bên trái: Chạy hàm SELECT * FROM ai.fn_compare_index_benchmark().")
    add_speech_card(
        doc,
        "Thưa Thầy Cô, để chứng minh sức mạnh vượt trội của Index trên quy mô lớn, nhóm chạy hàm Benchmark tự động kiểm thử trên 20.000 bản ghi. "
        "Bảng kết quả in ra tiếng Việt rất trực quan: Quét tuần tự Seq Scan phải đọc duyệt qua 128 block bộ nhớ và mất gần 1 mili-giây. "
        "Trong khi đó, B-Tree Index Scan chỉ cần đọc đúng 2 đến 3 block bộ nhớ và chỉ mất 0.17 mili-giây, tốc độ truy xuất nhanh hơn gấp 5.3 lần."
    )
    
    add_heading_2(doc, "Bước 3.2: Thao tác thủ công đối chiếu EXPLAIN ANALYZE trên bảng lịch sử 25.000 dòng")
    add_action_box(doc, "Neon SQL bên trái: DROP index, chạy EXPLAIN Seq Scan trên invoice_history, sau đó CREATE INDEX và chạy lại EXPLAIN.")
    add_speech_card(
        doc,
        "Nhóm tiếp tục đối chiếu thực tế trên bảng lịch sử giao dịch 25.000 dòng billing.invoice_history: "
        "Khi chưa có chỉ mục, CSDL phải Seq Scan duyệt qua toàn bộ 25.000 bản ghi, đọc 179 block bộ nhớ và mất gần 2 mili-giây. "
        "Ngay sau khi tạo B-Tree Index trên cột số tiền, CSDL tự động kích hoạt Index Scan, chỉ đọc đúng 2 block bộ nhớ và thời gian "
        "thực thi giảm xuống chỉ còn 0.04 mili-giây, tốc độ nhanh vượt trội hơn 40 lần!"
    )
    
    add_heading_2(doc, "Bước 3.3: Module AI 2 - Tự động Khuyến nghị Chỉ mục (Index Recommendation Engine)")
    add_action_box(doc, "Neon SQL bên trái: Chạy SELECT * FROM ai.fn_recommend_indexes() LIMIT 3.")
    add_speech_card(
        doc,
        "Bây giờ, nhóm chạy hàm khuyến nghị chỉ mục. Thay vì DBA phải ngồi phân tích từng truy vấn chậm thủ công, "
        "Module AI 2 tự động giám sát thống kê runtime pg_stat_user_tables trong CSDL, phát hiện ngay 3 bảng bị quét tuần tự quá nhiều lần "
        "(gồm gaming_session, invoice và maintenance), đồng thời tự động sinh sẵn câu lệnh CREATE INDEX kèm giải thích nguyên nhân rõ ràng."
    )
    
    # PHẦN 4
    add_heading_1(doc, "PHẦN 4: ĐIỂM NHẤN CÔNG NGHỆ VÀ 3 MODULE AI THỜI GIAN THỰC (REAL-TIME AI)")
    
    add_heading_2(doc, "Bước 4.1: Bóc tách dữ liệu bán cấu trúc NoSQL từ cột JSONB")
    add_action_box(doc, "Neon SQL bên trái: Chạy truy vấn lọc CPU i7, GPU RTX, RAM 32GB và kiểm tra toán tử tập con @>.")
    add_speech_card(
        doc,
        "Mỗi dòng máy tính phòng net có cấu hình linh hoạt khác nhau. Cột JSONB specs giúp lưu trữ linh hoạt như NoSQL MongoDB "
        "mà vẫn duy trì được tính toàn vẹn quan hệ ACID của PostgreSQL.\n\n"
        "Nhóm thực hiện lọc đồng thời 3 thông số phần cứng: card đồ họa chứa RTX, chip CPU chứa i7 và RAM 32GB bằng toán tử ->>. "
        "Sau đó nhóm dùng toán tử tập con Containment @> để tìm kiếm siêu tốc, hệ thống trả về chính xác đúng 5 máy trạm VIP từ PC016 đến PC020."
    )
    
    add_heading_2(doc, "Bước 4.2: [ĐIỂM CỘNG XUẤT SẮC] Module AI 1 - Cảnh báo rủi ro thời gian thực (LISTEN/NOTIFY) & SMS")
    add_action_box(doc, "Terminal bên phải: Chạy listen_ai_alerts.py -> Neon SQL bên trái: Nạp tiền 10 triệu -> Xem Terminal nổ cảnh báo & SMS.")
    add_speech_card(
        doc,
        "Kính mời Thầy Cô quan sát hai màn hình song song:\n"
        "Bên phải Terminal đang chạy tiến trình Python lắng nghe tín hiệu từ CSDL qua cơ chế Pub/Sub LISTEN/NOTIFY trên kênh ai_security_alert.\n"
        "Bên trái SQL, nhóm giả lập nạp số tiền lớn bất thường là 10.000.000 VNĐ vào bảng thanh toán.\n\n"
        "Ngay khoảnh khắc lệnh nạp tiền được Commit, Trigger CSDL tự động phát tín hiệu NOTIFY. Lập tức, màn hình Terminal bên phải nổ cảnh báo đỏ "
        "thời gian thực lúc real-time, đồng thời kích hoạt script gửi tin nhắn SMS Brandname thông báo biến động số dư về điện thoại thật của khách hàng "
        "mà không làm quá tải CSDL!"
    )
    
    add_heading_2(doc, "Bước 4.3: Module AI 4 - Thuật toán thống kê Z-Score phát hiện ngoại lai (Outliers)")
    add_action_box(doc, "Neon SQL bên trái: Chạy hàm SELECT * FROM ai.fn_detect_anomalies(1.5).")
    add_speech_card(
        doc,
        "Dựa trên cơ sở toán học là phân phối chuẩn Gauss và quy tắc 3-Sigma, Module AI 4 tự động tính toán trung bình mẫu và độ lệch chuẩn.\n\n"
        "Khi nhóm chạy hàm phát hiện dị biệt với ngưỡng Z bằng 1.5, AI gắn cờ chính xác 2 giao dịch bất thường trong hệ thống: "
        "thứ nhất là một phiên chơi kéo dài tới 2.100 phút (tức 35 tiếng) với hệ số Z bằng 2.65 so với trung bình quán chỉ hơn 6 tiếng; "
        "và thứ hai là giao dịch nạp 10 triệu đồng với hệ số Z bằng 2.13 so với trung bình nạp của quán."
    )
    
    add_heading_2(doc, "Bước 4.4: Module AI 5 - Kích cầu khuyến mãi, phân tầng Khách thường vs Khách VIP và Hoàn tác Rollback")
    add_action_box(doc, "Terminal phải: Chạy sms listener -> Neon SQL trái: Chạy fn_apply_promotions() -> Terminal báo gửi SMS & Verify lõi CSDL -> SQL: Chạy fn_rollback_promotions().")
    add_speech_card(
        doc,
        "Tiếp theo là điểm nhấn của Module AI 5: tự động kích cầu giờ thấp điểm và níu chân khách hàng VIP qua quy trình 5 bước bài bản:\n\n"
        "1. Đầu tiên, tool Python lắng nghe cổng SMS sẵn sàng bên Terminal.\n"
        "2. Trên SQL, nhóm gọi hàm fn_apply_promotions() để kích hoạt khuyến mãi AI.\n"
        "3. Ngay lập tức, Terminal bắt được tín hiệu thời gian thực từ CSDL, gửi tin nhắn SMS thành công tới số điện thoại đích danh 0327469105 "
        "của khách VIP Nguyễn Việt Anh, kèm theo tính toán mức giảm tiền giờ từ 10.000đ xuống còn 7.500đ.\n"
        "4. Trên SQL, nhóm truy vấn bảng giá để thấy hệ thống đã lưu cấu hình giảm 25% tiền giờ.\n"
        "5. Đặc biệt, để chứng minh giá thực sự giảm trong lõi CSDL chứ không phải hiển thị hình thức, nhóm chạy công cụ rà soát tầng lõi nhân CSDL verify_promotion_engine.py trên Terminal:\n"
        "   + Mục 1 rà soát khách VIP thụ hưởng: đích danh khách hàng Nguyễn Việt Anh nhận Voucher VIP độc quyền 30.000 VNĐ.\n"
        "   + Mục 2 rà soát giờ vàng toàn quán: toàn bộ máy trạm được hưởng Happy Hour giảm 25% tiền giờ.\n"
        "   + Mục 3 đối chiếu tính cước thực tế: khách thường chỉ trả 7.500đ một giờ; còn khách VIP Nguyễn Việt Anh được hưởng ưu đãi kép: "
        "vừa chơi giá 7.500đ/h, vừa trừ tiếp Voucher 30.000 VNĐ, ví dụ chơi 4 tiếng (4 x 7.5k = 30k) được hoàn toàn miễn phí 0 đồng!\n\n"
        "6. Cuối cùng, nhóm chứng minh tính an toàn qua cơ chế Hoàn tác Rollback: khi chạy hàm fn_rollback_promotions(), toàn bộ chương trình Happy Hour "
        "và Voucher AI được dọn sạch khỏi Database, giá giờ chơi toàn bộ hệ thống lập tức quay về nguyên bản 10.000 VNĐ/giờ!"
    )
    
    # PHẦN 5
    add_heading_1(doc, "PHẦN 5: TỔNG KẾT VÀ BẢO VỆ THÀNH CÔNG ĐỀ TÀI")
    add_action_box(doc, "Cúi đầu cảm ơn Hội đồng, sẵn sàng đón nhận câu hỏi phản biện từ Thầy Cô.")
    add_speech_card(
        doc,
        "Kính thưa Thầy Cô trong Hội đồng!\n\n"
        "Qua toàn bộ quá trình báo cáo và thực nghiệm vừa rồi, nhóm chúng em đã chứng minh trọn vẹn 5 trụ cột của một hệ thống Quản trị Cơ sở Dữ liệu hiện đại:\n"
        "1. Tính toàn vẹn và an toàn dữ liệu tự động với Pipeline ETL và cơ chế Fail-Safe ở tầng nhân CSDL;\n"
        "2. Kiến trúc an ninh bảo mật phân quyền đa tầng kết hợp hoàn hảo giữa RBAC, CLS và RLS;\n"
        "3. Khả năng tối ưu hiệu năng truy vấn quy mô lớn với B-Tree Index và AI Khuyến nghị chỉ mục tự động;\n"
        "4. Xử lý dữ liệu bán cấu trúc NoSQL JSONB linh hoạt trên nền tảng quan hệ ACID;\n"
        "5. Và đặc biệt là 5 Module AI hoạt động thời gian thực gắn kết chặt chẽ với bài toán kinh doanh thực tiễn của phòng máy.\n\n"
        "Nhóm chúng em xin chân thành cảm ơn quý Thầy Cô đã chú ý lắng nghe và rất mong nhận được những nhận xét, góp ý quý báu từ Hội đồng "
        "để đề tài được hoàn thiện hơn nữa!\n"
        "Em xin trân trọng cảm ơn!"
    )
    
    doc.save(output_path)
    print(f"Successfully generated spoken speech Word document: {output_path}")

if __name__ == "__main__":
    out_file = os.path.join("docs", "KichBan_LoiNoi_ThuyetTrinh_BaoVe.docx")
    build_speech_document(out_file)

