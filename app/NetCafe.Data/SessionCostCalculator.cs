// Trách nhiệm: tính tiền giờ tạm tính cho phiên đang hoạt động khi view chưa có play_amount.
// Quy tắc làm tròn phải khớp core.fn_calc_session_amount trong PostgreSQL.

namespace NetCafe.Data;

public static class SessionCostCalculator
{
    public static decimal EstimatePlayAmount(
        decimal hourlyRate,
        decimal? playedMinutes,
        decimal? reportedPlayAmount)
    {
        if (reportedPlayAmount is > 0)
        {
            return reportedPlayAmount.Value;
        }

        if (hourlyRate <= 0 || playedMinutes is not > 0)
        {
            return 0;
        }

        return Math.Round(
            hourlyRate * playedMinutes.Value / 60m,
            0,
            MidpointRounding.AwayFromZero);
    }
}
