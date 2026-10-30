import docx
from docx.oxml import parse_xml, OxmlElement
from docx.oxml.ns import nsdecls, qn
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT

NAVY_BLUE = RGBColor(27, 54, 93)      # #1B365D
DEEP_BLUE = RGBColor(15, 76, 129)     # #0F4C81
GRAY_TEXT = RGBColor(80, 80, 80)
CODE_TEXT = RGBColor(33, 33, 33)

def set_cell_background(cell, hex_color):
    """Thiết lập màu nền cho ô trong bảng."""
    shading_xml = f'<w:shd {nsdecls("w")} w:fill="{hex_color}"/>'
    cell._tc.get_or_add_tcPr().append(parse_xml(shading_xml))

def set_cell_margins(cell, top=140, bottom=140, start=180, end=180):
    """Thiết lập lề đệm bên trong ô (đơn vị dxa: 20 dxa = 1 pt)."""
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
    """Thêm tiêu đề Phần lớn (H1) có gạch dưới và màu xanh Navy."""
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = 'Calibri'
    run.font.size = Pt(15)
    run.font.bold = True
    run.font.color.rgb = NAVY_BLUE
    return p

def add_heading_2(doc, text):
    """Thêm tiêu đề Bước nhỏ (H2)."""
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(10)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    run = p.add_run(text)
    run.font.name = 'Calibri'
    run.font.size = Pt(12.5)
    run.font.bold = True
    run.font.color.rgb = DEEP_BLUE
    return p

def add_speech_box(doc, speech_text):
    """Hộp lời thuyết trình của sinh viên (Nền xanh nhạt, viền xanh)."""
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.rows[0].cells[0]
    cell.width = Inches(6.5)
    set_cell_background(cell, 'EBF3FB')
    set_cell_margins(cell, top=140, bottom=140, start=180, end=180)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing = 1.15
    run = p.add_run(f'🗣️ Lời thuyết trình: "{speech_text}"')
    run.font.name = 'Calibri'
    run.font.size = Pt(10.5)
    run.font.italic = True
    run.font.color.rgb = RGBColor(13, 71, 161)
    
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(3)

def add_code_table(doc, window_title, code_content):
    """Khối lệnh thực thi (Header hiển thị cửa sổ, body hiển thị mã lệnh)."""
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
        
    set_cell_margins(header_cell, top=80, bottom=80, start=140, end=140)
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
    set_cell_margins(body_cell, top=140, bottom=140, start=180, end=180)
    
    bp = body_cell.paragraphs[0]
    bp.paragraph_format.space_before = Pt(0)
    bp.paragraph_format.space_after = Pt(0)
    bp.paragraph_format.line_spacing = 1.15
    brun = bp.add_run(code_content)
    brun.font.name = 'Consolas'
    brun.font.size = Pt(9.5)
    brun.font.color.rgb = CODE_TEXT
    
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(4)

def add_result_table(doc, headers, rows):
    """Bảng hiển thị kết quả mong đợi."""
    table = doc.add_table(rows=len(rows) + 1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = True
    
    for c_idx, h_text in enumerate(headers):
        cell = table.rows[0].cells[c_idx]
        set_cell_background(cell, 'E2E8F0')
        set_cell_margins(cell, top=100, bottom=100, start=120, end=120)
        p = cell.paragraphs[0]
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(0)
        run = p.add_run(h_text)
        run.font.name = 'Consolas'
        run.font.size = Pt(9)
        run.font.bold = True
        run.font.color.rgb = NAVY_BLUE
        
    for r_idx, r_data in enumerate(rows):
        for c_idx, val in enumerate(r_data):
            cell = table.rows[r_idx + 1].cells[c_idx]
            bg = 'FFFFFF' if r_idx % 2 == 0 else 'F8FAFC'
            set_cell_background(cell, bg)
            set_cell_margins(cell, top=80, bottom=80, start=120, end=120)
            p = cell.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(0)
            run = p.add_run(str(val))
            run.font.name = 'Consolas'
            run.font.size = Pt(8.5)
            run.font.color.rgb = CODE_TEXT
            
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(4)

def add_explanation_box(doc, title, text):
    """Hộp ví dụ thực tế / Diễn giải kỹ thuật chuyên sâu (Nền vàng ấm)."""
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.rows[0].cells[0]
    cell.width = Inches(6.5)
    set_cell_background(cell, 'FFFBEB')
    set_cell_margins(cell, top=120, bottom=120, start=180, end=180)
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing = 1.15
    run1 = p.add_run(f'💡 {title}: ')
    run1.font.name = 'Calibri'
    run1.font.size = Pt(10)
    run1.font.bold = True
    run1.font.color.rgb = RGBColor(180, 83, 9)
    
    run2 = p.add_run(text)
    run2.font.name = 'Calibri'
    run2.font.size = Pt(10)
    run2.font.color.rgb = RGBColor(120, 53, 15)
    
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(3)
