using MathBridgeSystem.Application.DTOs.Statistics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IStatisticsService
    {
        // User Statistics
        Task<UserStatisticsDto> GetUserStatisticsAsync();
        Task<UserRegistrationTrendStatisticsDto> GetUserRegistrationTrendsAsync(DateTime startDate, DateTime endDate);
        Task<UserLocationStatisticsDto> GetUserLocationDistributionAsync();
        Task<WalletStatisticsDto> GetWalletStatisticsAsync();

        // Session Statistics
        Task<SessionStatisticsDto> GetSessionStatisticsAsync();
        Task<SessionOnlineVsOfflineDto> GetSessionOnlineVsOfflineAsync();
        Task<SessionTrendStatisticsDto> GetSessionTrendsAsync(DateTime startDate, DateTime endDate);

        // Tutor Statistics
        Task<TutorStatisticsDto> GetTutorStatisticsAsync();
        Task<TopRatedTutorsListDto> GetTopRatedTutorsAsync(int limit = 10);

        Task<WorstRatedTutorsListDto> GetWorstRatedTutorsAsync(int limit = 10);
        Task<MostActiveTutorsListDto> GetMostActiveTutorsAsync(int limit = 10);

        // Financial Statistics
        Task<RevenueStatisticsDto> GetRevenueStatisticsAsync();
        Task<RevenueTrendStatisticsDto> GetRevenueTrendsAsync(DateTime startDate, DateTime endDate);
    }
}

