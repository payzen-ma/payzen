using System;
using System.Collections.Generic;

namespace payzen_backend.Models.Permissions.Dtos
{
    public class RoleUsersDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<UserInRoleDto> Users { get; set; } = new();
    }

    public class UserInRoleDto
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }

        // Employee related info (nullable si user sans employee)
        public int? EmployeeId { get; set; }
        public string? EmployeeFirstName { get; set; }
        public string? EmployeeLastName { get; set; }

        // Company related info (nullable)
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }

        // Optionnel : date d'assignation du rôle
        public DateTimeOffset? AssignedAt { get; set; }
    }
}