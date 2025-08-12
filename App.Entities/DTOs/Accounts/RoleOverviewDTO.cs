using System;
using App.Entities.Entities.Core;

namespace App.Entities.DTOs.Accounts;

public class RoleOverviewDTO
{
    public int Id { get; set; }
    public string RoleName { get; set; } = null!;

    public RoleOverviewDTO(Role role)
    {
        Id = role.Id;
        RoleName = role.Name!;
    }
}