// Trách nhiệm: sơ đồ card máy theo ComputerId và mở panel thao tác của Admin.
// Liên kết: vw_computer_grid, ComputerCardControl và ComputerActionDialog.

using NetCafe.App.Controls;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class ComputersView : AdminDataViewBase
{
    private readonly Dictionary<int, ComputerCardControl> _cards = new();
    private bool _isRebuildingGrid;

    public ComputersView()
    {
        InitializeComponent();
        SetTitle("Sơ đồ máy trạm", "Tín hiệu máy và đồng bộ phiên được xử lý ở CSDL; sơ đồ cập nhật theo thời gian thực.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync(cancellationToken);
        var activeIds = rows.Select(row => row.ComputerId).ToHashSet();
        foreach (var stale in _cards.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            var card = _cards[stale];
            _cardsGrid.Controls.Remove(card);
            card.Dispose();
            _cards.Remove(stale);
        }

        foreach (var row in rows)
        {
            if (!_cards.TryGetValue(row.ComputerId, out var card))
            {
                card = new ComputerCardControl();
                card.CardSelected += HandleCardSelected;
                _cards.Add(row.ComputerId, card);
            }
            card.Bind(row);
        }

        RebuildGridPositions();
        SetTitle("Sơ đồ máy trạm", $"{rows.Count} máy trạm | Cập nhật lúc {DateTime.Now:HH:mm:ss}");
    }

    private void RebuildGridPositions()
    {
        if (_isRebuildingGrid)
        {
            return;
        }

        _isRebuildingGrid = true;
        try
        {
            const int minimumCardWidth = 240;
            const int gap = 16;
            const int verticalMargin = 12;
            var availableWidth = Math.Max(
                1,
                _cardsGrid.ClientSize.Width
                - _cardsGrid.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth);
            var columnCount = availableWidth >= (minimumCardWidth + gap) * 4
                ? 4
                : availableWidth >= (minimumCardWidth + gap) * 3
                    ? 3
                    : availableWidth >= (minimumCardWidth + gap) * 2
                        ? 2
                        : 1;
            var cardWidth = Math.Max(
                columnCount == 1 ? 1 : minimumCardWidth,
                (availableWidth - columnCount * gap) / columnCount);

            _cardsGrid.SuspendLayout();
            foreach (var card in _cards.Values.OrderBy(item => item.Model?.ComputerId))
            {
                if (!_cardsGrid.Controls.Contains(card))
                {
                    _cardsGrid.Controls.Add(card);
                }
                card.Width = cardWidth;
                card.Height = 190;
                // Hai margin dọc tạo khoảng trống 24px giữa các hàng, giữ rõ viền card.
                card.Margin = new Padding(0, verticalMargin, gap, verticalMargin);
            }
            _cardsGrid.ResumeLayout(true);
        }
        finally
        {
            _isRebuildingGrid = false;
        }
    }

    private void HandleCardSelected(object? sender, ComputerGridItem computer)
    {
        using var dialog = new ComputerActionDialog(computer, RequiredContext);
        dialog.ShowDialog(FindForm());
    }
}
