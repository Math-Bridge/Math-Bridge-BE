using FluentAssertions;
using MathBridgeSystem.Domain.Entities;
using Xunit;

namespace MathBridgeSystem.Tests.DomainTests
{
    public class DomainEntityTests
    {
        [Fact]
        public void User_CanBeCreatedWithProperties()
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FullName = "Test User",
                WalletBalance = 100.50m,
                Status = "active"
            };

            user.Email.Should().Be("test@example.com");
            user.WalletBalance.Should().Be(100.50m);
            user.Status.Should().Be("active");
        }

        [Fact]
        public void Child_CanBeCreatedWithProperties()
        {
            var child = new Child
            {
                ChildId = Guid.NewGuid(),
                FullName = "Child Name",
                Grade = "Grade 5",
                Status = "active"
            };

            child.FullName.Should().Be("Child Name");
            child.Grade.Should().Be("Grade 5");
            child.Status.Should().Be("active");
        }

        [Fact]
        public void Contract_CanBeCreatedWithDates()
        {
            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
                Status = "active"
            };

            contract.StartDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
            contract.EndDate.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddMonths(1)));
            contract.Status.Should().Be("active");
        }

        [Fact]
        public void Session_CanBeCreatedWithTimes()
        {
            var session = new Session
            {
                BookingId = Guid.NewGuid(),
                SessionDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                Status = "scheduled"
            };

            session.Status.Should().Be("scheduled");
            session.SessionDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        }

        [Fact]
        public void PaymentPackage_CanBeCreatedWithPrice()
        {
            var package = new PaymentPackage
            {
                PackageId = Guid.NewGuid(),
                PackageName = "Basic Package",
                Price = 500000m,
                SessionCount = 10,
                IsActive = true
            };

            package.PackageName.Should().Be("Basic Package");
            package.Price.Should().Be(500000m);
            package.SessionCount.Should().Be(10);
            package.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Role_CanBeCreatedWithName()
        {
            var role = new Role
            {
                RoleId = 1,
                RoleName = "admin"
            };

            role.RoleName.Should().Be("admin");
            role.RoleId.Should().Be(1);
        }

        [Fact]
        public void Curriculum_CanBeCreatedWithName()
        {
            var curriculum = new Curriculum
            {
                CurriculumId = Guid.NewGuid(),
                CurriculumName = "Math Grade 1",
                CurriculumCode = "MG1",
                Grades = "1"
            };

            curriculum.CurriculumName.Should().Be("Math Grade 1");
        }

        [Fact]
        public void Unit_CanBeCreatedWithOrder()
        {
            var unit = new Unit
            {
                UnitId = Guid.NewGuid(),
                UnitName = "Addition",
                UnitOrder = 1
            };

            unit.UnitName.Should().Be("Addition");
            unit.UnitOrder.Should().Be(1);
        }

        [Fact]
        public void DailyReport_CanBeCreatedWithDate()
        {
            var report = new DailyReport
            {
                ReportId = Guid.NewGuid(),
                CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                ChildId = Guid.NewGuid()
            };

            report.CreatedDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        }

        [Fact]
        public void School_CanBeCreatedWithName()
        {
            var school = new School
            {
                SchoolId = Guid.NewGuid(),
                SchoolName = "ABC Elementary"
            };

            school.SchoolName.Should().Be("ABC Elementary");
        }

        [Fact]
        public void Center_CanBeCreatedWithLocation()
        {
            var center = new Center
            {
                CenterId = Guid.NewGuid(),
                Name = "Downtown Center",
                Latitude = 10.762622,
                Longitude = 106.660172
            };

            center.Name.Should().Be("Downtown Center");
            center.Latitude.Should().Be(10.762622);
        }

        [Fact]
        public void TutorVerification_CanBeCreatedWithStatus()
        {
            var verification = new TutorVerification
            {
                VerificationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                VerificationStatus = "pending"
            };

            verification.VerificationStatus.Should().Be("pending");
        }

        [Fact]
        public void WalletTransaction_CanBeCreatedWithAmount()
        {
            var transaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = Guid.NewGuid(),
                Amount = 100000m,
                TransactionType = "deposit",
                Status = "completed"
            };

            transaction.Amount.Should().Be(100000m);
            transaction.TransactionType.Should().Be("deposit");
            transaction.Status.Should().Be("completed");
        }

    }
}
