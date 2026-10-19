// Trách nhiệm: booking và overbooking; liên kết core.booking và EXCLUDE GiST.

namespace NetCafe.App.Views;

public partial class BookingView : AdminCardViewBase
{
    public BookingView()
    {
        InitializeComponent();
        SetTitle("Đặt máy trước", "Ràng buộc EXCLUDE GiST trong CSDL ngăn chặn tuyệt đối trùng lặp thời gian đặt máy.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetBookingsAsync(cancellationToken);
        SetCards(
            rows,
            booking =>
            {
                var statusText = booking.Status.ToLowerInvariant() switch
                {
                    "confirmed" => "Đã xác nhận",
                    "pending" => "Chờ duyệt",
                    "cancelled" => "Đã hủy",
                    "completed" => "Hoàn thành",
                    _ => booking.Status
                };

                return new AdminCardModel(
                    $"{booking.CustomerName} · {booking.ComputerCode}",
                    $"{booking.BookedFrom:dd/MM HH:mm} → {booking.BookedTo:HH:mm}",
                    $"Mã đặt #{booking.BookingId}",
                    statusText,
                    booking.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase)
                        ? CyberTheme.Success
                        : CyberTheme.Warning);
            },
            $"{rows.Count} lượt đặt máy");
    }
}
