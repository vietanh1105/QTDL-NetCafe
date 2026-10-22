// Trách nhiệm: sơ đồ card máy theo ComputerId và mở panel thao tác của Admin.
// Liên kết: vw_computer_grid, ComputerCardControl và ComputerActionDialog.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Controls;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class ComputersView : AdminDataViewBase
{
    private readonly Dictionary<int, ComputerCardControl> _cards = new();
    private readonly Label _kpiLabel = new();
    private readonly ComboBox _filterCombo = new();
    private bool _isRebuildingGrid;
    private List<ComputerGridItem> _lastRows = new();

    public ComputersView()
    {
        InitializeComponent();
        SetTitle("Sơ đồ máy trạm", "Tín hiệu máy và đồng bộ phiên được xử lý ở CSDL; sơ đồ cập nhật theo thời gian thực.");

        _kpiLabel.AutoSize = true;
        _kpiLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _kpiLabel.ForeColor = CyberTheme.Text;
        _kpiLabel.TextAlign = ContentAlignment.MiddleLeft;
        _kpiLabel.Padding = new Padding(0, 7, 16, 0);
        _kpiLabel.Text = "Đang tải dữ liệu máy trạm...";

        _filterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _filterCombo.Width = 145;
        _filterCombo.Font = new Font("Segoe UI", 9F);
        _filterCombo.Items.AddRange(new object[] { "Tất cả máy", "🟢 Đang chơi", "⚪ Máy trống", "🔴 Bảo trì" });
        _filterCombo.SelectedIndex = 0;
        _filterCombo.Margin = new Padding(0, 4, 8, 4);
        _filterCombo.SelectedIndexChanged += (_, _) => FilterCards();

        ActionsPanel.Controls.Add(_kpiLabel);
        ActionsPanel.Controls.Add(_filterCombo);
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync(cancellationToken);
        _lastRows = rows.ToList();

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

        UpdateKpiSummary(rows);
        FilterCards();
        SetTitle("Sơ đồ máy trạm", $"{rows.Count} máy trạm | Cập nhật lúc {DateTime.Now:HH:mm:ss}");
    }

    private void UpdateKpiSummary(IReadOnlyList<ComputerGridItem> rows)
    {
        var total = rows.Count;
        var inUse = rows.Count(r => r.ComputerStatus == "in_use");
        var available = rows.Count(r => r.ComputerStatus == "available");
        var maintenance = rows.Count(r => r.ComputerStatus is "maintenance" or "broken");

        _kpiLabel.Text = $"Tổng: {total} máy  |  🟢 Đang chơi: {inUse}  |  ⚪ Máy trống: {available}  |  🔴 Bảo trì: {maintenance}";
    }

    private void FilterCards()
    {
        var filter = _filterCombo.SelectedItem?.ToString() ?? "Tất cả máy";
        foreach (var card in _cards.Values)
        {
            var status = card.Model?.ComputerStatus;
            card.Visible = filter switch
            {
                "🟢 Đang chơi" => status == "in_use",
                "⚪ Máy trống" => status == "available",
                "🔴 Bảo trì" => status is "maintenance" or "broken",
                _ => true
            };
        }
        RebuildGridPositions();
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
