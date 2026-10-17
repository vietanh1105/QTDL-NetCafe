// Trách nhiệm: booking và overbooking; liên kết core.booking và EXCLUDE GiST.

namespace NetCafe.App.Views;

public partial class BookingView : AdminDataViewBase
{
    public BookingView()
    {
        InitializeComponent();
        SetTitle("Đặt máy trước", "EXCLUDE USING gist là rào chắn cuối cùng cho khoảng thời gian giao nhau.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetBookingsAsync(cancellationToken);
        SetRows(rows, $"{rows.Count} booking");
    }
}
