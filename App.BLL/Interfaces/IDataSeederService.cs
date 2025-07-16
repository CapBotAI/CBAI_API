using System;

namespace App.BLL.Interfaces;

public interface IDataSeederService
{
    Task SeedDefaultDataAsync();
    Task SeedRolesAsync();
    Task SeedAdminUserAsync();
    Task SeedSampleUsersAsync();
}
