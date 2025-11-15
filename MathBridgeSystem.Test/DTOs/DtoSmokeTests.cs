using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.Contract;
using MathBridgeSystem.Application.DTOs.NotificationLog;
using MathBridgeSystem.Application.DTOs.NotificationTemplate;
using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.DTOs.SePay;

namespace MathBridgeSystem.Test.DTOs
{
    public class DtoSmokeTests
    {
        [Fact]
        public void AssignSupportRequestRequest_CanSetProperties()
        {
            var dto = new AssignSupportRequestRequest { AssignedToUserId = Guid.NewGuid() };
            dto.AssignedToUserId.Should().NotBeEmpty();
        }

        [Fact]
        public void AvailableSubTutorsDto_CanSet()
        {
            var dto = new AvailableSubTutorsDto { TotalAvailable = 2, AvailableTutors = new System.Collections.Generic.List<SubTutorInfoDto>() };
            dto.TotalAvailable.Should().Be(2);
        }

        [Fact]
        public void ContractAvailableTutorResponse_CanSet()
        {
            var dto = new AvailableTutorResponse { UserId = Guid.NewGuid(), FullName = "A" };
            dto.FullName.Should().Be("A");
        }

        [Fact]
        public void GetAvailableTutorsRequest_CanSet()
        {
            var dto = new GetAvailableTutorsRequest { ContractId = 123 };
            dto.ContractId.Should().Be(123);
        }

        [Fact]
        public void CreateMathProgramRequest_CanSet()
        {
            var dto = new CreateMathProgramRequest { ProgramName = "P", Description = "D" };
            dto.ProgramName.Should().Be("P");
        }

        [Fact]
        public void NotificationLog_Dtos_CanInstantiate()
        {
            var create = new CreateNotificationLogRequest();
            var search = new NotificationLogSearchRequest();
            var log = new NotificationLogDto();
            create.Should().NotBeNull();
            search.Should().NotBeNull();
            log.Should().NotBeNull();
        }

        [Fact]
        public void NotificationTemplate_Dtos_CanInstantiate()
        {
            var create = new CreateNotificationTemplateRequest();
            var dto = new NotificationTemplateDto();
            var update = new UpdateNotificationTemplateRequest();
            create.Should().NotBeNull();
            dto.Should().NotBeNull();
            update.Should().NotBeNull();
        }

        [Fact]
        public void Statistics_Revenue_CanInstantiate()
        {
            var list = new RevenueByPackageListDto();
            var item = new RevenueByPackageDto();
            list.Should().NotBeNull();
            item.Should().NotBeNull();
        }

        [Fact]
        public void SupportRequestDtos_CanInstantiate()
        {
            var dto = new SupportRequestDto();
            dto.Should().NotBeNull();
        }

        [Fact]
        public void UpdateMathProgramRequest_CanInstantiate()
        {
            var dto = new UpdateMathProgramRequest();
            dto.Should().NotBeNull();
        }

        [Fact]
        public void UpdateSessionDtos_CanSet()
        {
            var s1 = new UpdateSessionStatusRequest { Status = "completed" };
            var s2 = new UpdateSessionTutorRequest { NewTutorId = Guid.NewGuid() };
            s1.Status.Should().Be("completed");
            s2.NewTutorId.Should().NotBeEmpty();
        }

        [Fact]
        public void UpdateSupportRequests_CanSet()
        {
            var r1 = new UpdateSupportRequestRequest { Subject = "subj", Description = "desc", Category = "cat" };
            var r2 = new UpdateSupportRequestStatusRequest { Status = "closed" };
            r2.Status.Should().Be("closed");
            r1.Subject.Should().Be("subj");
        }

        [Fact]
        public void ValidateLocationRequest_CanSet()
        {
            var dto = new ValidateLocationRequest { ChildId = Guid.NewGuid(), Latitude = 1.23m, Longitude = 4.56m, MaxDistanceKm = 10 };
            dto.Latitude.Should().Be(1.23m);
        }

        [Fact]
        public void VerifyRequest_CanSet()
        {
            var dto = new VerifyRequest { Email = "test@test.com", Code = "123456" };
            dto.Code.Should().Be("123456");
        }

        [Fact]
        public void MeetingDetailsResult_CanSet()
        {
            var dto = new MathBridgeSystem.Application.DTOs.VideoConference.MeetingDetailsResult { MeetingId = "id", MeetingUri = "uri://test" };
            dto.MeetingUri.Should().Be("uri://test");
        }

        [Fact]
        public void CustomDateTimeConverter_Serializes()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new CustomDateTimeConverter());
            var now = new DateTime(2024,1,2,3,4,5,DateTimeKind.Utc);
            var payload = new { at = now };
            var json = JsonSerializer.Serialize(payload, options);
            json.Should().Contain("2024-");
        }
    }
}
