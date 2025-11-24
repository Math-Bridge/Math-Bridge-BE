using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Application.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IFinalFeedbackRepository _finalFeedbackRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IContractRepository _contractRepository;
        private readonly IPackageRepository _packageRepository;
        private readonly ISePayRepository _sePayRepository;

        public StatisticsService(
            IUserRepository userRepository,
            ISessionRepository sessionRepository,
            IFinalFeedbackRepository finalFeedbackRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IContractRepository contractRepository,
            IPackageRepository packageRepository,
            ISePayRepository sePayRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _finalFeedbackRepository = finalFeedbackRepository ?? throw new ArgumentNullException(nameof(finalFeedbackRepository));
            _walletTransactionRepository = walletTransactionRepository ?? throw new ArgumentNullException(nameof(walletTransactionRepository));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
            _sePayRepository = sePayRepository ?? throw new ArgumentNullException(nameof(sePayRepository));
        }

        #region User Statistics

        public async Task<UserStatisticsDto> GetUserStatisticsAsync()
        {
            var allUsers = await _userRepository.GetAllAsync();
            var tutors = await _userRepository.GetTutorsAsync();

            var parentCount = allUsers.Count(u => u.Role?.RoleName == "parent");
            var adminCount = allUsers.Count(u => u.Role?.RoleName == "admin");
            var staffCount = allUsers.Count(u => u.Role?.RoleName == "staff");

            var now = DateTime.UtcNow.ToLocalTime();
            var activeLast24Hours = allUsers.Count(u => u.LastActive >= now.AddHours(-24));
            var activeLast7Days = allUsers.Count(u => u.LastActive >= now.AddDays(-7));
            var activeLast30Days = allUsers.Count(u => u.LastActive >= now.AddDays(-30));

            return new UserStatisticsDto
            {
                TotalUsers = allUsers.Count,
                ActiveUsersLast24Hours = activeLast24Hours,
                ActiveUsersLastWeek = activeLast7Days,
                ActiveUsersLastMonth = activeLast30Days,
                TotalParents = parentCount,
                TotalTutors = tutors.Count,
                TotalAdmin = adminCount,
                TotalStaff = staffCount
            };
        }

        public async Task<UserRegistrationTrendStatisticsDto> GetUserRegistrationTrendsAsync(DateTime startDate, DateTime endDate)
        {
            var allUsers = await _userRepository.GetAllAsync();

            var usersInPeriod = allUsers
                .Where(u => u.CreatedDate >= startDate && u.CreatedDate <= endDate)
                .OrderBy(u => u.CreatedDate)
                .ToList();

            var trends = usersInPeriod
                .GroupBy(u => u.CreatedDate.Date)
                .Select(g => new UserRegistrationTrendDto
                {
                    Date = g.Key,
                    NewUsers = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            return new UserRegistrationTrendStatisticsDto
            {
                Trends = trends,
                TotalNewUsersInPeriod = usersInPeriod.Count
            };
        }

        public async Task<UserLocationStatisticsDto> GetUserLocationDistributionAsync()
        {
            var allUsers = await _userRepository.GetAllAsync();

            var cityDistribution = allUsers
                .Where(u => !string.IsNullOrEmpty(u.City))
                .GroupBy(u => u.City)
                .Select(g => new UserLocationDistributionDto
                {
                    City = g.Key,
                    UserCount = g.Count()
                })
                .OrderByDescending(d => d.UserCount)
                .ToList();

            return new UserLocationStatisticsDto
            {
                CityDistribution = cityDistribution,
                TotalCities = cityDistribution.Count
            };
        }

        public async Task<WalletStatisticsDto> GetWalletStatisticsAsync()
        {
            var allUsers = await _userRepository.GetAllAsync();
            var wallets = allUsers
                .Where(u => u.Role?.RoleName == "parent")
                .Select(u => u.WalletBalance)
                .OrderBy(b => b)
                .ToList();

            if (wallets.Count == 0)
            {
                return new WalletStatisticsDto
                {
                    TotalWalletBalance = 0,
                    AverageWalletBalance = 0,
                    MedianWalletBalance = 0,
                    MinWalletBalance = 0,
                    MaxWalletBalance = 0,
                    UsersWithZeroBalance = 0,
                    UsersWithPositiveBalance = 0
                };
            }

            var median = wallets.Count % 2 == 0
                ? (wallets[wallets.Count / 2 - 1] + wallets[wallets.Count / 2]) / 2
                : wallets[wallets.Count / 2];

            return new WalletStatisticsDto
            {
                TotalWalletBalance = wallets.Sum(),
                AverageWalletBalance = Math.Round((decimal)wallets.Average(), 2),
                MedianWalletBalance = median,
                MinWalletBalance = wallets.Min(),
                MaxWalletBalance = wallets.Max(),
                UsersWithZeroBalance = wallets.Count(b => b == 0),
                UsersWithPositiveBalance = wallets.Count(b => b > 0)
            };
        }

        #endregion

        #region Session Statistics

        public async Task<SessionStatisticsDto> GetSessionStatisticsAsync()
        {
            var allSessions = await _sessionRepository.GetSessionsInTimeRangeAsync(
                new DateTime(2000, 1, 1),
                DateTime.UtcNow.ToLocalTime().AddYears(10));

            var completedCount = allSessions.Count(s => s.Status.Equals("completed",StringComparison.OrdinalIgnoreCase));
            var cancelledCount = allSessions.Count(s => s.Status.Equals("cancelled",StringComparison.OrdinalIgnoreCase));
            var upcomingCount = allSessions.Count(s => s.Status.Equals("Scheduled",StringComparison.OrdinalIgnoreCase) && s.StartTime > DateTime.UtcNow.ToLocalTime());
            var rescheduledCount = allSessions.Count(s => s.Status.Equals("Rescheduled",StringComparison.OrdinalIgnoreCase));

            var completionRate = allSessions.Count > 0
                ? Math.Round((decimal)completedCount / allSessions.Count * 100, 2)
                : 0;

            return new SessionStatisticsDto
            {
                TotalSessions = allSessions.Count,
                CompletedSessions = completedCount,
                CancelledSessions = cancelledCount,
                UpcomingSessions = upcomingCount,
                RescheduledSessions = rescheduledCount,
                CompletionRate = completionRate
            };
        }

        public async Task<SessionOnlineVsOfflineDto> GetSessionOnlineVsOfflineAsync()
        {
            var allSessions = await _sessionRepository.GetSessionsInTimeRangeAsync(
                new DateTime(2000, 1, 1),
                DateTime.UtcNow.ToLocalTime().AddYears(10));

            var onlineCount = allSessions.Count(s => s.IsOnline);
            var offlineCount = allSessions.Count(s => !s.IsOnline);
            var totalCount = allSessions.Count;

            var onlinePercentage = totalCount > 0 ? Math.Round((decimal)onlineCount / totalCount * 100, 2) : 0;
            var offlinePercentage = totalCount > 0 ? Math.Round((decimal)offlineCount / totalCount * 100, 2) : 0;

            return new SessionOnlineVsOfflineDto
            {
                OnlineSessions = onlineCount,
                OfflineSessions = offlineCount,
                OnlinePercentage = onlinePercentage,
                OfflinePercentage = offlinePercentage
            };
        }

        public async Task<SessionTrendStatisticsDto> GetSessionTrendsAsync(DateTime startDate, DateTime endDate)
        {
            var allSessions = await _sessionRepository.GetSessionsInTimeRangeAsync(startDate, endDate);

            var trends = allSessions
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new SessionTrendDto
                {
                    Date = g.Key,
                    SessionCount = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            return new SessionTrendStatisticsDto
            {
                Trends = trends,
                TotalSessionsInPeriod = allSessions.Count
            };
        }

        #endregion

        #region Tutor Statistics

        public async Task<TutorStatisticsDto> GetTutorStatisticsAsync()
        {
            var tutors = await _userRepository.GetTutorsAsync();
            var allFinalFeedbacks = await _finalFeedbackRepository.GetAllAsync();
            var tutorFinalFeedbacks = allFinalFeedbacks.Where(f => tutors.Any(t => t.UserId == f.UserId)).ToList();

            var tutorsWithFeedback = tutorFinalFeedbacks.Select(f => f.UserId).Distinct().Count();
            var tutorsWithoutFeedback = tutors.Count - tutorsWithFeedback;

            var averageRating = tutorFinalFeedbacks.Count > 0
                ? Math.Round((decimal)tutorFinalFeedbacks.Average(f => f.OverallSatisfactionRating), 2)
                : 0;

            return new TutorStatisticsDto
            {
                TotalTutors = tutors.Count,
                AverageRating = averageRating,
                TutorsWithFeedback = tutorsWithFeedback,
                TutorsWithoutFeedback = tutorsWithoutFeedback
            };
        }

        public async Task<TopRatedTutorsListDto> GetTopRatedTutorsAsync(int limit = 10)
        {
            var tutors = await _userRepository.GetTutorsAsync();
            var allFinalFeedbacks = await _finalFeedbackRepository.GetAllAsync();

            var tutorRatings = tutors
                .Select(t => new
                {
                    Tutor = t,
                    Feedbacks = allFinalFeedbacks.Where(f => f.UserId == t.UserId).ToList()
                })
                .Where(tr => tr.Feedbacks.Count > 0)
                .Select(tr => new TopRatedTutorDto
                {
                    TutorId = tr.Tutor.UserId,
                    TutorName = tr.Tutor.FullName,
                    Email = tr.Tutor.Email,
                    AverageRating = Math.Round((decimal)tr.Feedbacks.Average(f => f.OverallSatisfactionRating), 2),
                    FeedbackCount = tr.Feedbacks.Count
                })
                .OrderByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.FeedbackCount)
                .Take(limit)
                .ToList();

            return new TopRatedTutorsListDto
            {
                Tutors = tutorRatings,
                TotalTutors = tutorRatings.Count
            };
        }

        public async Task<MostActiveTutorsListDto> GetMostActiveTutorsAsync(int limit = 10)
        {
            var tutors = await _userRepository.GetTutorsAsync();

            var tutorSessionCounts = new List<TutorSessionCountDto>();
            foreach (var tutor in tutors)
            {
                var sessions = await _sessionRepository.GetByTutorIdAsync(tutor.UserId);
                var completedSessions = sessions.Count(s => s.Status == "Completed");

                tutorSessionCounts.Add(new TutorSessionCountDto
                {
                    TutorId = tutor.UserId,
                    TutorName = tutor.FullName,
                    Email = tutor.Email,
                    SessionCount = sessions.Count,
                    CompletedSessions = completedSessions
                });
            }

            var topTutors = tutorSessionCounts
                .Where(t => t.SessionCount > 0)
                .OrderByDescending(t => t.SessionCount)
                .ThenByDescending(t => t.CompletedSessions)
                .Take(limit)
                .ToList();

            return new MostActiveTutorsListDto
            {
                Tutors = topTutors,
                TotalTutors = topTutors.Count
            };
        }

        #endregion

        #region Financial Statistics

        public async Task<RevenueStatisticsDto> GetRevenueStatisticsAsync()
        {
            var allTransactions = _sePayRepository.GetAllAsync().Result.ToList();
            var allUsers = await _userRepository.GetAllAsync();

            var successfulTransactions = allTransactions.Where(t => t.AccountNumber != null).ToList();

            var totalRevenue = successfulTransactions.Max(t => t.Accumulated);

            var successRate = allTransactions.Count > 0
                ? Math.Round((decimal)successfulTransactions.Count / allTransactions.Count * 100, 2)
                : 0;
            var averageTransaction = successfulTransactions.Count > 0
                ? Math.Round(totalRevenue / successfulTransactions.Count, 2)
                : 0;

            return new RevenueStatisticsDto
            {
                TotalRevenue = totalRevenue,
                AverageTransactionAmount = averageTransaction,
                TotalTransactions = allTransactions.Count,
                SuccessfulTransactions = successfulTransactions.Count,
                FailedTransactions = allTransactions.Count - successfulTransactions.Count,
                SuccessRate = successRate
            };
        }

        public async Task<RevenueTrendStatisticsDto> GetRevenueTrendsAsync(DateTime startDate, DateTime endDate)
        {
            var allTransactions = _sePayRepository.GetAllAsync().Result.ToList();

            var transactionsInPeriod = allTransactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.AccountNumber != null)
                .OrderBy(t => t.TransactionDate)
                .ToList();

            var trends = transactionsInPeriod
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new RevenueTrendDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(t => t.TransferAmount),
                    TransactionCount = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            return new RevenueTrendStatisticsDto
            {
                Trends = trends,
                TotalRevenueInPeriod = transactionsInPeriod.Sum(t => t.TransferAmount),
                TotalTransactionsInPeriod = transactionsInPeriod.Count
            };
        }

        #endregion
    }
}

