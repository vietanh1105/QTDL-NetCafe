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
            ? "Migration 08 đầy đủ"
            : "Migration 08 chưa đầy đủ: thiếu contract station";
        public string Summary => $"{Host} | {Database} | {User} | PostgreSQL {ServerVersion}";
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
        public decimal PlayAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "paid";
        public DateTime IssuedAt { get; set; }
    }

    public class EmployeeItem
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? DbUsername { get; set; }
        public string EmpRole { get; set; } = string.Empty;
        public string DefaultShift { get; set; } = string.Empty;
    }

    public class BookingItem
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ComputerCode { get; set; } = string.Empty;
        public DateTimeOffset BookedFrom { get; set; }
        public DateTimeOffset BookedTo { get; set; }
        public string Status { get; set; } = string.Empty;
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
}
