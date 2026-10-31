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
        "Thưa Thầy Cô, để chứng minh sức mạnh vượt trội của Index trên quy mô lớn, trên cửa sổ Neon SQL nhóm chạy hàm "
        "Benchmark tự động ai.fn_compare_index_benchmark() kiểm thử trên 20.000 bản ghi. Bảng kết quả in ra tiếng Việt rõ ràng: "
        "Seq Scan phải đọc qua 128 block bộ nhớ mất 0.92ms, trong khi B-Tree Index Scan chỉ đọc 2-3 block mất 0.17ms, "
        "nhanh hơn gấp 5.3 lần."
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
        ["phuong_thuc_quet", "quy_mo_du_lieu", "thoi_gian_thuc_thi_ms", "bo_nho_da_doc", "ket_luan_hieu_nang (Thực tế trên Neon)"],
        [
            ["1. Quét tuần tự (Seq Scan - Không Index)", "20,000 dòng", "0.921 ms", "~128 blocks bộ nhớ", "Chậm (Phải quét duyệt qua từng dòng một từ đầu đến cuối bảng)"],
            ["2. Quét chỉ mục (B-Tree Index Scan)", "20,000 dòng", "0.173 ms", "2-3 blocks bộ nhớ", "Nhanh vượt trội (Nhảy trực tiếp đến vị trí dòng qua cây B-Tree, tối ưu gấp 5.3 lần)"]
        ]
    )
    
    # Bước 3.2
    add_heading_2(doc, "Bước 3.2: Thao tác thủ công trên bảng lịch sử giao dịch (Đối chiếu EXPLAIN ANALYZE)")
    add_speech_box(
        doc,
        "Sau đó, nhóm thực hiện thao tác đối chiếu thực tế trên bảng lịch sử hóa đơn 25.000 dòng billing.invoice_history: "
        "Đầu tiên ở câu lệnh 1 và 2, nhóm xóa index và chạy EXPLAIN để xem Seq Scan: CSDL phải duyệt qua toàn bộ 25.000 bản ghi, "
        "đọc 179 block bộ nhớ và mất gần 2 mili-giây (1.94 ms). "
        "Tiếp theo ở câu lệnh 3 và 4, nhóm tạo B-Tree Index trên cột total_amount và chạy lại câu truy vấn: "
        "PostgreSQL tự động kích hoạt Index Scan, chỉ đọc đúng 2 block bộ nhớ và thời gian thực thi giảm xuống chỉ còn 0.04 mili-giây, "
        "tốc độ nhanh vượt trội gấp hơn 40 lần!"
    )
    add_code_table(
        doc,
        "🖥️ CỬA SỔ THAO TÁC: NEON SQL EDITOR / DBEAVER",
        "RESET ROLE;\n\n"
        "-- 1. Xóa index cũ để đưa bảng về trạng thái nguyên bản:\n"
        "DROP INDEX IF EXISTS billing.idx_invoice_history_amount;\n\n"
        "-- 2. Chạy EXPLAIN tìm hóa đơn 630.000 VNĐ khi CHƯA CÓ INDEX -> Kết quả: Seq Scan (Chậm ~2ms):\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice_history \n"
        "WHERE total_amount = 630000;\n\n"
        "-- 3. Tạo chỉ mục B-Tree trên cột số tiền hóa đơn:\n"
        "CREATE INDEX idx_invoice_history_amount ON billing.invoice_history (total_amount);\n\n"
        "-- 4. Chạy lại EXPLAIN sau khi có INDEX -> Kết quả: Index Scan (Nhanh siêu tốc ~0.04ms, tối ưu 42 lần):\n"
        "EXPLAIN (ANALYZE, BUFFERS)\n"
        "SELECT invoice_id, customer_id, cashier_id, total_amount, status \n"
        "FROM billing.invoice_history \n"
        "WHERE total_amount = 630000;"
    )
    add_result_table(
        doc,
        ["Trạng thái kiểm thử", "Kế hoạch thực thi EXPLAIN (ANALYZE, BUFFERS) thực tế trên Neon"],
        [
            ["Khi CHƯA CÓ Index (Seq Scan)", "Seq Scan on invoice_history (cost=0.00..384.85 rows=82 width=60) (actual time=1.920..1.925 rows=1) | Buffers: shared hit=179 | Time: ~1.94 ms (CHẬM)"],
            ["Sau khi TẠO Index B-Tree (Index Scan)", "Bitmap Index Scan on idx_invoice_history_amount (cost=0.00..5.22) (actual time=0.024..0.030 rows=1) | Buffers: shared hit=1 read=2 | Time: ~0.04 ms (NHANH GẤP 42 LẦN!)"]
        ]
    )
    add_explanation_box(
        doc,
        "Chứng minh hiệu năng vượt trội trước Hội đồng",
        "Trên bảng lịch sử 25.000 giao dịch (billing.invoice_history): Quét tuần tự Seq Scan phải đọc duyệt qua 179 block bộ nhớ mất gần 2ms. "
        "Ngay khi có B-Tree Index, CSDL tự động chuyển sang Index Scan chỉ đọc 2 block bộ nhớ và mất 0.04ms. "
        "CSDL tự động tối ưu nhanh gấp 42 lần mà không cần can thiệp ép buộc tham số hệ thống."
    )
    
    # Bước 3.3
    add_heading_2(doc, "Bước 3.3: Module AI 2 - Tự động Khuyến nghị Chỉ mục (Index Recommendation Engine)")
    add_speech_box(
        doc,
        "Bây giờ trên Neon SQL, nhóm chạy hàm ai.fn_recommend_indexes(). Thay vì DBA phải ngồi phân tích từng truy vấn chậm thủ công, "
        "AI tự động quét pg_stat_user_tables và pg_stat_user_indexes trong runtime để phát hiện các bảng bị quét tuần tự "
        "quá nhiều lần và sinh sẵn câu lệnh CREATE INDEX, kèm theo giải thích lý do kỹ thuật rõ ràng trong ngoặc."
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
        ["stt", "bang_can_toi_uu", "cot_can_danh_index", "ly_do_ai_khuyen_nghi (Thực tế trên Neon)", "lenh_tao_index_tu_dong"],
        [
            ["1", "core.gaming_session", "status", "Bảng core.gaming_session bị quét tuần tự 24 lần nhưng chỉ có 18 lượt quét chỉ mục -> (Giải thích: CSDL đang phải đọc duyệt từng dòng để tìm kiếm. Cần tạo B-Tree Index trên cột [status] để nhảy thẳng đến dữ liệu cần tìm, tránh quét toàn bộ bảng).", "CREATE INDEX idx_gaming_session_auto_opt ON core.gaming_session (status);"],
            ["2", "billing.invoice", "status", "Bảng billing.invoice bị quét tuần tự 24 lần nhưng chỉ có 10 lượt quét chỉ mục -> (Giải thích: CSDL đang phải đọc duyệt từng dòng để tìm kiếm. Cần tạo B-Tree Index trên cột [status] để nhảy thẳng đến dữ liệu cần tìm, tránh quét toàn bộ bảng).", "CREATE INDEX idx_invoice_auto_opt ON billing.invoice (status);"],
            ["3", "hr.maintenance", "started_at", "Bảng hr.maintenance bị quét tuần tự 6 lần nhưng chỉ có 0 lượt quét chỉ mục -> (Giải thích: Cần tạo B-Tree Index trên cột [started_at] để nhảy thẳng đến dữ liệu cần tìm, tránh quét toàn bộ bảng).", "CREATE INDEX idx_maintenance_auto_opt ON hr.maintenance (started_at);"]
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
        "Mỗi dòng máy tính phòng net có cấu hình khác nhau: máy VIP cần chip i7, card RTX 4070, RAM 32GB; "
        "máy tiêu chuẩn dùng i5, RAM 16GB. Cột JSONB specs giúp lưu trữ linh hoạt như NoSQL MongoDB mà vẫn duy trì được "
        "tính toàn vẹn quan hệ ACID của PostgreSQL."
    )
    add_speech_box(
        doc,
        "Trên Neon SQL, nhóm thực hiện bóc tách và lọc đồng thời 3 thông số phần cứng trong cột specs: GPU chứa 'RTX', CPU chứa 'i7' "
        "và RAM là '32GB'. Sau đó ở câu truy vấn thứ hai, nhóm sử dụng toán tử tập con JSONB Containment (@>) để tìm kiếm siêu tốc. "
        "Hệ thống trả về chính xác 5 máy trạm VIP từ PC016 đến PC020."
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
    add_result_table(
        doc,
        ["ma_may", "ten_may", "chip_cpu", "card_man_hinh", "dung_luong_ram", "Phân khúc máy"],
        [
            ["PC016", "Máy VIP 16", "i7-13700F", "RTX 4070", "32GB", "Máy trạm thi đấu cao cấp"],
            ["PC017", "Máy VIP 17", "i7-13700F", "RTX 4070", "32GB", "Máy trạm thi đấu cao cấp"],
            ["PC018", "Máy VIP 18", "i7-13700F", "RTX 4070", "32GB", "Máy trạm thi đấu cao cấp"],
            ["PC019", "Máy VIP 19", "i7-13700F", "RTX 4070", "32GB", "Máy trạm thi đấu cao cấp"],
            ["PC020", "Máy VIP 20", "i7-13700F", "RTX 4070", "32GB", "Máy trạm thi đấu cao cấp"]
        ]
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
        "Kính mời Thầy Cô quan sát 2 màn hình song song: "
        "Cửa sổ bên phải Terminal chạy tiến trình Python lắng nghe kênh 'ai_security_alert'. "
        "Cửa sổ bên trái Neon SQL: Nhóm chèn giao dịch nạp số tiền lớn 10.000.000 VNĐ vào bảng billing.payment. "
        "Ngay khi lệnh Commit, Trigger CSDL lập tức phát NOTIFY, màn hình Terminal bên phải nổ cảnh báo đỏ tức thì "
        "và gửi tin nhắn SMS Brandname thông báo biến động số dư về điện thoại của khách hàng!"
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
    add_result_table(
        doc,
        ["Kênh tiếp nhận", "Nội dung cảnh báo nổ ra thời gian thực trên màn hình Terminal"],
        [
            ["Terminal PowerShell (listen_ai_alerts.py)", "[🚨 CẢNH BÁO AN NINH THỜI GIAN THỰC LÚC REAL-TIME]\n- Loại cảnh báo: TRANSACTION_ANOMALY (Mức độ: CRITICAL)\n- Số tiền nạp: 10,000,000 VNĐ | Khách hàng: Nguyen Viet Anh (ID: 1)\n- Trạng thái: Đã tự động kích hoạt bảo vệ an toàn CSDL!"],
            ["Điện thoại cá nhân (SMS Brandname)", "[NETCAFE PRO] Khách hàng Nguyen Viet Anh vừa nạp thành công: +10,000,000 VND. Số dư tài khoản hiện tại: 10,050,000 VND. Chúc bạn chơi game vui vẻ!"]
        ]
    )
    
    # Bước 4.3
    add_heading_2(doc, "Bước 4.3: Module AI 4 - Thuật toán thống kê Z-Score phát hiện ngoại lai (Outliers)")
    add_explanation_box(
        doc,
        "Cơ sở toán học: Phân phối chuẩn Gauss và Quy tắc 3-Sigma",
        "1. Giá trị trung bình: μ = (1/N) * Σ xi\n"
        "2. Độ lệch chuẩn: σ = sqrt[(1/N) * Σ (xi - μ)²]\n"
        "3. Chỉ số Z-Score: Z = |x - μ| / σ\n"
        "Theo phân phối chuẩn Gauss, khoảng [-2σ, +2σ] bao phủ 95.45% các hành vi bình thường. "
        "Bất kỳ giao dịch nào có |Z| >= 1.5 - 2.0 đều là ngoại lai cực kỳ hiếm gặp (dưới 4.55% tổng thể)."
    )
    add_speech_box(
        doc,
        "Bây giờ trên Neon SQL, nhóm chạy hàm ai.fn_detect_anomalies(1.5). Thuật toán thống kê Z-Score tự động tính toán "
        "trung bình mẫu, độ lệch chuẩn và phát hiện chính xác 2 giao dịch bất thường trong hệ thống: "
        "một phiên chơi dài tới 2.100 phút (~35 tiếng, Z=2.65 so với trung bình quán 373 phút) và "
        "giao dịch nạp 10 triệu đồng (Z=2.13 so với trung bình quán 1.8 triệu đồng)."
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
    add_result_table(
        doc,
        ["ma_canh_bao", "loai_bat_thuong", "gia_tri_thuc_te", "he_so_z_score", "trung_binh", "do_lech_chuan", "mo_ta_de_hieu_cho_hoi_dong (Thực tế trên Neon)"],
        [
            ["1", "SESSION_DURATION_OUTLIER", "2.100 phút", "2.65", "373.33 phút", "650.92 phút", "Phiên chơi quá dài (Khách chơi tới 2.100 phút ~ 35 tiếng, trung bình quán chỉ 373 phút -> Cần kiểm tra khách ngủ quên hoặc máy bị treo)"],
            ["2", "PAYMENT_AMOUNT_OUTLIER", "10.000.000 VNĐ", "2.13", "1.826.000 VNĐ", "3.830.515 VNĐ", "Nạp tiền quá lớn (Khách nạp tới 10 triệu VNĐ, trung bình quán chỉ 1.8 triệu VNĐ -> Cần kiểm tra giao dịch bất thường hoặc rửa tiền)"]
        ]
    )
    
    # Bước 4.4
    add_heading_2(doc, "Bước 4.4: Module AI 5 - Kích hoạt Khuyến mãi & Gửi SMS Discount thời gian thực")
    add_explanation_box(
        doc,
        "Ví dụ đời thường (Trợ lý Marketing kích cầu giờ ế và Níu chân khách VIP)",
        "AI tự động phân tích doanh thu để kích hoạt 2 chính sách phân tầng rõ rệt: "
        "1. Khách bình thường: Hưởng giờ vàng Happy Hour giảm 25% tiền giờ (từ 10k còn 7.5k/h) áp dụng toàn quán; "
        "2. Khách VIP đích danh (Nguyễn Việt Anh): Nhận ưu đãi kép: vừa giảm 25% tiền giờ, vừa được tặng thêm Voucher độc quyền 30.000 VNĐ gửi đích danh SMS về điện thoại!"
    )
    add_speech_box(
        doc,
        "Ở bước này, nhóm thực hiện luồng tương tác 6 bước bài bản: "
        "Bước A: Chạy tool lắng nghe python python/sms_gateway_listener.py trên Terminal bên phải; "
        "Bước B: Chạy lệnh SQL ai.fn_apply_promotions() trên Neon SQL bên trái; "
        "Bước C: Terminal bên phải tự động bắt sự kiện thời gian thực, tính toán giảm 25% tiền giờ từ 10k còn 7.5k và gửi SMS thành công đích danh cho anh Nguyễn Việt Anh (0327469105); "
        "Bước D: Chạy truy vấn SQL đối chiếu bảng giá tính tiền; "
        "Bước E: Chạy tool verify_promotion_engine.py để rà soát trực tiếp tầng lõi nhân CSDL, chứng minh sự phân tầng giữa khách thường và khách VIP; "
        "Bước F: Chạy hàm hoàn tác ai.fn_rollback_promotions() và kiểm chứng giá hệ thống lập tức trở về nguyên bản 10.000 VNĐ/giờ!"
    )
    
    # 4.4 - Bước A: Tool Python lắng nghe
    add_code_table(
        doc,
        "🖥️ BƯỚC A: CỬA SỔ PHẢI (TERMINAL POWERSHELL) - CHẠY TOOL PYTHON LẮNG NGHE CỔNG SMS GATEWAY:",
        "python python/sms_gateway_listener.py\n"
        "# Màn hình Terminal hiển thị: Đang lắng nghe kênh 'ai_sms_dispatch' và chờ lệnh từ Neon...\n"
        "# (Hoặc công cụ gửi SMS trực tiếp: python python/send_brandname_sms.py --promo --phone \"0327469105\" --name \"Nguyen Viet Anh\")"
    )
    
    # 4.4 - Bước B: Lệnh SQL kích hoạt trên Neon
    add_code_table(
        doc,
        "🖥️ BƯỚC B: CỬA SỔ TRÁI (NEON SQL EDITOR) - CHẠY LỆNH SQL KÍCH HOẠT KHUYẾN MÃI AI:",
        "SELECT applied_code, applied_name, target_info, discount_val, status_msg \n"
        "FROM ai.fn_apply_promotions();"
    )
    add_result_table(
        doc,
        ["applied_code", "applied_name", "target_info", "discount_val", "status_msg (Thực tế trên Neon)"],
        [
            ["HAPPY_AI_16H", "Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 16h)", "Toàn quán", "25.00%", "Đã kích hoạt thành công & Đã gửi thông báo tới điện thoại thật"],
            ["HAPPY_AI_0H", "Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 0h)", "Toàn quán", "25.00%", "Đã kích hoạt thành công & Đã gửi thông báo tới điện thoại thật"],
            ["VIP_VIETANH_30K", "Voucher VIP AI: Tặng 30.000 VNĐ cho Nguyễn Việt Anh (0327469105)", "0327469105", "30,000 VNĐ", "Đã kích hoạt thành công & Đã gửi thông báo tới điện thoại thật"]
        ]
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
        ["ma_khuyen_mai", "gia_goc_moi_gio", "phan_tram_giam", "so_tien_duoc_giam", "gia_thuc_te_phai_tra", "trang_thai_he_thong (Thực tế trên Neon)"],
        [
            ["HAPPY_AI_16H", "10.000 VNĐ", "25.00%", "2.500 VNĐ", "7.500 VNĐ", "ĐÃ ÁP DỤNG THÀNH CÔNG VÀO HỆ THỐNG TÍNH TIỀN"]
        ]
    )
    
    # 4.4 - Bước E: Rà soát lõi CSDL bằng Python Terminal
    add_code_table(
        doc,
        "🖥️ BƯỚC E [ĐIỂM NHẤN THUYẾT PHỤC GIẢNG VIÊN]: TERMINAL POWERSHELL - RÀ SOÁT TẦNG LÕI NHÂN CSDL:",
        "python python/verify_promotion_engine.py"
    )
    add_code_table(
        doc,
        "📊 KẾT QUẢ RÀ SOÁT TẦNG LÕI NHÂN CSDL HIỂN THỊ TRỰC TIẾP TRÊN TERMINAL:",
        "================================================================================\n"
        "     NETCAFE DATABASE CORE - KIỂM ĐỊNH LÕI TÍNH CƯỚC & KHUYẾN MÃI AI      \n"
        "================================================================================\n"
        "[*] 1. RÀ SOÁT ĐÍCH DANH KHÁCH HÀNG VIP THỤ HƯỞNG (VIP PERSONAL):\n"
        "    - Khách hàng VIP           : Nguyễn Việt Anh (ID: 12)\n"
        "    - Số điện thoại            : 0327469105\n"
        "    - Ràng buộc an ninh SMS    : CHỈ GỬI ĐÍCH DANH DUY NHẤT CHO NGUYỄN VIỆT ANH\n"
        "    - Voucher VIP độc quyền    : VIP_VIETANH_30K\n"
        "    - Giá trị Voucher tặng     : TẶNG 30,000 VNĐ (Trừ thẳng vào hóa đơn)\n"
        "    - Trạng thái Voucher VIP   : ĐANG KÍCH HOẠT (ACTIVE)\n"
        "    - Thời hạn sử dụng         : 31/10/2026 -> 03/11/2026\n"
        "--------------------------------------------------------------------------------\n"
        "[*] 2. RÀ SOÁT KHUYẾN MÃI TOÀN QUÁN (PUBLIC HAPPY HOUR - CHO MỌI KHÁCH):\n"
        "    - Đối tượng áp dụng        : TẤT CẢ KHÁCH HÀNG ĐANG CHƠI TẠI PHÒNG MÁY\n"
        "    - Trạng thái Khuyến mãi    : ĐANG KÍCH HOẠT (ACTIVE - GIỜ VÀNG HAPPY HOUR)\n"
        "    - Mã chương trình          : HAPPY_AI_16H\n"
        "    - Tên chương trình         : Khuyến Mãi AI: Giảm 25% Tiền Giờ (Khung 16h)\n"
        "    - Loại chiết khấu          : happy_hour (Giảm % tiền giờ chơi)\n"
        "    - Tỷ lệ giảm áp dụng       : GIẢM 25.0%\n"
        "    - Thời hạn hiệu lực        : 31/10/2026 -> 07/11/2026\n"
        "--------------------------------------------------------------------------------\n"
        "[*] 3. KẾT QUẢ ĐỐI CHIẾU TÍNH CƯỚC THỰC TẾ (LÕI CSDL POSTGRESQL):\n"
        "    ------------------------------------------------------------------------\n"
        "    (A) ĐỐI VỚI KHÁCH HÀNG BÌNH THƯỜNG (Chỉ hưởng Happy Hour toàn quán):\n"
        "        + Đơn giá giờ chơi gốc      : 10,000 VNĐ / giờ\n"
        "        + Mức giảm Happy Hour (25%) : -2,500 VNĐ\n"
        "        -> GIÁ KHÁCH THƯỜNG TRẢ     :  7,500 VNĐ / giờ\n"
        "    ------------------------------------------------------------------------\n"
        "    (B) ĐỐI VỚI KHÁCH VIP NGUYỄN VIỆT ANH (HƯỞNG ƯU ĐÃI KÉP):\n"
        "        + Đơn giá giờ chơi          :  7,500 VNĐ / giờ (Đã hưởng 25% Happy Hour)\n"
        "        + Ưu đãi đặc quyền VIP      : TẶNG THÊM VOUCHER -30,000 VNĐ\n"
        "        -> VÍ DỤ PHIÊN CHƠI 4 TIẾNG : 4h x 7.500đ = 30.000đ\n"
        "           Trừ tiếp Voucher VIP 30k : 30.000đ - 30.000đ = 0 VNĐ (HOÀN TOÀN MIỄN PHÍ!)\n"
        "    ------------------------------------------------------------------------\n"
        "[🔥 KẾT LUẬN KIỂM ĐỊNH LÕI]:\n"
        "    -> Khách thường và Khách VIP được phân tầng chiết khấu rõ ràng tại CSDL!\n"
        "    -> CSDL tự động tính đúng: Khách thường 7.500đ/h, Khách VIP nhận thêm Voucher 30.000đ.\n"
        "================================================================================"
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
    
    # 4.4 - Bước F: Lệnh hoàn tác Rollback trong SQL & Kiểm chứng
    add_code_table(
        doc,
        "🖥️ BƯỚC F [TÙY CHỌN BẢO VỆ]: HÀM HOÀN TÁC CÁC CHƯƠNG TRÌNH KHUYẾN MÃI DO AI TỰ ĐỘNG SINH:",
        "-- 1. Chạy trên Cửa sổ Neon SQL Editor để hoàn tác/thu hồi sạch sẽ:\n"
        "SELECT * FROM ai.fn_rollback_promotions();\n\n"
        "-- 2. Chạy trên Terminal PowerShell để kiểm chứng giá quay về 10k/h:\n"
        "python python/verify_promotion_engine.py"
    )
    add_result_table(
        doc,
        ["rolled_back_code", "status_msg (Thực tế trên Neon)"],
        [
            ["HAPPY_AI_16H", "Đã hoàn tác và xóa khỏi hệ thống khuyến mãi"],
            ["HAPPY_AI_0H", "Đã hoàn tác và xóa khỏi hệ thống khuyến mãi"],
            ["VIP_VIETANH_30K", "Đã hoàn tác và xóa khỏi hệ thống khuyến mãi"]
        ]
    )
    add_code_table(
        doc,
        "📊 KẾT QUẢ KIỂM CHỨNG SAU HOÀN TÁC (TERMINAL POWERSHELL):",
        "[*] 1. RÀ SOÁT ĐÍCH DANH KHÁCH HÀNG VIP THỤ HƯỞNG (VIP PERSONAL):\n"
        "    - Voucher VIP độc quyền    : KHÔNG CÓ (Đã thu hồi / Hoàn tác)\n"
        "    - Trạng thái Voucher VIP   : KHÔNG HOẠT ĐỘNG\n"
        "[*] 2. RÀ SOÁT KHUYẾN MÃI TOÀN QUÁN (PUBLIC HAPPY HOUR - CHO MỌI KHÁCH):\n"
        "    - Trạng thái Khuyến mãi    : ĐÃ HOÀN TÁC TOÀN BỘ (ĐÃ BỎ HẾT HAPPY HOUR)\n"
        "    - Chương trình Happy Hour  : Không có mã nào kích hoạt (Hệ thống chạy giá chuẩn)\n"
        "[*] 3. KẾT QUẢ ĐỐI CHIẾU TÍNH CƯỚC THỰC TẾ (LÕI CSDL POSTGRESQL):\n"
        "    -> Đơn giá giờ chơi tiêu chuẩn: 10,000 VNĐ / giờ (Không giảm giá)\n"
        "[✅ XÁC NHẬN HOÀN TÁC THÀNH CÔNG]:\n"
        "    -> Toàn bộ chương trình Happy Hour và Voucher VIP đã được dọn sạch khỏi Database!\n"
        "    -> Giá giờ chơi toàn bộ hệ thống đã TRỞ VỀ NGUYÊN BẢN 10,000 VNĐ / giờ như ban đầu!"
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
