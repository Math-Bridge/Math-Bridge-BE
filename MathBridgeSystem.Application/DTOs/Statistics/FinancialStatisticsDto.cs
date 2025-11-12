using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.Statistics
{
    public class RevenueStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class RevenueTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
    }

    public class RevenueTrendStatisticsDto
    {
        public List<RevenueTrendDto> Trends { get; set; } = new List<RevenueTrendDto>();
        public decimal TotalRevenueInPeriod { get; set; }
        public int TotalTransactionsInPeriod { get; set; }
    }

    public class RevenueByPackageDto
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public decimal TotalRevenue { get; set; }
        public int ContractCount { get; set; }
        public decimal AverageRevenuePerContract { get; set; }
    }

    public class RevenueByPackageListDto
    {
        public List<RevenueByPackageDto> Packages { get; set; } = new List<RevenueByPackageDto>();
        public decimal TotalRevenue { get; set; }
    }
}

