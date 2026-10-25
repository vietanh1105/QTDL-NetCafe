// Trách nhiệm: sơ đồ card máy theo ComputerId và mở panel thao tác của Admin.
// Liên kết: vw_computer_grid, ComputerCardControl và ComputerActionDialog.

using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using NetCafe.App.Controls;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class ComputersView : AdminDataViewBase
{
    private readonly Dictionary<int, ComputerCardControl> _cards = new();
    private readonly Panel _headerTitlePanel = new();
    private readonly Label _titleHeaderLabel = new();
    private readonly Label _subtitleHeaderLabel = new();
    private readonly Label _badgeTotal = new();
    private readonly Label _badgeInUse = new();
    private readonly Label _badgeAvailable = new();
    private readonly Label _badgeMaintenance = new();
    private readonly ComboBox _filterCombo = new();
    private bool _isRebuildingGrid;
    private List<ComputerGridItem> _lastRows = new();

    public ComputersView()
    {
        InitializeComponent();

        Toolbar.Height = 60;
        Toolbar.Padding = new Padding(16, 8, 16, 8);

        TitleLabel.Visible = false;
        TitleLabel.Width = 0;

        _headerTitlePanel.Dock = DockStyle.Left;
        _headerTitlePanel.AutoSize = true;
        _headerTitlePanel.BackColor = Color.Transparent;

        _titleHeaderLabel.Text = "SƠ ĐỒ MÁY TRẠM";
        _titleHeaderLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _titleHeaderLabel.ForeColor = CyberTheme.Text;
        _titleHeaderLabel.AutoSize = true;
        _titleHeaderLabel.Location = new Point(0, 3);

        _subtitleHeaderLabel.Text = "Đang tải dữ liệu máy trạm...";
        _subtitleHeaderLabel.Font = new Font("Segoe UI", 8.5F);
        _subtitleHeaderLabel.ForeColor = CyberTheme.MutedText;
        _subtitleHeaderLabel.AutoSize = true;
        _subtitleHeaderLabel.Location = new Point(0, 26);

        _headerTitlePanel.Controls.Add(_titleHeaderLabel);
        _headerTitlePanel.Controls.Add(_subtitleHeaderLabel);

        StyleBadge(_badgeTotal, "🖥️ Tổng: 0", Color.FromArgb(203, 213, 225), Color.FromArgb(30, 41, 59));
        StyleBadge(_badgeInUse, "🟢 Đang chơi: 0", Color.FromArgb(34, 197, 94), Color.FromArgb(20, 50, 35));
        StyleBadge(_badgeAvailable, "🟡 Máy trống: 0", Color.FromArgb(250, 204, 21), Color.FromArgb(60, 48, 15));
        StyleBadge(_badgeMaintenance, "🔴 Bảo trì: 0", Color.FromArgb(248, 113, 113), Color.FromArgb(60, 20, 25));

        _badgeTotal.Click += (_, _) => _filterCombo.SelectedItem = "Tất cả máy";
        _badgeInUse.Click += (_, _) => _filterCombo.SelectedItem = "🟢 Đang chơi";
        _badgeAvailable.Click += (_, _) => _filterCombo.SelectedItem = "🟡 Máy trống";
        _badgeMaintenance.Click += (_, _) => _filterCombo.SelectedItem = "🔴 Bảo trì";

        _filterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _filterCombo.Width = 135;
        _filterCombo.Height = 30;
        _filterCombo.Font = new Font("Segoe UI", 9F);
        _filterCombo.Items.AddRange(new object[] { "Tất cả máy", "🟢 Đang chơi", "🟡 Máy trống", "🔴 Bảo trì" });
        _filterCombo.SelectedIndex = 0;
        _filterCombo.Margin = new Padding(6, 6, 4, 6);
        _filterCombo.BackColor = CyberTheme.SurfaceRaised;
        _filterCombo.ForeColor = CyberTheme.Text;
        _filterCombo.FlatStyle = FlatStyle.Flat;
        _filterCombo.SelectedIndexChanged += (_, _) => FilterCards();

        RefreshButton.Width = 36;
        RefreshButton.Height = 30;
        RefreshButton.Margin = new Padding(4, 6, 0, 6);

        ActionsPanel.Dock = DockStyle.Right;
        ActionsPanel.AutoSize = true;
        ActionsPanel.WrapContents = false;
        ActionsPanel.AutoScroll = false;
        ActionsPanel.FlowDirection = FlowDirection.LeftToRight;
        ActionsPanel.Padding = new Padding(0);
        ActionsPanel.Margin = new Padding(0);

        ActionsPanel.Controls.Add(_badgeTotal);
        ActionsPanel.Controls.Add(_badgeInUse);
        ActionsPanel.Controls.Add(_badgeAvailable);
        ActionsPanel.Controls.Add(_badgeMaintenance);
        ActionsPanel.Controls.Add(_filterCombo);
        ActionsPanel.Controls.Add(RefreshButton);

        Toolbar.Controls.Add(_headerTitlePanel);

        EnableDoubleBuffering(_cardsGrid);
        _cardsGrid.Resize += (_, _) => RebuildGridPositions();
    }

    private static void StyleBadge(Label label, string text, Color foreColor, Color backColor)
    {
        label.Text = text;
        label.ForeColor = foreColor;
        label.BackColor = backColor;
        label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        label.AutoSize = true;
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Padding = new Padding(8, 5, 8, 5);
        label.Margin = new Padding(3, 6, 3, 6);
        label.BorderStyle = BorderStyle.None;
        label.Cursor = Cursors.Hand;
    }

    private static void EnableDoubleBuffering(Control control)
    {
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(control, true, null);
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await RequiredContext.Repository.GetComputersAsync(cancellationToken);
            if (rows is null) return;
            _lastRows = rows.ToList();

            var activeIds = rows.Select(row => row.ComputerId).ToHashSet();
            var structureChanged = false;

            foreach (var stale in _cards.Keys.Where(id => !activeIds.Contains(id)).ToList())
            {
                var card = _cards[stale];
                _cardsGrid.Controls.Remove(card);
                card.Dispose();
                _cards.Remove(stale);
                structureChanged = true;
            }

            foreach (var row in rows)
            {
                if (!_cards.TryGetValue(row.ComputerId, out var card))
                {
                    card = new ComputerCardControl();
                    card.CardSelected += HandleCardSelected;
                    _cards.Add(row.ComputerId, card);
                    structureChanged = true;
                }
                card.Bind(row);
            }

            UpdateKpiSummary(rows);
            FilterCards(triggerRebuild: structureChanged);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void UpdateKpiSummary(IReadOnlyList<ComputerGridItem> rows)
    {
        var total = rows.Count;
        var inUse = rows.Count(r => r.ComputerStatus == "in_use");
        var available = rows.Count(r => r.ComputerStatus == "available");
        var maintenance = rows.Count(r => r.ComputerStatus is "maintenance" or "broken");

        _badgeTotal.Text = $"🖥️ Tổng: {total}";
        _badgeInUse.Text = $"🟢 Đang chơi: {inUse}";
        _badgeAvailable.Text = $"🟡 Máy trống: {available}";
        _badgeMaintenance.Text = $"🔴 Bảo trì: {maintenance}";
        _subtitleHeaderLabel.Text = $"{total} máy trạm  •  Cập nhật lúc {DateTime.Now:HH:mm:ss}";
    }

    private void FilterCards(bool triggerRebuild = true)
    {
        var filter = _filterCombo.SelectedItem?.ToString() ?? "Tất cả máy";
        var visibilityChanged = false;

        foreach (var card in _cards.Values)
        {
            var status = card.Model?.ComputerStatus;
            var targetVisible = filter switch
            {
                "🟢 Đang chơi" => status == "in_use",
                "🟡 Máy trống" => status == "available",
                "⚪ Máy trống" => status == "available",
                "🔴 Bảo trì" => status is "maintenance" or "broken",
                _ => true
            };

            if (card.Visible != targetVisible)
            {
                card.Visible = targetVisible;
                visibilityChanged = true;
            }
        }

        if (triggerRebuild || visibilityChanged)
        {
            RebuildGridPositions();
        }
    }

    private void RebuildGridPositions()
    {
        if (_isRebuildingGrid || _cardsGrid.IsDisposed)
        {
            return;
        }

        _isRebuildingGrid = true;
        try
        {
            const int minimumCardWidth = 240;
            const int gap = 16;
            const int verticalMargin = 8;

            int clientWidth = _cardsGrid.ClientSize.Width;
            int horizontalPadding = _cardsGrid.Padding.Horizontal;
            int scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
            int availableWidth = Math.Max(minimumCardWidth, clientWidth - horizontalPadding - scrollbarWidth);

            var columnCount = availableWidth >= (minimumCardWidth + gap) * 4
                ? 4
                : availableWidth >= (minimumCardWidth + gap) * 3
                    ? 3
                    : availableWidth >= (minimumCardWidth + gap) * 2
                        ? 2
                        : 1;

            var cardWidth = Math.Max(
                columnCount == 1 ? availableWidth : minimumCardWidth,
                (availableWidth - (columnCount - 1) * gap) / columnCount);

            var visibleCards = _cards.Values
                .Where(c => c.Visible)
                .OrderBy(item => item.Model?.ComputerId)
                .ToList();

            _cardsGrid.SuspendLayout();
            for (int i = 0; i < visibleCards.Count; i++)
            {
                var card = visibleCards[i];
                if (!_cardsGrid.Controls.Contains(card))
                {
                    _cardsGrid.Controls.Add(card);
                }
                if (card.Width != cardWidth) card.Width = cardWidth;
                if (card.Height != 190) card.Height = 190;

                bool isLastInRow = ((i + 1) % columnCount == 0);
                card.Margin = new Padding(0, verticalMargin, isLastInRow ? 0 : gap, verticalMargin);
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
