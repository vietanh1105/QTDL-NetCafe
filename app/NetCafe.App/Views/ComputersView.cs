// Trách nhiệm: sơ đồ card máy theo ComputerId; liên kết vw_computer_grid và reconcile.

using System.Drawing;
using NetCafe.App.Controls;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class ComputersView : AdminDataViewBase
{
    private readonly Dictionary<int, ComputerCardControl> _cards = new();

    public ComputersView()
    {
        InitializeComponent();
        SetTitle("Sơ đồ máy trạm", "Heartbeat/reconcile được xử lý ở database; refresh chỉ chạy khi view đang hiện.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync();
        var activeIds = rows.Select(row => row.ComputerId).ToHashSet();

        foreach (var stale in _cards.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            _cardsPanel.Controls.Remove(_cards[stale]);
            _cards[stale].Dispose();
            _cards.Remove(stale);
        }

        foreach (var row in rows)
        {
            if (!_cards.TryGetValue(row.ComputerId, out var card))
            {
                card = new ComputerCardControl();
                card.CardSelected += HandleCardSelected;
                _cards.Add(row.ComputerId, card);
                _cardsPanel.Controls.Add(card);
            }
            card.Bind(row);
        }

        SetTitle("Sơ đồ máy trạm", $"{rows.Count} máy | cập nhật {DateTime.Now:HH:mm:ss} | card chỉ cập nhật theo ComputerId");
    }

    private async void HandleCardSelected(object? sender, ComputerGridItem computer)
    {
        if (computer.CurrentSessionId is null)
        {
            MessageBox.Show($"{computer.ComputerCode}\nTrạng thái: {computer.ComputerStatus}\nKhách: chưa có", "Chi tiết máy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var message = $"{computer.ComputerCode}\nKhách: {computer.CurrentCustomerName}\n" +
                      $"Thời gian: {computer.PlayedMinutes:N0} phút\nChi phí dự kiến: {computer.HourlyRate * (computer.PlayedMinutes ?? 0) / 60:N0}đ\n\nĐóng phiên ngay?";
        if (RequiredContext.CurrentEmployeeId <= 0 || MessageBox.Show(message, "Chi tiết phiên", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await RequiredContext.Repository.CloseSessionAndPayAsync(computer.CurrentSessionId.Value, RequiredContext.CurrentEmployeeId);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}
