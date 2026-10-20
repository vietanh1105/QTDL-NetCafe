// Designer layout cho sơ đồ máy: host cuộn riêng và grid tự tính 4/3/2 cột.
// Liên kết: ComputersView.cs giữ dictionary card theo ComputerId.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Controls;

namespace NetCafe.App.Views;

public partial class ComputersView
{
    private Panel _cardsViewport = null!;
    private TableLayoutPanel _cardsGrid = null!;

    private void InitializeComponent()
    {
        _cardsViewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(243, 246, 251),
            Padding = new Padding(16, 16, 24, 24)
        };
        _cardsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0),
            GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };
        _cardsViewport.Resize += (_, _) => LayoutCards();
        _cardsViewport.Controls.Add(_cardsGrid);
        Controls.Add(_cardsViewport);
        Grid.Visible = false;
        Name = "ComputersView";
        TabIndex = 0;
    }

    private void LayoutCards()
    {
        if (_cardsGrid is null || _cardsViewport is null)
        {
            return;
        }

        var width = Math.Max(300, _cardsViewport.ClientSize.Width - _cardsViewport.Padding.Horizontal - _cardsGrid.Padding.Horizontal);
        var columnCount = width >= 1100 ? 4 : width >= 760 ? 3 : 2;
        var cardWidth = Math.Max(190, (width - (columnCount * 12)) / columnCount);
        _cardsGrid.SuspendLayout();
        _cardsGrid.ColumnCount = columnCount;
        _cardsGrid.ColumnStyles.Clear();
        for (var column = 0; column < columnCount; column++)
        {
            _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columnCount));
        }

        foreach (Control control in _cardsGrid.Controls)
        {
            if (control is ComputerCardControl card)
            {
                card.Width = cardWidth;
                card.Height = 190;
            }
        }
        _cardsGrid.ResumeLayout(true);
    }
}
