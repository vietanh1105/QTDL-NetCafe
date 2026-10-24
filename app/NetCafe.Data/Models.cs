// Trách nhiệm: DTO thuần cho Admin/Station; mapping được thực hiện trong repository.
// Không chứa SQL hoặc trạng thái UI.

using System;
using System.Collections.Generic;

namespace NetCafe.Data
{
    public class DbExceptionInfo
    {
        public string SqlState { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ConstraintName { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[LỖI CSDL - SQLSTATE {SqlState}] {Message}\n" +
                   (string.IsNullOrEmpty(ConstraintName) ? "" : $"Ràng buộc vi phạm: {ConstraintName}\n") +
                   (string.IsNullOrEmpty(Detail) ? "" : $"Chi tiết: {Detail}");
        }
    }

    public sealed class DatabaseRuntimeInfo
    {
        public string Database { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public string Host { get; init; } = string.Empty;
        public string ServerVersion { get; init; } = string.Empty;
        public bool HasStationMigration { get; init; }
        public bool HasReconcileFunction { get; init; }
        public bool HasComputerGridContract { get; init; }
        public bool HasCustomerMenuContract { get; init; }
        public bool HasStationLogoutContract { get; init; }
        public bool IsStationContractReady => HasStationMigration
            && HasReconcileFunction
            && HasComputerGridContract
            && HasCustomerMenuContract
            && HasStationLogoutContract;
        public string ContractStatus => IsStationContractReady
            ? "Bản cập nhật 08 đầy đủ"
            : "Bản cập nhật 08 chưa đầy đủ: thiếu cấu trúc máy trạm";
        public string Summary => $"Máy chủ: {Host} | CSDL: {Database} | Người dùng: {User} | PostgreSQL {ServerVersion}";
    }

    public class SqlLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string CommandText { get; set; } = string.Empty;
        public string Parameters { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public override string ToString()
        {
            var status = IsSuccess ? "THÀNH CÔNG" : "LỖI";
            return $"[{Timestamp:HH:mm:ss}] ({DurationMs}ms) [{status}] {CommandText} {(string.IsNullOrEmpty(Parameters) ? "" : $"| {Parameters}")}" +
                   (IsSuccess ? "" : $" -> {ErrorMessage}");
        }
    }

    public class ComputerGridItem
    {
        public int ComputerId { get; set; }
        public string ComputerCode { get; set; } = string.Empty;
        public string ComputerName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string ComputerStatus { get; set; } = "available";
        public string IpAddress { get; set; } = string.Empty;
        public long? CurrentSessionId { get; set; }
        public int? CurrentCustomerId { get; set; }
        public string? CurrentCustomerName { get; set; }
        public string? CurrentUsername { get; set; }
        public DateTime? SessionStartTime { get; set; }
        public decimal? PlayedMinutes { get; set; }
        public decimal? CurrentPlayAmount { get; set; }
        public decimal? CurrentServiceAmount { get; set; }
        public decimal? CurrentBalance { get; set; }
        public DateTime? LastHeartbeatAt { get; set; }
        public string Specs { get; set; } = "{}";
    }

    public class CustomerItem
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public int LoyaltyPoints { get; set; }
        public string MembershipTier { get; set; } = "regular";
        public bool IsActive { get; set; }
    }

    public class ProductItem
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int StockQty { get; set; }
    }

    public class InvoiceItem
    {
        public long InvoiceId { get; set; }
        public long SessionId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ComputerCode { get; set; } = string.Empty;
        public decimal PlayAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "paid";
        public DateTime IssuedAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = "session";
        public string Note { get; set; } = string.Empty;
    }

    public sealed record InvoiceDetailItem
    {
        public long InvoiceId { get; init; }
        public long SessionId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string ComputerCode { get; init; } = string.Empty;
        public DateTime? SessionStart { get; init; }
        public DateTime? SessionEnd { get; init; }
        public decimal PlayAmount { get; init; }
        public decimal ServiceAmount { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime IssuedAt { get; init; }
        public string PaymentMethod { get; init; } = string.Empty;
        public IReadOnlyList<InvoiceLineItem> Lines { get; init; } = Array.Empty<InvoiceLineItem>();
    }

    public sealed class InvoiceLineItem
    {
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

    public class EmployeeItem
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? DbUsername { get; set; }
        public string EmpRole { get; set; } = string.Empty;
        public string DefaultShift { get; set; } = string.Empty;
        public decimal? Salary { get; set; }
        public DateTime? HiredDate { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class MaintenanceItem
    {
        public int MaintenanceId { get; init; }
        public int ComputerId { get; init; }
        public string ComputerCode { get; init; } = string.Empty;
        public int TechnicianId { get; init; }
        public string TechnicianName { get; init; } = string.Empty;
        public string IssueDescription { get; init; } = string.Empty;
        public DateTime StartedAt { get; init; }
        public DateTime? FinishedAt { get; init; }
        public decimal Cost { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public class DailyRevenueItem
    {
        public DateTime RevenueDate { get; set; }
        public long TotalInvoices { get; set; }
        public decimal TotalPlayAmount { get; set; }
        public decimal TotalServiceAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal NetRevenue { get; set; }
    }

    public class ComputerUsageItem
    {
        public int ComputerId { get; set; }
        public string ComputerCode { get; set; } = string.Empty;
        public string ComputerName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public long TotalSessions { get; set; }
        public decimal TotalHoursPlayed { get; set; }
        public decimal OccupancyRate => Math.Min(100.0m, Math.Round((TotalHoursPlayed / 720.0m) * 100.0m, 1));
        public decimal TotalPlayRevenue { get; set; }
    }

    public class TopCustomerItem
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string MembershipTier { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public long TotalSessions { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPlayedAt { get; set; }
    }

    public class KitchenOrderItem
    {
        public long OrderId { get; set; }
        public long SessionId { get; set; }
        public string ComputerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ItemsSummary { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "placed";
        public DateTime OrderedAt { get; set; }
    }

    public class ActivePromotionItem
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string PromotionName { get; set; } = string.Empty;
        public string PromoType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
    }
}
