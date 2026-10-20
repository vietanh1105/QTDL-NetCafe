// Trách nhiệm: xuất DTO hóa đơn và danh sách hóa đơn ra PDF/Excel.
// Liên kết: InvoiceDetailItem, InvoiceItem và các view Admin; không đọc trực tiếp control UI.

using ClosedXML.Excel;
using NetCafe.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NetCafe.App.Services;

public static class ExportService
{
    public static void ExportObjectsExcel<T>(IEnumerable<T> rows, string sheetName, string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);
        var properties = typeof(T).GetProperties().Where(property => property.CanRead).ToArray();
        for (var column = 0; column < properties.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = properties[column].Name;
        }
        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < properties.Length; column++)
            {
                var value = properties[column].GetValue(row);
                sheet.Cell(rowIndex, column + 1).Value = value?.ToString() ?? string.Empty;
            }
            rowIndex++;
        }
        sheet.Range(1, 1, 1, properties.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    public static void ExportReportPdf(string title, IEnumerable<string> lines, string path)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(36);
                page.Header().Text(title).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    foreach (var line in lines)
                    {
                        column.Item().Text(line);
                    }
                });
                page.Footer().AlignCenter().Text("Báo cáo được xuất từ NetCafe Admin");
            });
        }).GeneratePdf(path);
    }

    public static void ExportInvoicePdf(InvoiceDetailItem invoice, string path)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Margin(36);
                page.Header().Text("NETCAFE CYBER GAMING").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Hóa đơn #{invoice.InvoiceId}").Bold();
                    column.Item().Text($"Khách: {invoice.CustomerName} (@{invoice.Username})");
                    column.Item().Text($"Máy: {invoice.ComputerCode} | Phiên: {invoice.SessionId}");
                    column.Item().Text($"Thời gian: {invoice.IssuedAt:dd/MM/yyyy HH:mm:ss}");
                    column.Item().LineHorizontal(1);
                    column.Item().Text($"Tiền giờ: {invoice.PlayAmount:N0} đ");
                    column.Item().Text($"Tiền F&B: {invoice.ServiceAmount:N0} đ");
                    column.Item().Text($"Giảm giá: -{invoice.DiscountAmount:N0} đ");
                    foreach (var line in invoice.Lines)
                    {
                        column.Item().Text($"{line.ProductName} x{line.Quantity}: {line.LineTotal:N0} đ");
                    }
                    column.Item().LineHorizontal(1);
                    column.Item().Text($"TỔNG CỘNG: {invoice.TotalAmount:N0} đ").Bold().FontSize(14);
                    column.Item().Text($"Trạng thái: {invoice.Status} | Thanh toán: {invoice.PaymentMethod}");
                });
                page.Footer().AlignCenter().Text("Phiếu được xuất từ NetCafe Admin");
            });
        }).GeneratePdf(path);
    }

    public static void ExportInvoicesExcel(IEnumerable<InvoiceItem> invoices, string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Hóa đơn");
        var headers = new[] { "Mã hóa đơn", "Khách hàng", "Máy", "Tiền giờ", "Tiền F&B", "Giảm giá", "Tổng tiền", "Trạng thái", "Phương thức", "Thời gian" };
        for (var index = 0; index < headers.Length; index++)
        {
            sheet.Cell(1, index + 1).Value = headers[index];
        }
        var rowIndex = 2;
        foreach (var invoice in invoices)
        {
            sheet.Cell(rowIndex, 1).Value = invoice.InvoiceId;
            sheet.Cell(rowIndex, 2).Value = invoice.CustomerName;
            sheet.Cell(rowIndex, 3).Value = invoice.ComputerCode;
            sheet.Cell(rowIndex, 4).Value = (double)invoice.PlayAmount;
            sheet.Cell(rowIndex, 5).Value = (double)invoice.ServiceAmount;
            sheet.Cell(rowIndex, 6).Value = (double)invoice.DiscountAmount;
            sheet.Cell(rowIndex, 7).Value = (double)invoice.TotalAmount;
            sheet.Cell(rowIndex, 8).Value = invoice.Status;
            sheet.Cell(rowIndex, 9).Value = invoice.PaymentMethod;
            sheet.Cell(rowIndex, 10).Value = invoice.IssuedAt;
            rowIndex++;
        }
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }
}
