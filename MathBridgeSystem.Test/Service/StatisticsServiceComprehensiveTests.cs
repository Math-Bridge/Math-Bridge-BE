using FluentAssertions;
using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Application.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class StatisticsServiceComprehensiveTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<ISessionRepository> _sessionRepo;
        private readonly Mock<IFinalFeedbackRepository> _feedbackRepo;
        private readonly Mock<IWalletTransactionRepository> _walletRepo;
        private readonly Mock<IContractRepository> _contractRepo;
        private readonly Mock<IPackageRepository> _packageRepo;
        private readonly Mock<ISePayRepository> _sepayRepo;
        private readonly Mock<IWithdrawalRequestRepository> _withdrawalRepo;
        private readonly StatisticsService _service;

        public StatisticsServiceComprehensiveTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _sessionRepo = new Mock<ISessionRepository>();
            _feedbackRepo = new Mock<IFinalFeedbackRepository>();
            _walletRepo = new Mock<IWalletTransactionRepository>();
            _contractRepo = new Mock<IContractRepository>();
            _packageRepo = new Mock<IPackageRepository>();
            _sepayRepo = new Mock<ISePayRepository>();
            _withdrawalRepo = new Mock<IWithdrawalRequestRepository>();
            _service = new StatisticsService(_userRepo.Object, _sessionRepo.Object, _feedbackRepo.Object, _walletRepo.Object, _contractRepo.Object, _packageRepo.Object, _sepayRepo.Object, _withdrawalRepo.Object);
        }

        [Fact]
        public async Task GetUserStatisticsAsync_ComputesCounts()
        {
            var now = DateTime.UtcNow.ToLocalTime();
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { Role = new Role{ RoleName = "parent" }, LastActive = now.AddHours(-1) },
                new User { Role = new Role{ RoleName = "admin" }, LastActive = now.AddDays(-2) },
                new User { Role = new Role{ RoleName = "staff" }, LastActive = now.AddDays(-10) },
                new User { Role = new Role{ RoleName = "parent" }, LastActive = now.AddDays(-40) }
            });
            _userRepo.Setup(r => r.GetTutorsAsync()).ReturnsAsync(new List<User> { new User(), new User() });

            var dto = await _service.GetUserStatisticsAsync();
            dto.TotalUsers.Should().Be(4);
            dto.TotalParents.Should().Be(2);
            dto.TotalAdmin.Should().Be(1);
            dto.TotalStaff.Should().Be(1);
        }

        [Fact]
        public async Task GetUserRegistrationTrendsAsync_GroupsByDate()
        {
            var start = DateTime.Today.AddDays(-3);
            var end = DateTime.Today;
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User { CreatedDate = start },
                new User { CreatedDate = start.AddDays(1) },
                new User { CreatedDate = end }
            });

            var dto = await _service.GetUserRegistrationTrendsAsync(start, end);
            dto.TotalNewUsersInPeriod.Should().Be(3);
            dto.Trends.Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task GetUserLocationDistributionAsync_GroupsByCity()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User{ City = "A" }, new User{ City = "A" }, new User{ City = "B" }, new User{ City = null }
            });
            var dto = await _service.GetUserLocationDistributionAsync();
            dto.TotalCities.Should().Be(2);
        }

        [Fact]
        public async Task GetWalletStatisticsAsync_Empty_ReturnsZeros()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
            var dto = await _service.GetWalletStatisticsAsync();
            dto.TotalWalletBalance.Should().Be(0);
        }

        [Fact]
        public async Task GetWalletStatisticsAsync_ComputesMetrics()
        {
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User{ Role = new Role{ RoleName = "parent" }, WalletBalance = 0 },
                new User{ Role = new Role{ RoleName = "parent" }, WalletBalance = 100 },
                new User{ Role = new Role{ RoleName = "parent" }, WalletBalance = 200 },
            });
            var dto = await _service.GetWalletStatisticsAsync();
            dto.TotalWalletBalance.Should().Be(300);
            dto.UsersWithZeroBalance.Should().Be(1);
            dto.UsersWithPositiveBalance.Should().Be(2);
        }

        //[Fact]
        //public async Task GetSessionStatisticsAsync_Computes()
        //{
        //    _sessionRepo.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
        //        .ReturnsAsync(new List<Session>
        //        {
        //            new Session{ Status = "Completed", StartTime = DateTime.UtcNow.ToLocalTime().AddHours(-1) },
        //            new Session{ Status = "Cancelled" },
        //            new Session{ Status = "Scheduled", StartTime = DateTime.UtcNow.ToLocalTime().AddHours(1) },
        //            new Session{ Status = "Rescheduled" }
        //        });
        //    var dto = await _service.GetSessionStatisticsAsync();
        //    dto.CompletedSessions.Should().Be(1);
        //    dto.CancelledSessions.Should().Be(1);
        //    dto.UpcomingSessions.Should().Be(1);
        //    dto.RescheduledSessions.Should().Be(1);
        //}

        //[Fact]
        //public async Task GetSessionOnlineVsOfflineAsync_Computes()
        //{
        //    _sessionRepo.Setup(r => r.GetSessionsInTimeRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
        //        .ReturnsAsync(new List<Session>
        //        {
        //            new Session{ IsOnline = true }, new Session{ IsOnline = false }, new Session{ IsOnline = true }
        //        });
        //    var dto = await _service.GetSessionOnlineVsOfflineAsync();
        //    (dto.OnlineSessions + dto.OfflineSessions).Should().Be(3);
        //}

        //[Fact]
        //public async Task GetSessionTrendsAsync_GroupsByCreatedDate()
        //{
        //    var start = DateTime.Today.AddDays(-2);
        //    var end = DateTime.Today;
        //    _sessionRepo.Setup(r => r.GetSessionsInTimeRangeAsync(start, end))
        //        .ReturnsAsync(new List<Session>
        //        {
        //            new Session{ CreatedAt = start }, new Session{ CreatedAt = start }, new Session{ CreatedAt = end }
        //        });

        //    var dto = await _service.GetSessionTrendsAsync(start, end);
        //    dto.TotalSessionsInPeriod.Should().Be(3);
        //    dto.Trends.Should().HaveCount(2);
        //}

        [Fact]
        public async Task GetTutorStatisticsAsync_Computes()
        {
            var tutorId = Guid.NewGuid();
            _userRepo.Setup(r => r.GetTutorsAsync()).ReturnsAsync(new List<User> { new User { UserId = tutorId }, new User { UserId = Guid.NewGuid() } });
            _feedbackRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FinalFeedback>
            {
                new FinalFeedback{ UserId = tutorId, OverallSatisfactionRating = 4 },
                new FinalFeedback{ UserId = tutorId, OverallSatisfactionRating = 5 },
                new FinalFeedback{ UserId = Guid.NewGuid(), OverallSatisfactionRating = 3 }
            });

            var dto = await _service.GetTutorStatisticsAsync();
            dto.TotalTutors.Should().Be(2);
            dto.TutorsWithFeedback.Should().Be(1);
            dto.AverageRating.Should().Be(4.5m);
        }

        [Fact]
        public async Task GetTopRatedTutorsAsync_SortsAndLimits()
        {
            var tutorA = new User { UserId = Guid.NewGuid(), FullName = "A", Email = "a@a" };
            var tutorB = new User { UserId = Guid.NewGuid(), FullName = "B", Email = "b@b" };
            _userRepo.Setup(r => r.GetTutorsAsync()).ReturnsAsync(new List<User> { tutorA, tutorB });
            _feedbackRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FinalFeedback>
            {
                new FinalFeedback{ UserId = tutorA.UserId, OverallSatisfactionRating = 5 },
                new FinalFeedback{ UserId = tutorB.UserId, OverallSatisfactionRating = 4 },
                new FinalFeedback{ UserId = tutorB.UserId, OverallSatisfactionRating = 4 }
            });

            var dto = await _service.GetTopRatedTutorsAsync(1);
            dto.Tutors.Should().HaveCount(1);
            dto.Tutors[0].TutorName.Should().Be("A");
        }

        [Fact]
        public async Task GetMostActiveTutorsAsync_ComputesCounts()
        {
            var tutorA = new User { UserId = Guid.NewGuid(), FullName = "A", Email = "a@a" };
            var tutorB = new User { UserId = Guid.NewGuid(), FullName = "B", Email = "b@b" };
            _userRepo.Setup(r => r.GetTutorsAsync()).ReturnsAsync(new List<User> { tutorA, tutorB });
            _sessionRepo.Setup(r => r.GetByTutorIdAsync(tutorA.UserId)).ReturnsAsync(new List<Session> { new Session { Status = "Completed" }, new Session { Status = "Scheduled" } });
            _sessionRepo.Setup(r => r.GetByTutorIdAsync(tutorB.UserId)).ReturnsAsync(new List<Session> { new Session { Status = "Completed" } });

            var dto = await _service.GetMostActiveTutorsAsync(2);
            dto.Tutors.Should().HaveCount(2);
            dto.Tutors[0].SessionCount.Should().BeGreaterThanOrEqualTo(dto.Tutors[1].SessionCount);
        }

        [Fact]
        public async Task GetRevenueStatisticsAsync_Computes()
        {
            _sepayRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SepayTransaction>
            {
                new SepayTransaction{ AccountNumber = "1", Accumulated = 100, TransferAmount = 100, TransactionDate = DateTime.Today },
                new SepayTransaction{ AccountNumber = null, Accumulated = 0, TransferAmount = 0, TransactionDate = DateTime.Today }
            });
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var dto = await _service.GetRevenueStatisticsAsync();
            dto.TotalTransactions.Should().Be(2);
            dto.SuccessfulTransactions.Should().Be(1);
            dto.TotalRevenue.Should().Be(100);
        }

        [Fact]
        public async Task GetRevenueTrendsAsync_GroupsByDate()
        {
            var start = DateTime.Today.AddDays(-2);
            var end = DateTime.Today;
            _sepayRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SepayTransaction>
            {
                new SepayTransaction{ AccountNumber = "1", TransferAmount = 50, TransactionDate = start },
                new SepayTransaction{ AccountNumber = "1", TransferAmount = 25, TransactionDate = end }
            });

            var dto = await _service.GetRevenueTrendsAsync(start, end);
            dto.TotalTransactionsInPeriod.Should().Be(2);
            dto.TotalRevenueInPeriod.Should().Be(75);
        }
    }
}
