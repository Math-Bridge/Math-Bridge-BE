using CloudinaryDotNet;
using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
using Moq;
using Org.BouncyCastle.Ocsp;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IWalletTransactionRepository> _walletTransactionRepositoryMock;
        private readonly UserService _userService;
        private readonly Mock<ICloudinaryService> _cloudinary;
        private readonly Mock<INotificationService> _notificationService;

        private readonly Guid _adminId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _otherUserId = Guid.NewGuid();
        private readonly string _adminRole = "admin";
        private readonly string _parentRole = "parent";

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _walletTransactionRepositoryMock = new Mock<IWalletTransactionRepository>();
            _cloudinary= new Mock<ICloudinaryService>();
            _notificationService = new Mock<INotificationService>();

            _userService = new UserService(
                _userRepositoryMock.Object,
                _walletTransactionRepositoryMock.Object,
                _cloudinary.Object,
                _notificationService.Object
            );
        }

        #region GetUserByIdAsync Tests

        // Test: Lấy user thành công (Admin lấy)
        [Fact]
        public async Task GetUserByIdAsync_AsAdmin_ReturnsUser()
        {
            // Arrange
            var user = new User { UserId = _userId, FullName = "Test User" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(_userId, _adminId, _adminRole);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be("Test User");
        }

        // Test: Lấy user thành công (Tự lấy)
        [Fact]
        public async Task GetUserByIdAsync_AsSelf_ReturnsUser()
        {
            var user = new User { UserId = _userId, FullName = "Test User" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

            var result = await _userService.GetUserByIdAsync(_userId, _userId, _parentRole);

            result.Should().NotBeNull();
        }

        // Test: Ném lỗi khi không được phép
        [Fact]
        public async Task GetUserByIdAsync_Unauthorized_ThrowsException()
        {
            Func<Task> act = () => _userService.GetUserByIdAsync(_userId, _otherUserId, _parentRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Unauthorized access");
        }

        // Test: Ném lỗi khi không tìm thấy user
        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ThrowsException()
        {
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync((User)null);
            Func<Task> act = () => _userService.GetUserByIdAsync(_userId, _adminId, _adminRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        }

        #endregion

        #region UpdateUserAsync Tests

        // Test: Cập nhật user thành công (Admin)
        [Fact]
        public async Task UpdateUserAsync_AsAdmin_UpdatesUser()
        {
            var user = new User { UserId = _userId };
            var request = new UpdateUserRequest { FullName = "New Name" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

            await _userService.UpdateUserAsync(_userId, request, _adminId, _adminRole);

            user.FullName.Should().Be("New Name");
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        // Test: Ném lỗi khi không được phép
        [Fact]
        public async Task UpdateUserAsync_Unauthorized_ThrowsException()
        {
            var request = new UpdateUserRequest();
            Func<Task> act = () => _userService.UpdateUserAsync(_userId, request, _otherUserId, _parentRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Unauthorized access");
        }

        // Test: Ném lỗi khi không tìm thấy user
        [Fact]
        public async Task UpdateUserAsync_UserNotFound_ThrowsException()
        {
            var request = new UpdateUserRequest();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync((User)null);
            Func<Task> act = () => _userService.UpdateUserAsync(_userId, request, _adminId, _adminRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        }

        // Test: Ném lỗi khi request là null
        [Fact]
        public async Task UpdateUserAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _userService.UpdateUserAsync(_userId, null, _adminId, _adminRole);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        #endregion

        #region GetWalletAsync Tests

        // Test: Lấy ví thành công (Admin)
        [Fact]
        public async Task GetWalletAsync_AsAdmin_ReturnsWallet()
        {
            // Arrange
            var parent = new User { UserId = _userId, Role = new Role { RoleName = "parent" }, WalletBalance = 100 };
            var transactions = new List<WalletTransaction> { new WalletTransaction { Amount = 100 } };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(parent);
            _walletTransactionRepositoryMock.Setup(r => r.GetByParentIdAsync(_userId)).ReturnsAsync(transactions);

            // Act
            var result = await _userService.GetWalletAsync(_userId, _adminId, _adminRole);

            // Assert
            result.WalletBalance.Should().Be(100);
            result.Transactions.Should().HaveCount(1);
        }

        // Test: Ném lỗi khi không được phép
        [Fact]
        public async Task GetWalletAsync_Unauthorized_ThrowsException()
        {
            Func<Task> act = () => _userService.GetWalletAsync(_userId, _otherUserId, _parentRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Unauthorized access");
        }

        // Test: Ném lỗi khi user không phải "parent"
        [Fact]
        public async Task GetWalletAsync_UserNotParent_ThrowsException()
        {
            var user = new User { UserId = _userId, Role = new Role { RoleName = "tutor" } };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

            Func<Task> act = () => _userService.GetWalletAsync(_userId, _adminId, _adminRole);

            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid parent user");
        }

        #endregion

        #region AdminCreateUserAsync Tests

        // Test: Admin tạo user thành công
        [Fact]
        public async Task AdminCreateUserAsync_ValidRequest_CreatesUser()
        {
            // Arrange
            var request = new RegisterRequest { Email = "new@test.com", Password = "Pass123!", RoleId = 2 };
            _userRepositoryMock.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.RoleExistsAsync(request.RoleId)).ReturnsAsync(true);

            // Act
            var result = await _userService.AdminCreateUserAsync(request, _adminRole);

            // Assert
            result.Should().NotBe(Guid.Empty);
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == request.Email && u.RoleId == 2)), Times.Once);
        }

        // Test: Ném lỗi khi không phải Admin
        [Fact]
        public async Task AdminCreateUserAsync_NotAdmin_ThrowsException()
        {
            var request = new RegisterRequest();
            Func<Task> act = () => _userService.AdminCreateUserAsync(request, _parentRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Only admins can create users");
        }

        // Test: Ném lỗi khi Email đã tồn tại
        [Fact]
        public async Task AdminCreateUserAsync_EmailExists_ThrowsException()
        {
            var request = new RegisterRequest { Email = "exists@test.com" };
            _userRepositoryMock.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(true);
            Func<Task> act = () => _userService.AdminCreateUserAsync(request, _adminRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Email already exists");
        }

        // Test: Ném lỗi khi Role không tồn tại
        [Fact]
        public async Task AdminCreateUserAsync_RoleNotExists_ThrowsException()
        {
            var request = new RegisterRequest { Email = "new@test.com", RoleId = 99 };
            _userRepositoryMock.Setup(r => r.EmailExistsAsync(request.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.RoleExistsAsync(request.RoleId)).ReturnsAsync(false);
            Func<Task> act = () => _userService.AdminCreateUserAsync(request, _adminRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid RoleId: 99");
        }

        #endregion

        #region UpdateUserStatusAsync Tests

        // Test: Admin cập nhật status thành công
        [Fact]
        public async Task UpdateUserStatusAsync_ValidRequest_UpdatesStatus()
        {
            var user = new User { UserId = _userId, Status = "active" };
            var request = new UpdateStatusRequest { Status = "banned" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

            await _userService.UpdateUserStatusAsync(_userId, request, _adminRole);

            user.Status.Should().Be("banned");
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        // Test: Ném lỗi khi không phải Admin
        [Fact]
        public async Task UpdateUserStatusAsync_NotAdmin_ThrowsException()
        {
            var request = new UpdateStatusRequest();
            Func<Task> act = () => _userService.UpdateUserStatusAsync(_userId, request, _parentRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("Only admins can update user status");
        }

        // Test: Ném lỗi khi không tìm thấy User
        [Fact]
        public async Task UpdateUserStatusAsync_UserNotFound_ThrowsException()
        {
            var request = new UpdateStatusRequest();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync((User)null);
            Func<Task> act = () => _userService.UpdateUserStatusAsync(_userId, request, _adminRole);
            await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        }

        #endregion

        #region DeductWalletAsync Tests

        // Test: Trừ tiền ví thành công
        [Fact]
        public async Task DeductWalletAsync_ValidRequest_DeductsBalanceAndCreatesTransaction()
        {
            // Arrange
            var parent = new User { UserId = _userId, Role = new Role { RoleName = "parent" }, WalletBalance = 2000 };
            var contractId = Guid.NewGuid();
            var package = new PaymentPackage { PackageName = "Gói 1", Price = 1500 };
            var contract = new Contract { ContractId = contractId, ParentId = _userId, Package = package };
            var request = new DeductWalletRequest { ContractId = new Guid() };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(parent);
            _userRepositoryMock.Setup(r => r.GetContractWithPackageAsync(contractId)).ReturnsAsync(contract);

            // Act
            var result = await _userService.DeductWalletAsync(_userId, request.ContractId, _adminId, _adminRole, package.Price);

            // Assert
            result.AmountDeducted.Should().Be(1500);
            result.NewWalletBalance.Should().Be(500); 
            result.TransactionStatus.Should().Be("completed");

            parent.WalletBalance.Should().Be(500);

            _walletTransactionRepositoryMock.Verify(r => r.AddAsync(It.Is<WalletTransaction>(t => t.Amount == 1500 && t.TransactionType == "withdrawal")), Times.Once);
            _userRepositoryMock.Verify(r => r.UpdateAsync(parent), Times.Once);
        }
        

        // Test: Ném lỗi khi không tìm thấy Contract
        [Fact]
        public async Task DeductWalletAsync_ContractNotFound_ThrowsException()
        {
            var parent = new User { UserId = _userId, Role = new Role { RoleName = "parent" } };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(parent);
            _userRepositoryMock.Setup(r => r.GetContractWithPackageAsync(It.IsAny<Guid>())).ReturnsAsync((Contract)null);

            Func<Task> act = () => _userService.DeductWalletAsync(_userId, new Guid(), _adminId, _adminRole, 500);

            await act.Should().ThrowAsync<Exception>().WithMessage("Contract not found");
        }

        // Test: Ném lỗi khi Contract không thuộc về Parent
        [Fact]
        public async Task DeductWalletAsync_ContractNotOwnedByParent_ThrowsException()
        {
            var parent = new User { UserId = _userId, Role = new Role { RoleName = "parent" } };
            var request = new DeductWalletRequest { ContractId = new Guid() };
            var contract = new Contract { ParentId = _otherUserId, Package = new PaymentPackage() }; 
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(parent);
            _userRepositoryMock.Setup(r => r.GetContractWithPackageAsync(It.IsAny<Guid>())).ReturnsAsync(contract);

            Func<Task> act = () => _userService.DeductWalletAsync(_userId, request.ContractId, _adminId, _adminRole,500);

            await act.Should().ThrowAsync<Exception>().WithMessage("Contract does not belong to this parent");
        }

        // Test: Ném lỗi khi không đủ tiền trong ví
        [Fact]
        public async Task DeductWalletAsync_InsufficientBalance_ThrowsException()
        {
            var parent = new User { UserId = _userId, Role = new Role { RoleName = "parent" }, WalletBalance = 1000 };
            var contract = new Contract { ParentId = _userId, Package = new PaymentPackage { Price = 1500 } }; 
            _userRepositoryMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(parent);
            _userRepositoryMock.Setup(r => r.GetContractWithPackageAsync(It.IsAny<Guid>())).ReturnsAsync(contract);

            Func<Task> act = () => _userService.DeductWalletAsync(_userId, new Guid(), _adminId, _adminRole,500);

            await act.Should().ThrowAsync<Exception>().WithMessage("*Insufficient wallet balance*");
        }

        #endregion
    }
}