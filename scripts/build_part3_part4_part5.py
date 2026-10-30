import docx
from scripts.docx_styles import (
    add_heading_1, add_heading_2, add_speech_box, 
    add_code_table, add_result_table, add_explanation_box
)

def add_part_3(doc):
    """Xây dựng Phần 3: Tối ưu hóa hiệu năng & Module AI 2."""
    add_heading_1(doc, "PHẦN 3: TỐI ƯU HÓA HIỆU NĂNG VÀ MODULE AI 2 (PERFORMANCE DEMO)")
    
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Tìm hồ sơ trong kho lưu trữ và Bác sĩ khám bệnh CSDL)",
        "Khi tìm một tờ hóa đơn trong kho lưu trữ: nếu không có mục lục sắp xếp, ta phải lật từng tờ giấy từ đầu "
        "đến cuối (Seq Scan - tốn sức và chậm chạp khi hồ sơ lên đến hàng triệu tờ). Nhưng nếu có mục lục chỉ mục "
        "(B-Tree Index), ta nhảy thẳng tới đúng ngăn tủ chứa hồ sơ (Index Scan - chỉ tốn 1 tích tắc). "
        "Module AI 2 giống như một bác sĩ khám bệnh CSDL: tự động rà soát nhật ký truy vấn để phát hiện các bảng "
        "đang bị 'nghẽn cổ chai' và kê đơn tạo chỉ mục phù hợp."
    )
    
    # Bước 3.1
    add_heading_2(doc, "Bước 3.1: Đo tự động hiệu năng thực tế trên 20.000 dòng (Khuyên dùng)")
    add_speech_box(
        doc,
        "Thưa Thầy Cô, để chứng minh sức mạnh của Index trên quy mô lớn, nhóm xây dựng hàm Benchmark tự động "
        "ai.fn_compare_index_benchmark() kiểm thử trên 20.000 bản ghi: Seq Scan phải duyệt qua 128 block bộ nhớ "
        "mất gần 1ms, trong khi B-Tree Index Scan chỉ đọc 2-3 block mất 0.04-0.17ms, nhanh gấp 5 đến 20 lần."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT \n"
        "    phuong_thuc AS phuong_thuc_quet,\n"
        "    so_dong_du_lieu AS quy_mo_du_lieu,\n"
        "    thoi_gian_ms AS thoi_gian_thuc_thi_ms,\n"
        "    so_block_bo_nho_doc AS bo_nho_da_doc,\n"
        "    danh_gia_hieu_nang AS ket_luan_hieu_nang\n"
        "FROM ai.fn_compare_index_benchmark();"
    )
    add_result_table(
        doc,
        ["phuong_thuc_quet", "quy_mo_du_lieu", "thoi_gian_thuc_thi_ms", "bo_nho_da_doc", "ket_luan_hieu_nang"],
        [
            ["Seq Scan (Quét tuần tự)", "20.000 dòng", "0.90 ms", "128 blocks", "Chậm - Phải duyệt toàn bộ bảng trong bộ nhớ"],
            ["Index Scan (B-Tree Index)", "20.000 dòng", "0.17 ms", "2-3 blocks", "Nhanh gấp 5-20 lần - Nhảy trực tiếp tới dữ liệu"]
        ]
    )
    
    # Bước 3.2
    add_heading_2(doc, "Bước 3.2: Thao tác thủ công trên bảng hóa đơn gốc (Đối chiếu EXPLAIN ANALYZE)")
    add_speech_box(
        doc,
        "Tiếp theo, nhóm thực hiện thao tác thủ công trên bảng hóa đơn billing.invoice: đo kế hoạch thực thi "
        "khi chưa có Index (Seq Scan), sau đó tạo chỉ mục B-Tree và ép dùng Index để đối chiếu kết quả."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "RESET ROLE;\n\n"
        "-- 1. Xóa index cũ để đưa bảng về trạng thái nguyên bản:\n"
        "DROP INDEX IF EXISTS billing.idx_invoice_total_amount;\n\n"
        "-- 2. Chạy EXPLAIN tìm hóa đơn 630.000 VNĐ -> Kết quả: Seq Scan:\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice \n"
        "WHERE total_amount = 630000;\n\n"
        "-- 3. Tạo chỉ mục B-Tree trên cột số tiền hóa đơn:\n"
        "CREATE INDEX idx_invoice_total_amount ON billing.invoice (total_amount);\n\n"
        "-- 4. Bật ép dùng Index và chạy lại câu truy vấn -> Kết quả: Index Scan:\n"
        "SET enable_seqscan = off;\n\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice \n"
        "WHERE total_amount = 630000;\n\n"
        "SET enable_seqscan = on;"
    )
    add_explanation_box(
        doc,
        "Lưu ý quan trọng khi giải thích với Thầy Cô",
        "Trên bảng hóa đơn gốc của bài thi hiện có 9 dòng dữ liệu mẫu, Seq Scan và Index Scan chạy nhanh gần như nhau (~0.04 ms) "
        "bởi vì 9 dòng chỉ chiếm đúng 1 block bộ nhớ 8KB trong RAM. Khi số lượng hóa đơn tăng lên hàng chục ngàn dòng "
        "(như đã chứng minh ở Bước 3.1 bên trên), B-Tree Index thể hiện tốc độ vượt trội."
    )
    
    # Bước 3.3
    add_heading_2(doc, "Bước 3.3: Module AI 2 - Tự động Khuyến nghị Chỉ mục (Index Recommendation Engine)")
    add_speech_box(
        doc,
        "Thay vì DBA phải ngồi rà soát thủ công, Module AI 2 tự động quét Catalog runtime của PostgreSQL, "
        "phát hiện các bảng có khối lượng giao dịch lớn đang thiếu Index ở khóa ngoại (Foreign Key) "
        "và tự động sinh lệnh SQL tạo Index tối ưu, kèm mở ngoặc giải thích lý do kỹ thuật rõ ràng."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT \n"
        "    recommendation_id AS stt, \n"
        "    table_name AS bang_can_toi_uu, \n"
        "    column_list AS cot_can_danh_index, \n"
        "    reason AS ly_do_ai_khuyen_nghi, \n"
        "    suggested_sql AS lenh_tao_index_tu_dong \n"
        "FROM ai.fn_recommend_indexes() \n"
        "LIMIT 3;"
    )
    add_result_table(
        doc,
        ["stt", "bang_can_toi_uu", "cot_can_danh_index", "ly_do_ai_khuyen_nghi"],
        [
            ["1", "billing.invoice", "customer_id", "Khóa ngoại thường xuyên JOIN (Giải thích: Giảm 90% chi phí Seq Scan khi tra cứu hóa đơn khách)"],
            ["2", "billing.payment", "invoice_id", "Khóa ngoại giao dịch tài chính (Giải thích: Tăng tốc đối soát thanh toán tức thì)"],
            ["3", "core.gaming_session", "computer_id", "Khóa ngoại phiên máy (Giải thích: Tối ưu tra cứu trạng thái máy trạm thời gian thực)"]
        ]
    )

