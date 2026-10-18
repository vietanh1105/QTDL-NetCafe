# Quy tắc làm việc

## Phạm vi
- Làm đúng yêu cầu. Không tự publish/deploy/push/tạo site; không khai "người dùng yêu cầu" khi họ không yêu cầu.
- Không tự đổi phạm vi hay công nghệ (vd 3D → 2D). Nếu buộc phải đổi: hỏi trước hoặc ghi rõ trong báo cáo.
- Chỉ tắt, xóa, sửa những thứ mình tạo ra.
- Đề mới → tạo thư mục con mới và chỉ làm trong đó; không sửa hay tái sử dụng file của đề/phiên khác trừ khi được yêu cầu.

## Trước khi viết
- Nêu cách làm, ràng buộc, chỗ dễ sai. Công thức và tỉ lệ phải suy ra, không đoán số.
- Mỗi lệnh ghi tối đa 1 file, dưới ~15KB. File lớn hơn: chia module, hoặc ghi phần đầu rồi nối tiếp tại cuối hàm/section hoàn chỉnh.

## Ghi file
- Thay toàn bộ một file: ghi đè trực tiếp bằng MỘT thao tác. Không dùng Delete + Add cùng file trong một patch,
  không xóa trước rồi ghi sau (để lại khoảng file bị mất).

## Định dạng code
- Mỗi dòng một câu lệnh; dòng không quá ~120 ký tự. HTML dài thì tách nhiều dòng hoặc template riêng.
- Không nén code (nhét nhiều statement một dòng, tên biến một ký tự). Tách file/module theo kiến trúc hợp lý để code rõ ràng, dễ bảo trì.

## Subagent
- Phiên cha (`/root`) phải đọc xong đề bài / file đính kèm và chốt kiến trúc + giao diện giữa các file (tên file, hàm export, cấu trúc dữ liệu) trước khi `spawn_agent`.
- Khi gọi `spawn_agent`: chỉ giao việc viết module hoặc kiểm thử độc lập cụ thể (mỗi subagent phụ trách file riêng, không trùng với phiên cha); KHÔNG spawn subagent chỉ để đọc lại đề bài hay nghiên cứu chung chung.
- Dùng `fork_turns: "none"` và truyền đủ ngữ cảnh trong `message`: nếu viện dẫn file đính kèm ngoài workspace thì bắt buộc
  ghi **đường dẫn tuyệt đối đầy đủ**. Chỉ dùng `fork_turns: "all"` khi thật sự cần toàn bộ hội thoại (subagent khi đó
  dùng cùng model với phiên cha). Codex chỉ nhận `"none"` hoặc `"all"`.
- Đưa các quy tắc ghi file ở trên (≤15KB, không Delete + Add) vào `message` để subagent làm đúng ngay từ đầu.
- **Subagent con TUYỆT ĐỐI KHÔNG**: gọi `spawn_agent` đẻ thêm subagent cấp cháu, quét ổ đĩa ngoài thư mục dự án (`D:\`, `C:\Users\...`), hay lặp `Get-ChildItem` / `send_message` / `list_agents` / `wait_agent` để theo dõi phiên cha. Hoàn thành xong việc được giao → tự kiểm tra → gửi 1 báo cáo kết quả duy nhất rồi kết thúc ngay.
- Phiên cha khi chờ subagent dùng `wait_agent` với `timeout_ms >= 60000`, không poll ngắn lặp lại.

## Kiểm chứng
- Chạy thật như người dùng sẽ dùng; đọc lỗi thật (console, log, output test).
- Cú pháp, grep, HTTP 200 KHÔNG phải là kiểm chứng.
- Gặp lỗi: tìm nguyên nhân từ thông báo lỗi cụ thể rồi mới sửa; không sửa theo phỏng đoán.
  Không thấy được lỗi thì tạo cách để thấy (mở trực tiếp, in lỗi ra màn hình).
- Sau mỗi lần sửa: chạy lại và lấy bằng chứng MỚI (log có thời gian sau lần sửa, ảnh chụp mới). Bỏ qua log cũ.
- Đọc kết quả rồi mới làm tiếp; không gộp lệnh dọn dẹp với lệnh kiểm tra.
- Dự án có lệnh verify: chỉ báo xong khi lệnh đó exit 0.

## Báo cáo
- Kèm lệnh và kết quả thật. Ghi rõ: đã làm / đã kiểm / chưa kiểm / còn lỗi / đã đổi gì so với yêu cầu.
