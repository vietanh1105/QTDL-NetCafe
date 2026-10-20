-- Trách nhiệm: loại bỏ contract Đặt máy khỏi database đã tồn tại sau khi có backup.
-- Liên kết: sql/01_schema.sql, sql/04_security.sql, tests/verify.sql và DbRepository.
-- Bảo vệ: chỉ xóa core.booking; không reset, seed, truncate hay cascade dữ liệu khác.

DO $$
DECLARE
    v_booking REGCLASS;
    v_dependency TEXT;
BEGIN
    v_booking := to_regclass('core.booking');
    IF v_booking IS NULL THEN
        RAISE NOTICE 'core.booking đã không tồn tại; không có gì để xóa.';
        RETURN;
    END IF;

    SELECT pg_describe_object(d.classid, d.objid, d.objsubid)
    INTO v_dependency
    FROM pg_depend AS d
    WHERE d.refobjid = v_booking
      AND d.deptype = 'n'
      AND NOT (d.classid = 'pg_class'::regclass AND d.objid = v_booking);

    IF v_dependency IS NOT NULL THEN
        RAISE EXCEPTION 'core.booking còn dependency ngoài phạm vi: %', v_dependency
            USING ERRCODE = '2BP01';
    END IF;

    DROP TABLE core.booking;
    RAISE NOTICE 'Đã xóa core.booking có kiểm soát; dữ liệu các bảng nghiệp vụ khác được giữ nguyên.';
END $$;

COMMENT ON SCHEMA core IS 'Nghiệp vụ phòng máy; contract Đặt máy đã được loại bỏ trong migration 09.';
