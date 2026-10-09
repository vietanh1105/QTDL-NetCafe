-- ============================================================================
-- HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
-- Môn: Quản trị Cơ sở Dữ liệu (QTDL)
-- File: 00_create_database.sql
-- Mục đích: Khởi tạo Database độc lập netcafe_db không phụ thuộc locale HĐH
-- Lưu ý: Chạy file này khi đang kết nối tới Database 'postgres' bằng user postgres (Superuser)
-- ============================================================================

-- 1. Đóng kết nối hiện có tới netcafe_db nếu đang tồn tại
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'netcafe_db' AND pid <> pg_backend_pid();

-- 2. Xóa database cũ nếu đã tồn tại để đảm bảo tính tái lập (idempotent)
DROP DATABASE IF EXISTS netcafe_db;

-- 3. Tạo database mới chuẩn hóa UTF-8 từ template0
-- Giải thích thiết kế: 
-- - TEMPLATE template0 + ENCODING 'UTF8': Đảm bảo CSDL hoàn toàn độc lập với Locale 
--   và bảng mã của máy chủ cài đặt (đặc biệt khi chấm trên môi trường Windows hoặc Linux khác nhau).
-- - LC_COLLATE = 'C', LC_CTYPE = 'C': Tránh phụ thuộc vào collation tiếng Việt của OS, 
--   việc tìm kiếm không dấu và so khớp chuỗi sẽ được giải quyết triệt để thông qua extension 'unaccent' và 'pg_trgm'.
CREATE DATABASE netcafe_db
    WITH 
    TEMPLATE = template0
    ENCODING = 'UTF8'
    LC_COLLATE = 'C'
    LC_CTYPE = 'C'
    CONNECTION LIMIT = -1;

COMMENT ON DATABASE netcafe_db IS 'Database quản lý phòng game/net - Đề tài môn Quản trị dữ liệu';
