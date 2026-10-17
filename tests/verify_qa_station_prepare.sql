-- Trách nhiệm: tạo fixture station TEST_ trên netcafe_qa trước khi kiểm thử role giới hạn.
-- Liên kết: verify_qa_station.sql và tests/verify_qa_station_assert.sql.

\set ON_ERROR_STOP on

DO $$
BEGIN
    INSERT INTO core.customer(full_name, phone, email, username, password_hash, balance, is_active)
    VALUES (
        'TEST Station QA',
        '0930000001',
        'test_station_qa@example.com',
        'test_station_qa',
        public.crypt('Test@123', public.gen_salt('bf')),
        80000,
        TRUE
    )
    ON CONFLICT (username) DO UPDATE
    SET full_name = EXCLUDED.full_name,
        phone = EXCLUDED.phone,
        email = EXCLUDED.email,
        password_hash = EXCLUDED.password_hash,
        balance = GREATEST(core.customer.balance, 80000),
        is_active = TRUE;
END $$;

SELECT 'PASS fixture_ready' AS result;
