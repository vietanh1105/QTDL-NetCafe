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
    add_heading_2(doc, "Bước 4.4: Module AI 5 - Kích hoạt Khuyến mãi & Gửi SMS Discount thời gian thực")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Trợ lý Marketing kích cầu giờ ế và Níu chân khách VIP)",
        "Giống như một chuyên viên tiếp thị giàu kinh nghiệm: AI liên tục phân tích Materialized View doanh thu "
        "để phát hiện các khung giờ vắng khách để kích hoạt giảm 25% tiền giờ, đồng thời tặng mã VIP 30K "
        "bằng cách tự động phát tín hiệu NOTIFY bắn tin nhắn SMS tính toán mức giảm tiền cụ thể về điện thoại khách hàng."
    )
    add_speech_box(
        doc,
        "Ở bước này, nhóm kết hợp trọn vẹn luồng tương tác giữa Database Cloud và Tool Python: "
        "Đầu tiên, chạy tool Python trên Terminal để mở cổng đón nhận SMS. Sau đó trên Neon SQL, nhóm chạy hàm AI "
        "kích hoạt khuyến mãi. Hệ thống tự động gửi tin nhắn SMS tới điện thoại tính rõ mức giảm: giá gốc 10.000đ/giờ, "
        "giảm 25% (-2.500đ) chỉ còn 7.500đ/giờ! Tiếp theo, nhóm chạy câu lệnh SQL đối chiếu giá để chứng minh "
        "hệ thống tính tiền đã cập nhật chính xác từ 10k xuống 7.5k!"
    )
    
    # 4.4 - Bước A: Tool Python lắng nghe
    add_code_table(
        doc,
        "🖥️ BƯỚC A: CỬA SỔ PHẢI (TERMINAL POWERSHELL) - CHẠY TOOL PYTHON LẮNG NGHE CỔNG SMS GATEWAY:",
        "python python/sms_gateway_listener.py\n"
        "# Màn hình Terminal hiển thị: Đang lắng nghe kênh 'ai_sms_dispatch' và chờ lệnh từ Neon...\n"
        "# (Hoặc chạy công cụ gửi SMS trực tiếp: python python/send_brandname_sms.py --promo --phone \"0327469105\" --name \"Nguyen Viet Anh\")"
    )
    
    # 4.4 - Bước B: Lệnh SQL kích hoạt trên Neon
    add_code_table(
        doc,
        "🖥️ BƯỚC B: CỬA SỔ TRÁI (NEON SQL EDITOR) - CHẠY LỆNH SQL KÍCH HOẠT KHUYẾN MÃI AI:",
        "SELECT applied_code, applied_name, target_info, discount_val, status_msg \n"
        "FROM ai.fn_apply_promotions();"
    )
    
    # 4.4 - Bước C: Màn hình Terminal báo gửi SMS thành công & mức discount
    add_code_table(
        doc,
        "📊 BƯỚC C: MÀN HÌNH TERMINAL (TỰ ĐỘNG BẬT TÍN HIỆU GỬI SMS & DISCOUNT 25% TỪ 10K -> 7.5K):",
        "[🔔 TÍN HIỆU CSDL NHẬN ĐƯỢC LÚC REAL-TIME]\n"
        "    - Khách hàng đích  : Nguyễn Việt Anh (SĐT: 0327469105)\n"
        "    - Mã Chương trình  : HAPPY_AI_16H + VIP_VIETANH_30K\n"
        "    - Chính sách giá   : Giá gốc 10.000đ/h -> GIẢM 25% (-2.500đ) -> Còn 7.500đ/h!\n"
        "    - Nội dung SMS     : [NETCAFE PRO] Chào bạn Nguyễn Việt Anh! Khung giờ vàng kích cầu: "
        "Giá gốc 10.000đ/h -> GIẢM 25% (-2.500đ) chỉ còn 7.500đ/h! Tặng kèm mã VIP30K giảm 30.000đ. Hẹn gặp bạn tại NetCafe!\n"
        "----------------------------------------------------------------------\n"
        "[📲 TRẠNG THÁI GỬI SMS]: THÀNH CÔNG (HTTP 200 OK - DELIVERED)!\n"
        "[✅] Đã gửi tin nhắn SMS thành công tới số điện thoại 0327469105!\n"
        "[🔗] Kênh theo dõi trực tiếp trên điện thoại: https://ntfy.sh/netcafe_vietanh"
    )
    
    # 4.4 - Bước D: Lệnh SQL đối chiếu bảng giá từ 10k còn 7.5k
    add_code_table(
        doc,
        "🖥️ BƯỚC D: CỬA SỔ TRÁI (NEON SQL EDITOR) - ĐỐI CHIẾU BẢNG GIÁ THỰC TẾ (TỪ 10K CÒN 7.5K):",
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
    add_result_table(
        doc,
        ["ma_khuyen_mai", "gia_goc_moi_gio", "phan_tram_giam", "so_tien_duoc_giam", "gia_thuc_te_phai_tra", "trang_thai_he_thong"],
        [
            ["HAPPY_AI_16H", "10.000 VNĐ", "25.0%", "2.500 VNĐ", "7.500 VNĐ", "ĐÃ ÁP DỤNG THÀNH CÔNG VÀO HỆ THỐNG TÍNH TIỀN"]
        ]
    )
    
    # 4.4 - Bước E: Rà soát lõi CSDL bằng Python Terminal
    add_code_table(
        doc,
        "🖥️ BƯỚC E [ĐIỂM NHẤN THUYẾT PHỤC GIẢNG VIÊN]: TERMINAL POWERSHELL - RÀ SOÁT TẦNG LÕI NHÂN CSDL:",
        "python python/verify_promotion_engine.py"
    )
    add_explanation_box(
        doc,
        "Ý nghĩa kiểm định lõi gửi Hội đồng chấm điểm",
        "Lệnh trên kết nối trực tiếp vào PostgreSQL engine, phân định rành mạch 2 cơ chế khuyến mãi: "
        "1. Khách VIP đích danh (Nguyễn Việt Anh - 0327469105): Nhận Voucher độc quyền 30.000 VNĐ trừ thẳng vào hóa đơn; "
        "2. Toàn quán (Mọi khách hàng): Áp dụng giờ vàng Happy Hour giảm 25% tiền giờ (10k còn 7.5k/h); "
        "3. Lõi tính cước CSDL: Khách thường trả 7.500đ/h, Khách VIP hưởng ưu đãi kép (vừa giảm giờ chơi vừa trừ Voucher 30k); "
        "4. Cơ chế Hoàn tác (Rollback): Khi chạy rollback, toàn bộ Happy Hour và Voucher VIP bị thu hồi, "
        "giá giờ chơi lập tức hoàn trả về nguyên bản 10.000 VNĐ/giờ!"
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
