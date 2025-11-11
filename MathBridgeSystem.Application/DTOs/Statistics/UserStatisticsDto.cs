using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.Statistics
{
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsersLast24Hours { get; set; }
        public int ActiveUsersLastWeek { get; set; }
        public int ActiveUsersLastMonth { get; set; }
        public int TotalParents { get; set; }
        public int TotalTutors { get; set; }
        public int TotalAdmin { get; set; }
        public int TotalStaff { get; set; }
    }

    public class UserRegistrationTrendDto
    {
        public DateTime Date { get; set; }
        public int NewUsers { get; set; }
    }

    public class UserRegistrationTrendStatisticsDto
    {
        public List<UserRegistrationTrendDto> Trends { get; set; } = new List<UserRegistrationTrendDto>();
        public int TotalNewUsersInPeriod { get; set; }
    }

    public class UserLocationDistributionDto
    {
        public string City { get; set; } = null!;
        public int UserCount { get; set; }
    }

    public class UserLocationStatisticsDto
    {
        public List<UserLocationDistributionDto> CityDistribution { get; set; } = new List<UserLocationDistributionDto>();
        public int TotalCities { get; set; }
    }

    public class WalletStatisticsDto
    {
        public decimal TotalWalletBalance { get; set; }
        public decimal AverageWalletBalance { get; set; }
        public decimal MedianWalletBalance { get; set; }
        public decimal MinWalletBalance { get; set; }
        public decimal MaxWalletBalance { get; set; }
        public int UsersWithZeroBalance { get; set; }
        public int UsersWithPositiveBalance { get; set; }
    }
}

