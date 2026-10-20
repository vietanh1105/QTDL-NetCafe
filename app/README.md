# HƯỚNG DẪN KHỞI CHẠY PHẦN MỀM DESKTOP (NETCAFE PRO)

> **Công nghệ:** C# / .NET 10 (Windows Forms) + PostgreSQL 17 + Npgsql  
> **Kiến trúc:** 3 Projects phân tách rõ ràng (NetCafe.Data, NetCafe.Data.Tests, NetCafe.App)

---

## 1. CÁCH KHỞI CHẠY PHẦN MỀM TRỰC TIẾP

Mở PowerShell tại thư mục gốc dự án và chạy:

```powershell
dotnet run --project app/NetCafe.App
```

Cửa sổ giao diện WinForms sẽ bật lên ngay lập tức trên màn hình Windows của bạn!

---

## 2. NGUYÊN TẮC VÀ ĐẶC ĐIỂM KHI DEMO

1. **Khung báo lỗi đỏ từ PostgreSQL:**  
   - Bất kỳ thao tác nào vi phạm ràng buộc (chèn SĐT sai regex, gọi món vượt tồn kho `CHECK`, số dư không đủ) sẽ được CSDL chặn đứng.
   - Giao diện bắt đúng đối tượng `PostgresException` và hiển thị chi tiết mã `SQLSTATE`, tên ràng buộc và thông báo lỗi nguyên bản từ PostgreSQL.
2. **Nhật ký SQL thời gian thực (Chân cửa sổ):**  
   - Mọi thao tác click chuột trên phần mềm đều ghi nhận rõ câu lệnh `SELECT`, `CALL` hay Function kèm tham số và thời gian chạy (ms).
   - Dễ dàng đối chiếu song song với cửa sổ pgAdmin khi bảo vệ đồ án.
3. **Cơ chế Thin Client:**  
   - Phần mềm chỉ là lớp hiển thị, 100% logic nghiệp vụ (mở phiên, tính tiền, áp mã khuyến mãi, trừ tồn kho) nằm trong CSDL PostgreSQL 17.

---

## 3. CHẠY BỘ KIỂM THỬ TÍCH HỢP XUNIT

```powershell
dotnet test app/NetCafe.Data.Tests
```
