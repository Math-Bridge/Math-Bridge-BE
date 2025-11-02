using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; }
        public int RoleId { get; set; }
    }
}
