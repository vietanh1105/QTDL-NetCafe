// Trách nhiệm: booking và overbooking; liên kết core.booking và EXCLUDE GiST.

namespace NetCafe.App.Views;

public partial class BookingView : AdminCardViewBase
{
    public BookingView()
    {
        InitializeComponent();
        SetTitle("Đặt máy trước", "EXCLUDE USING gist là rào chắn cuối cùng cho khoảng thời gian giao nhau.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetBookingsAsync(cancellationToken);
        SetCards(
            rows,
            booking => new AdminCardModel(
                $"{booking.CustomerName} · {booking.ComputerCode}",
                $"{booking.BookedFrom:dd/MM HH:mm} → {booking.BookedTo:HH:mm}",
                $"Booking #{booking.BookingId}",
                booking.Status,
                booking.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase)
                    ? CyberTheme.Success
                    : CyberTheme.Warning),
            $"{rows.Count} booking");
    }
}
