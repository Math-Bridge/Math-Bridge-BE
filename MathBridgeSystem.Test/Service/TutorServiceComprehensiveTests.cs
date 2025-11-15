using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class TutorServiceComprehensiveTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<ITutorCenterRepository> _tutorCenterRepo;
        private readonly Mock<ITutorScheduleRepository> _tutorScheduleRepo;
        private readonly Mock<IFinalFeedbackRepository> _feedbackRepo;
        private readonly TutorService _service;

        public TutorServiceComprehensiveTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _tutorCenterRepo = new Mock<ITutorCenterRepository>();
            _tutorScheduleRepo = new Mock<ITutorScheduleRepository>();
            _feedbackRepo = new Mock<IFinalFeedbackRepository>();
            _service = new TutorService(_userRepo.Object, _tutorCenterRepo.Object, _tutorScheduleRepo.Object, _feedbackRepo.Object);
        }

        [Fact]
        public async Task GetTutorByIdAsync_NotFound_Throws()
        {
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);
            await FluentActions.Invoking(() => _service.GetTutorByIdAsync(Guid.NewGuid(), Guid.NewGuid(), "admin"))
                .Should().ThrowAsync<Exception>().WithMessage("Tutor not found");
        }

        [Fact]
        public async Task GetTutorByIdAsync_NotTutor_Throws()
        {
            var id = Guid.NewGuid();
            _userRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new User { UserId = id, Role = new Role { RoleName = "parent" } });
            await FluentActions.Invoking(() => _service.GetTutorByIdAsync(id, Guid.NewGuid(), "admin"))
                .Should().ThrowAsync<Exception>().WithMessage("User is not a tutor");
        }

        [Fact]
        public async Task GetTutorByIdAsync_MapsAllCollections()
        {
            var id = Guid.NewGuid();
            var user = new User
            {
                UserId = id,
                FullName = "Tutor",
                Email = "t@t.com",
                Role = new Role { RoleName = "tutor" },
                TutorVerification = new TutorVerification
                {
                    VerificationId = Guid.NewGuid(),
                    University = "U",
                    Major = "M",
                    HourlyRate = 10,
                    Bio = "Bio",
                    VerificationStatus = "Approved",
                    VerificationDate = DateTime.Today,
                    CreatedDate = DateTime.Today
                }
            };
            _userRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
            _tutorCenterRepo.Setup(r => r.GetByTutorIdAsync(id)).ReturnsAsync(new List<TutorCenter>
            {
                new TutorCenter{ TutorCenterId = Guid.NewGuid(), CenterId = Guid.NewGuid(), Center = new Center{ CenterId = Guid.NewGuid(), Name = "C1" }, CreatedDate = DateTime.Today }
            });
            _tutorScheduleRepo.Setup(r => r.GetByTutorIdAsync(id)).ReturnsAsync(new List<TutorSchedule>
            {
                new TutorSchedule{ AvailabilityId = Guid.NewGuid(), DaysOfWeek = 1, AvailableFrom = new TimeOnly(8,0), AvailableUntil = new TimeOnly(12,0), EffectiveFrom = DateOnly.FromDateTime(DateTime.Today), CanTeachOnline = true, CanTeachOffline = false, IsBooked = false, Status = "Active", CreatedDate = DateTime.Today }
            });
            _feedbackRepo.Setup(r => r.GetByUserIdAsync(id)).ReturnsAsync(new List<FinalFeedback>
            {
                new FinalFeedback{ FeedbackId = Guid.NewGuid(), UserId = id, FeedbackText = "Great", OverallSatisfactionRating = 5, CreatedDate = DateTime.UtcNow }
            });

            var dto = await _service.GetTutorByIdAsync(id, id, "admin");
            dto.UserId.Should().Be(id);
            dto.TutorCenters.Should().HaveCount(1);
            dto.TutorSchedules.Should().HaveCount(1);
            dto.FinalFeedbacks.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateTutorAsync_Unauthorized_Throws()
        {
            await FluentActions.Invoking(() => _service.UpdateTutorAsync(Guid.NewGuid(), new UpdateTutorRequest(), Guid.NewGuid(), "parent"))
                .Should().ThrowAsync<Exception>().WithMessage("Unauthorized access");
        }

        [Fact]
        public async Task UpdateTutorAsync_NotFound_Throws()
        {
            var id = Guid.NewGuid();
            await FluentActions.Invoking(() => _service.UpdateTutorAsync(id, new UpdateTutorRequest(), id, "admin"))
                .Should().ThrowAsync<Exception>().WithMessage("Tutor not found");
        }

        [Fact]
        public async Task UpdateTutorAsync_NotTutor_Throws()
        {
            var id = Guid.NewGuid();
            _userRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new User { UserId = id, Role = new Role { RoleName = "parent" } });
            await FluentActions.Invoking(() => _service.UpdateTutorAsync(id, new UpdateTutorRequest(), id, "admin"))
                .Should().ThrowAsync<Exception>().WithMessage("User is not a tutor");
        }

        [Fact]
        public async Task UpdateTutorAsync_InvalidGender_Throws()
        {
            var id = Guid.NewGuid();
            _userRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new User { UserId = id, Role = new Role { RoleName = "tutor" } });
            var req = new UpdateTutorRequest { Gender = "Alien" };
            await FluentActions.Invoking(() => _service.UpdateTutorAsync(id, req, id, "admin"))
                .Should().ThrowAsync<Exception>().WithMessage("Invalid gender value*");
        }

        [Fact]
        public async Task UpdateTutorAsync_Valid_UpdatesUserAndVerification()
        {
            var id = Guid.NewGuid();
            var user = new User { UserId = id, Role = new Role { RoleName = "tutor" }, TutorVerification = new TutorVerification { University = "OldU" } };
            _userRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
            _userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            var req = new UpdateTutorRequest
            {
                FullName = "New Name",
                Gender = "Male",
                TutorVerification = new MathBridgeSystem.Application.DTOs.TutorVerification.UpdateTutorVerificationRequest { University = "NewU", HourlyRate = 20 }
            };

            var result = await _service.UpdateTutorAsync(id, req, id, "admin");
            result.Should().Be(id);
            user.FullName.Should().Be("New Name");
            user.Gender.Should().Be("Male");
            user.TutorVerification!.University.Should().Be("NewU");
        }

        [Fact]
        public async Task GetAllTutorsAsync_FiltersOnlyTutors()
        {
            var tutorId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
            {
                new User{ UserId = tutorId, Role = new Role{ RoleName = "tutor" } },
                new User{ UserId = parentId, Role = new Role{ RoleName = "parent" } },
            });
            _tutorCenterRepo.Setup(r => r.GetByTutorIdAsync(tutorId)).ReturnsAsync(new List<TutorCenter>());
            _tutorScheduleRepo.Setup(r => r.GetByTutorIdAsync(tutorId)).ReturnsAsync(new List<TutorSchedule>());
            _feedbackRepo.Setup(r => r.GetByUserIdAsync(tutorId)).ReturnsAsync(new List<FinalFeedback>());

            var list = await _service.GetAllTutorsAsync();
            list.Should().HaveCount(1);
            list[0].UserId.Should().Be(tutorId);
        }
    }
}
