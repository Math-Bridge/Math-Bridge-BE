using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public int UserCount { get; set; }
    }

    public class CreateRoleRequest
    {
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
    }
}