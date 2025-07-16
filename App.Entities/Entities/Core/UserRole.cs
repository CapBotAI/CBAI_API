using Microsoft.AspNetCore.Identity;

namespace App.Entities.Entities.Core;

public partial class RoleClaim : IdentityRoleClaim<int>
{
    public virtual Role? Role { get; set; }
}

public partial class UserClaim : IdentityUserClaim<int>
{
    public virtual User? User { get; set; }
}

public partial class UserLogin : IdentityUserLogin<int>
{
    public virtual User? User { get; set; }
}

public partial class UserRole : IdentityUserRole<int>
{
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}

public partial class UserToken : IdentityUserToken<int>
{
    public virtual User? User { get; set; }
}
