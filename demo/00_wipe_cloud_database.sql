-- ============================================================================
-- BUỔI BẢO VỆ TIỂU LUẬN MÔN QUẢN TRỊ CƠ SỞ DỮ LIỆU (QTDL)
-- BƯỚC 0: XÓA SẠCH TOÀN BỘ CSDL ĐỂ DEMO TRƯỚC MẶT THẦY TỪ CON SỐ 0
-- ============================================================================

-- 1. Xóa sạch toàn bộ 8 Schemas và các bảng phụ thuộc (CASCADE):
DROP SCHEMA IF EXISTS core, inventory, billing, hr, audit, ai, reporting, staging CASCADE;

-- 2. Chứng minh CSDL trên Cloud đang hoàn toàn TRỐNG TRƠN (Kết quả trả về: 0 bảng):
SELECT count(table_name) AS total_tables 
FROM information_schema.tables 
WHERE table_schema IN ('core', 'inventory', 'billing', 'hr', 'audit', 'ai', 'reporting', 'staging');

/*
KẾT QUẢ QUAN SÁT TRÊN DBEAVER / PGADMIN:
- total_tables = 0
- Khi nhấn F5 trên cây thư mục Schemas bên trái, toàn bộ các bảng biến mất hoàn toàn!
- CSDL đã sẵn sàng để trình diễn luồng Khởi tạo tự động từ con số 0.
*/

