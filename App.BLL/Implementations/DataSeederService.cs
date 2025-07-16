using System;
using App.BLL.Interfaces;
using App.Entities.Entities.Core;
using App.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class DataSeederService : IDataSeederService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeederService> _logger;

    public DataSeederService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration configuration,
        ILogger<DataSeederService> logger
        )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedDefaultDataAsync()
    {
        try
        {
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedSampleUsersAsync();

            _logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during data seeding");
            throw;
        }
    }

    public async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new { Name = "Administrator", NormalizedName = "ADMINISTRATOR", IsAdmin = true, ConcurrencyStamp = Guid.NewGuid().ToString() },
            new { Name = "Moderator", NormalizedName = "MODERATOR", IsAdmin = false, ConcurrencyStamp = Guid.NewGuid().ToString() },
            new { Name = "Supervisor", NormalizedName = "SUPERVISOR", IsAdmin = false, ConcurrencyStamp = Guid.NewGuid().ToString() },
            new { Name = "Reviewer", NormalizedName = "REVIEWER", IsAdmin = false, ConcurrencyStamp = Guid.NewGuid().ToString() }
        };

        foreach (var roleInfo in roles)
        {
            var existingRole = await _roleManager.FindByNameAsync(roleInfo.Name);
            if (existingRole == null)
            {
                var role = new Role
                {
                    Name = roleInfo.Name,
                    IsAdmin = roleInfo.IsAdmin,
                    ConcurrencyStamp = roleInfo.ConcurrencyStamp
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Role '{roleInfo.Name}' created successfully");
                }
                else
                {
                    _logger.LogError($"Failed to create role '{roleInfo.Name}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    public async Task SeedAdminUserAsync()
    {
        try
        {
            var email = _configuration["AdminAccount:Email"];
            var password = _configuration["AdminAccount:Password"];
            var userName = _configuration["AdminAccount:UserName"];
            var phoneNumber = _configuration["AdminAccount:PhoneNumber"];

            var existingAdmin = await _userManager.FindByEmailAsync(email);
            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    UserName = userName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false,
                    CreatedAt = DateTime.Now,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                var result = await _userManager.CreateAsync(adminUser, password);
                if (result.Succeeded)
                {
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(adminUser, SystemRoles.Administrator.ToString());

                    // Add admin claims
                    var adminClaims = new[]
                    {
                        new { Type = "permission", Value = "admin.full_access" },
                    };

                    foreach (var claim in adminClaims)
                    {
                        await _userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim(claim.Type, claim.Value));
                    }

                    _logger.LogInformation($"Admin user '{email}' created successfully");
                }
                else
                {
                    _logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                _logger.LogInformation($"Admin user '{email}' already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user");
            throw;
        }
    }

    public async Task SeedSampleUsersAsync()
    {
        // Sample moderator user
        var moderatorEmail = "moderator@example.com";
        var existingModerator = await _userManager.FindByEmailAsync(moderatorEmail);

        if (existingModerator == null)
        {
            var moderatorUser = new User
            {
                UserName = moderatorEmail,
                Email = moderatorEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(moderatorUser, "Moderator@123456");
            if (result.Succeeded)
            {
                // Assign Moderator role
                await _userManager.AddToRoleAsync(moderatorUser, "Moderator");

                // Add moderator claims
                var moderatorClaims = new[]
                {
                    new { Type = "permission", Value = "moderator.full_access" },
                };

                foreach (var claim in moderatorClaims)
                {
                    await _userManager.AddClaimAsync(moderatorUser, new System.Security.Claims.Claim(claim.Type, claim.Value));
                }

                _logger.LogInformation($"Sample moderator '{moderatorEmail}' created successfully");
            }
        }
        else
        {
            _logger.LogInformation($"Sample moderator '{moderatorEmail}' already exists");
        }

        // Sample supervisor user
        var supervisorEmail = "supervisor@example.com";
        var existingSupervisor = await _userManager.FindByEmailAsync(supervisorEmail);

        if (existingSupervisor == null)
        {
            var supervisorUser = new User
            {
                UserName = supervisorEmail,
                Email = supervisorEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(supervisorUser, "Supervisor@123456");
            if (result.Succeeded)
            {
                // Assign Supervisor role
                await _userManager.AddToRoleAsync(supervisorUser, "Supervisor");

                // Add supervisor claims
                var supervisorClaims = new[]
                {
                    new { Type = "permission", Value = "news.view" },
                    new { Type = "permission", Value = "news.execute" },
                    new { Type = "permission", Value = "profile.manage" }
                };

                foreach (var claim in supervisorClaims)
                {
                    await _userManager.AddClaimAsync(supervisorUser, new System.Security.Claims.Claim(claim.Type, claim.Value));
                }

                _logger.LogInformation($"Sample supervisor '{supervisorEmail}' created successfully");
            }
        }

        // Sample reviewer user
        var reviewerEmail = "reviewer@example.com";
        var existingReviewer = await _userManager.FindByEmailAsync(reviewerEmail);

        if (existingReviewer == null)
        {
            var reviewerUser = new User
            {
                UserName = reviewerEmail,
                Email = reviewerEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(reviewerUser, "Reviewer@123456");
            if (result.Succeeded)
            {
                // Assign Reviewer role
                await _userManager.AddToRoleAsync(reviewerUser, "Reviewer");

                // Add reviewer claims
                var reviewerClaims = new[]
                {
                    new { Type = "permission", Value = "news.view" },
                    new { Type = "permission", Value = "news.execute" },
                    new { Type = "permission", Value = "profile.manage" }
                };

                foreach (var claim in reviewerClaims)
                {
                    await _userManager.AddClaimAsync(reviewerUser, new System.Security.Claims.Claim(claim.Type, claim.Value));
                }

                _logger.LogInformation($"Sample reviewer '{reviewerEmail}' created successfully");
            }
        }
    }
}