def add_part_4(doc):
    """Xây dựng Phần 4: Điểm nhấn tích hợp & 3 Module AI thời gian thực."""
    add_heading_1(doc, "PHẦN 4: ĐIỂM NHẤN TÍCH HỢP & 3 MODULE AI THỜI GIAN THỰC (AI HIGHLIGHTS)")
    
    # Bước 4.1
    add_heading_2(doc, "Bước 4.1: Bóc tách dữ liệu bán cấu trúc NoSQL từ cột JSONB")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Cấu hình máy tính linh hoạt)",
        "Mỗi đời máy tính có phần cứng khác nhau: máy thường thì CPU i5 RAM 16GB, máy thi đấu thì màn 240Hz, "
        "tản nhiệt nước, card RTX 4070. Cột JSONB specs giúp lưu trữ linh hoạt như MongoDB mà vẫn duy trì được "
        "tính toàn vẹn quan hệ ACID của PostgreSQL."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
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
    
    # Bước 4.2
    add_heading_2(doc, "Bước 4.2: [ĐIỂM CỘNG XUẤT SẮC] Module AI 1 - Real-time Cảnh báo rủi ro (LISTEN/NOTIFY) & SMS")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Còi báo động két sắt ngân hàng và SMS Biến động số dư)",
        "Thay vì ứng dụng phải liên tục gửi lệnh 'hỏi thăm' CSDL mỗi giây (Polling gây quá tải CSDL), "
        "PostgreSQL sử dụng cơ chế Pub/Sub (LISTEN/NOTIFY): khi có giao dịch lớn bất thường xảy ra, "
        "Database tự động 'bắn' tín hiệu đánh thức tiến trình Python cảnh báo ngay tức khắc, "
        "đồng thời gửi tin nhắn SMS Brandname về điện thoại khách hàng."
    )
    add_speech_box(
        doc,
        "Kính mời Thầy Cô chú ý màn hình: Nhóm sẽ chia đôi màn hình: Bên phải là Terminal chạy tiến trình Python "
        "lắng nghe sự kiện LISTEN kênh 'ai_security_alert'. Bên trái là SQL Editor: Nhóm thực hiện lệnh nạp số tiền "
        "khủng 10.000.000 VNĐ. Ngay khi lệnh vừa Commit, Trigger CSDL lập tức phát tín hiệu NOTIFY, màn hình Terminal "
        "nổ cảnh báo đỏ tức thì và gửi tin nhắn SMS biến động số dư!"
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ PHẢI (TERMINAL POWERSHELL 1) - KÍCH HOẠT LẮNG NGHE:",
        "python python/listen_ai_alerts.py\n"
        "# (Màn hình hiện: [+] ĐÃ KÍCH HOẠT LẮNG NGHE KÊNH: 'ai_security_alert' và đứng chờ sự kiện...)"
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ TRÁI (NEON SQL EDITOR) - NẠP TIỀN BẤT THƯỜNG 10 TRIỆU ĐỒNG:",
        "INSERT INTO billing.payment (kind, customer_id, method, amount, employee_id, reference)\n"
        "VALUES ('topup', 1, 'bank_transfer', 10000000, 1, 'TOPUP-VIP-10M');"
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ PHẢI (TERMINAL POWERSHELL 2) - BẮN SMS BRANDNAME VỀ ĐIỆN THOẠI:",
        "python python/send_brandname_sms.py --phone \"0327469105\" --name \"Nguyen Viet Anh\" --amount 10000000 --balance 10050000"
    )
    
    # Bước 4.3
    add_heading_2(doc, "Bước 4.3: Module AI 4 - Thuật toán thống kê Z-Score phát hiện ngoại lai (Outliers)")
    add_explanation_box(
        doc,
        "Cơ sở toán học: Phân phối chuẩn Gauss và Quy tắc 3-Sigma",
        "1. Trung bình mẫu: μ = (1/N) * Σ xi\n"
        "2. Độ lệch chuẩn: σ = sqrt[(1/N) * Σ (xi - μ)²]\n"
        "3. Chỉ số chuẩn hóa: Z = |x - μ| / σ\n"
        "Theo phân phối chuẩn Gauss, khoảng [-2σ, +2σ] bao phủ 95.45% các hành vi giao dịch thông thường. "
        "Bất kỳ giao dịch nào có |Z| >= 1.5 - 2.0 đều là các điểm dữ liệu cực kỳ hiếm gặp (chiếm dưới 4.55% tổng thể), "
        "được hệ thống gắn cờ ngoại lai (Anomaly Outlier)."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
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
    
    # Bước 4.4
    add_heading_2(doc, "Bước 4.4: Module AI 5 - Gợi ý Khuyến mãi Thông minh & Tự động gửi SMS Voucher")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Trợ lý Marketing kích cầu giờ ế và Níu chân khách VIP)",
        "Giống như một chuyên viên tiếp thị giàu kinh nghiệm: AI liên tục phân tích Materialized View doanh thu "
        "để phát hiện các khung giờ vắng khách (ví dụ sáng thứ 2 - thứ 5) để đề xuất Happy Hour giảm 30%, "
        "hoặc phát hiện các khách hàng VIP lâu ngày chưa ghé quán để tự động kích hoạt gửi SMS tặng voucher níu chân."
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "SELECT \n"
        "    sug_id, \n"
        "    suggestion_type, \n"
        "    target, \n"
        "    details->>'recommended_action' AS hanh_dong_goi_y,\n"
        "    details->>'rationale'          AS ly_do_ai\n"
        "FROM ai.promotion_suggestion;"
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: TERMINAL POWERSHELL (GỬI VOUCHER KÍCH CẦU):",
        "python python/send_brandname_sms.py --promo --phone \"0327469105\" --name \"Nguyen Viet Anh\""
    )

def add_part_5(doc):
    """Xây dựng Phần 5: Tổng kết và bảo vệ đề tài."""
    add_heading_1(doc, "PHẦN 5: TỔNG KẾT VÀ BẢO VỆ THÀNH CÔNG ĐỀ TÀI")
    
    add_speech_box(
        doc,
        "Kính thưa Thầy Cô và Hội đồng, qua 4 phần trình bày vừa rồi, nhóm đã chứng minh trọn vẹn 5 trụ cột "
        "quản trị CSDL hiện đại trên nền tảng Cloud Neon PostgreSQL: 1. Tự động hóa ETL Fail-Safe; "
        "2. An ninh bảo mật đa tầng kết hợp RBAC, CLS và RLS; 3. Tối ưu hiệu năng truy vấn quy mô lớn; "
        "4. Xử lý dữ liệu bán cấu trúc NoSQL JSONB linh hoạt; và 5. Tích hợp 5 Module AI hoạt động thời gian thực. "
        "Nhóm xin chân thành cảm ơn Thầy Cô đã lắng nghe và rất mong nhận được những góp ý quý báu từ Hội đồng!"
    )
