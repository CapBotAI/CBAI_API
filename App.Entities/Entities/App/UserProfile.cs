using System;
using App.Commons;

namespace App.Entities.Entities.App;

public class UserProfile : CommonDataModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
    public string? CoverImage { get; set; }
    public virtual Core.User? User { get; set; }
}