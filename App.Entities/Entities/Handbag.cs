using System;
using System.Collections.Generic;

namespace App.Entities.Entities_2;

public partial class Handbag
{
    public int HandbagID { get; set; }

    public int? BrandID { get; set; }

    public string ModelName { get; set; } = null!;

    public string? Material { get; set; }

    public string? Color { get; set; }

    public decimal? Price { get; set; }

    public int? Stock { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public virtual Brand? Brand { get; set; }
}
