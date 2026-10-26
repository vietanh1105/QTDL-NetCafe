-- ============================================================================
-- CONTRACT BỔ SUNG CHO BÁO CÁO VÀ TELEMETRY DBA
-- Mục đích: đồng bộ database chính đã tồn tại với schema nguồn mới.
-- Tính chất: idempotent, không reset hoặc xóa dữ liệu nghiệp vụ.
-- ============================================================================

\c netcafe_db;

CREATE EXTENSION IF NOT EXISTS btree_gist;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'core.gaming_session'::regclass
          AND conname = 'ex_gaming_session_computer_time'
    ) THEN
        ALTER TABLE core.gaming_session
            ADD CONSTRAINT ex_gaming_session_computer_time
            EXCLUDE USING gist (
                computer_id WITH =,
                tstzrange(
                    start_time,
                    COALESCE(end_time, 'infinity'::timestamptz),
                    '[)'
                ) WITH &&
            )
            WHERE (status IN ('active', 'finished'));
    END IF;
END $$;

CREATE MATERIALIZED VIEW IF NOT EXISTS reporting.mv_revenue_daily AS
SELECT
    revenue_date,
    total_invoices,
    total_play_amount,
    total_service_amount,
    total_discount_amount,
    net_revenue
FROM reporting.vw_daily_revenue;

CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_revenue_daily_date
    ON reporting.mv_revenue_daily (revenue_date);

CREATE OR REPLACE FUNCTION reporting.sp_refresh_materialized_views()
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, reporting
AS $$
BEGIN
    REFRESH MATERIALIZED VIEW reporting.mv_revenue_daily;
    REFRESH MATERIALIZED VIEW reporting.mv_hourly_usage;
    REFRESH MATERIALIZED VIEW reporting.mv_top_customers;
    REFRESH MATERIALIZED VIEW reporting.mv_monthly_revenue;
END;
$$;

GRANT EXECUTE ON FUNCTION reporting.sp_refresh_materialized_views() TO role_admin, role_manager;
GRANT EXECUTE ON FUNCTION hr.sp_create_maintenance(INT, INT, TEXT) TO role_admin, role_manager, role_technician;
GRANT EXECUTE ON FUNCTION hr.sp_complete_maintenance(INT, VARCHAR, NUMERIC) TO role_admin, role_manager, role_technician;
GRANT MAINTAIN ON ALL TABLES IN SCHEMA core, inventory, billing, hr, audit, ai TO role_admin;
GRANT SELECT ON reporting.mv_revenue_daily TO role_admin, role_manager, role_cashier, role_technician;
GRANT ALL ON reporting.mv_revenue_daily TO role_admin;
GRANT SELECT ON ALL TABLES IN SCHEMA reporting TO role_admin, role_manager, role_cashier, role_technician;
