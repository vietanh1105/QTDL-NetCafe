// Trách nhiệm: sơ đồ card máy theo ComputerId và mở panel thao tác của Admin.
// Liên kết: vw_computer_grid, ComputerCardControl và ComputerActionDialog.

using NetCafe.App.Controls;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class ComputersView : AdminDataViewBase
{
    private readonly Dictionary<int, ComputerCardControl> _cards = new();

    public ComputersView()
    {
        InitializeComponent();
        SetTitle("Sơ đồ máy trạm", "Heartbeat/reconcile được xử lý ở database; card chỉ cập nhật theo ComputerId.");
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
        SetTitle("Sơ đồ máy trạm", $"{rows.Count} máy | cập nhật {DateTime.Now:HH:mm:ss} | card chỉ cập nhật theo ComputerId");
    }

    private void RebuildGridPositions()
    {
        var width = Math.Max(300, _cardsViewport.ClientSize.Width - _cardsViewport.Padding.Horizontal - _cardsGrid.Padding.Horizontal);
        var columnCount = width >= 1100 ? 4 : width >= 760 ? 3 : 2;
        _cardsGrid.RowCount = Math.Max(1, (int)Math.Ceiling((double)_cards.Count / columnCount));
        _cardsGrid.RowStyles.Clear();
        for (var row = 0; row < _cardsGrid.RowCount; row++)
        {
            _cardsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var index = 0;
        foreach (var card in _cards.Values.OrderBy(item => item.Model?.ComputerId))
        {
            if (!_cardsGrid.Controls.Contains(card))
            {
                _cardsGrid.Controls.Add(card);
            }
            _cardsGrid.SetCellPosition(card, new TableLayoutPanelCellPosition(index % columnCount, index / columnCount));
            index++;
        }
        LayoutCards();
    }

    private void HandleCardSelected(object? sender, ComputerGridItem computer)
    {
        using var dialog = new ComputerActionDialog(computer, RequiredContext);
        dialog.ShowDialog(FindForm());
    }
}
