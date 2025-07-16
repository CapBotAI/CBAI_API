using System;
using Microsoft.AspNetCore.Identity;

namespace App.Entities.Entities.Core;

public partial class Role : IdentityRole<int>
{
    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual ICollection<RoleClaim> RoleClaims { get; set; }
    public bool IsAdmin { get; set; }
}
