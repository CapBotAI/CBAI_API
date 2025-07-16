using System;
using System.Collections.Generic;

namespace App.Entities.Entities_2;

public partial class SystemAccount
{
    public int AccountID { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int? Role { get; set; }

    public bool? IsActive { get; set; }
}
