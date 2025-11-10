namespace MathBridgeSystem.Application.DTOs
{
    public class UserResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public decimal WalletBalance { get; set; }
        public int RoleId { get; set; }
        public string Status { get; set; }
        public string FormattedAddress { get; set; }
        public string placeId { get; set; }
    }
}