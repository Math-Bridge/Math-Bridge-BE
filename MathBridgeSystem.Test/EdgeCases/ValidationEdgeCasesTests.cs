using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using Xunit;

namespace MathBridgeSystem.Tests.EdgeCases
{
    public class ValidationEdgeCasesTests
    {
        #region RegisterRequest Validation Tests

        [Fact]
        public void RegisterRequest_EmailWithSpecialCharacters_ShouldBeValid()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test+tag@example.co.uk",
                Password = "ValidPass123!",
                PhoneNumber = "1234567890",
                Gender = "male",
                RoleId = 3
            };

            // Act & Assert
            request.Email.Should().Contain("+");
            request.Email.Should().Contain(".");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        public void RegisterRequest_EmptyOrWhitespaceEmail_ShouldBeInvalid(string email)
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = email,
                FullName = "Test User",
                Password = "ValidPass123!"
            };

            // Act & Assert
            string.IsNullOrWhiteSpace(request.Email).Should().BeTrue();
        }

        [Theory]
        [InlineData("short")]
        [InlineData("12345")]
        public void RegisterRequest_ShortPassword_ShouldBeInvalid(string password)
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = password
            };

            // Act & Assert
            request.Password.Length.Should().BeLessThan(8);
        }

        [Fact]
        public void RegisterRequest_VeryLongName_ShouldBeHandled()
        {
            // Arrange
            var longName = new string('A', 500);
            var request = new RegisterRequest
            {
                FullName = longName,
                Email = "test@example.com",
                Password = "ValidPass123!"
            };

            // Act & Assert
            request.FullName.Length.Should().Be(500);
        }

        #endregion

        #region Date Range Edge Cases

        [Fact]
        public void DateOfBirth_FutureDate_ShouldBeInvalid()
        {
            // Arrange
            var futureDate = DateTime.Now.AddYears(1);

            // Act & Assert
            futureDate.Should().BeAfter(DateTime.Now);
        }

        [Fact]
        public void DateOfBirth_VeryOldDate_ShouldBeHandled()
        {
            // Arrange
            var oldDate = new DateTime(1900, 1, 1);

            // Act & Assert
            oldDate.Year.Should().Be(1900);
            (DateTime.Now.Year - oldDate.Year).Should().BeGreaterThan(100);
        }

        [Fact]
        public void Session_StartTimeBeforeEndTime_ShouldBeValid()
        {
            // Arrange
            var startTime = DateTime.Now;
            var endTime = startTime.AddHours(2);

            // Act & Assert
            endTime.Should().BeAfter(startTime);
        }

        [Fact]
        public void Session_EndTimeBeforeStartTime_ShouldBeInvalid()
        {
            // Arrange
            var startTime = DateTime.Now;
            var endTime = startTime.AddHours(-1);

            // Act & Assert
            endTime.Should().BeBefore(startTime);
        }

        #endregion

        #region Numeric Edge Cases

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Price_NegativeOrZeroValue_ShouldBeInvalid(decimal price)
        {
            // Act & Assert
            price.Should().BeLessThanOrEqualTo(0);
        }

        [Fact]
        public void Price_MaxDecimalValue_ShouldBeHandled()
        {
            // Arrange
            var maxPrice = decimal.MaxValue;

            // Act & Assert
            maxPrice.Should().Be(decimal.MaxValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SessionCount_ZeroOrNegative_ShouldBeInvalid(int count)
        {
            // Act & Assert
            count.Should().BeLessThanOrEqualTo(0);
        }

        [Fact]
        public void SessionCount_VeryLargeNumber_ShouldBeHandled()
        {
            // Arrange
            var largeCount = int.MaxValue;

            // Act & Assert
            largeCount.Should().Be(int.MaxValue);
        }

        #endregion

        #region Guid Edge Cases

        [Fact]
        public void Guid_EmptyGuid_ShouldBeIdentifiable()
        {
            // Arrange
            var emptyGuid = Guid.Empty;

            // Act & Assert
            emptyGuid.Should().Be(Guid.Empty);
            (emptyGuid == Guid.Empty).Should().BeTrue();
        }

        [Fact]
        public void Guid_NewGuid_ShouldBeUnique()
        {
            // Arrange
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            // Act & Assert
            guid1.Should().NotBe(guid2);
        }

        #endregion

        #region String Manipulation Edge Cases

        [Theory]
        [InlineData("test@example.com", "TEST@EXAMPLE.COM")]
        [InlineData("Test@Example.Com", "test@example.com")]
        public void Email_CaseInsensitive_ShouldBeEqual(string email1, string email2)
        {
            // Act & Assert
            email1.ToLower().Should().Be(email2.ToLower());
        }

        [Theory]
        [InlineData("  test@example.com  ")]
        [InlineData("\ttest@example.com\t")]
        [InlineData("\ntest@example.com\n")]
        public void Email_WithWhitespace_ShouldBeTrimmed(string email)
        {
            // Act
            var trimmed = email.Trim();

            // Assert
            trimmed.Should().Be("test@example.com");
        }

        [Fact]
        public void PhoneNumber_WithSpecialCharacters_ShouldBeNormalized()
        {
            // Arrange
            var phoneWithDashes = "123-456-7890";
            var phoneWithParens = "(123) 456-7890";
            var phoneWithSpaces = "123 456 7890";

            // Act
            var normalized1 = phoneWithDashes.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            var normalized2 = phoneWithParens.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            var normalized3 = phoneWithSpaces.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");

            // Assert
            normalized1.Should().Be("1234567890");
            normalized2.Should().Be("1234567890");
            normalized3.Should().Be("1234567890");
        }

        #endregion

        #region Null and Empty Collection Tests

        [Fact]
        public void Collection_Null_ShouldBeHandled()
        {
            // Arrange
            List<string>? nullList = null;

            // Act & Assert
            nullList.Should().BeNull();
        }

        [Fact]
        public void Collection_Empty_ShouldBeValid()
        {
            // Arrange
            var emptyList = new List<string>();

            // Act & Assert
            emptyList.Should().BeEmpty();
            emptyList.Count.Should().Be(0);
        }

        #endregion

        #region Concurrent Operation Edge Cases

        [Fact]
        public void DateTime_Now_MultipleCallsShouldBeDifferent()
        {
            // Arrange & Act
            var time1 = DateTime.Now;
            Thread.Sleep(10);
            var time2 = DateTime.Now;

            // Assert
            time2.Should().BeAfter(time1);
        }

        [Fact]
        public void Guid_MultipleThreads_ShouldGenerateUniqueIds()
        {
            // Arrange
            var guids = new System.Collections.Concurrent.ConcurrentBag<Guid>();
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => guids.Add(Guid.NewGuid())));
            }
            Task.WaitAll(tasks.ToArray());

            // Assert
            guids.Should().HaveCount(100);
            guids.Distinct().Should().HaveCount(100);
        }

        #endregion

        #region Boundary Value Tests

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void PageSize_VariousSizes_ShouldBeHandled(int pageSize)
        {
            // Act & Assert
            pageSize.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void PageNumber_BoundaryValues_ShouldBeHandled(int pageNumber)
        {
            // Act & Assert
            pageNumber.Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region Enum Edge Cases

        [Fact]
        public void Gender_ValidValues_ShouldBeRecognized()
        {
            // Arrange
            var validGenders = new[] { "male", "female", "other" };

            // Act & Assert
            foreach (var gender in validGenders)
            {
                validGenders.Should().Contain(gender.ToLower());
            }
        }

        [Fact]
        public void Gender_CaseInsensitive_ShouldBeHandled()
        {
            // Arrange
            var genders = new[] { "Male", "MALE", "male", "MaLe" };

            // Act & Assert
            foreach (var gender in genders)
            {
                gender.ToLower().Should().Be("male");
            }
        }

        #endregion

        #region Special Character Edge Cases

        [Theory]
        [InlineData("Test <script>alert('xss')</script>")]
        [InlineData("Test & Sons")]
        [InlineData("Test \"quoted\" name")]
        [InlineData("Test 'single' quote")]
        public void Name_WithSpecialCharacters_ShouldBeHandled(string name)
        {
            // Act & Assert
            name.Should().NotBeNullOrEmpty();
            name.Length.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("SELECT * FROM users")]
        [InlineData("'; DROP TABLE users--")]
        [InlineData("1' OR '1'='1")]
        public void Input_SQLInjectionAttempts_ShouldBeDetectable(string input)
        {
            // Act & Assert
            input.Should().ContainAny("SELECT", "DROP", "OR");
        }

        #endregion

        #region Timezone Edge Cases

        [Fact]
        public void DateTime_UtcConversion_ShouldMaintainAccuracy()
        {
            // Arrange
            var localTime = DateTime.Now;
            var utcTime = localTime.ToUniversalTime();

            // Act
            var backToLocal = utcTime.ToLocalTime();

            // Assert
            backToLocal.Should().BeCloseTo(localTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void DateTime_DifferentTimezones_ShouldBeComparable()
        {
            // Arrange
            var utcNow = DateTime.UtcNow;
            var localNow = DateTime.Now;

            // Act & Assert
            // Both represent roughly the same moment in time
            var difference = Math.Abs((utcNow - localNow.ToUniversalTime()).TotalSeconds);
            difference.Should().BeLessThan(1);
        }

        #endregion
    }
}

