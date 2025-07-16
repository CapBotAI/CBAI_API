using System;
using App.Entities.Entities_2;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace CapBot.api.OData;

public static class EdmModelBuilder
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();

        builder.EntitySet<Handbag>("Handbags");
        builder.EntitySet<Brand>("Brands");

        var handbagType = builder.EntityType<Handbag>();
        handbagType.HasKey(h => h.HandbagID);
        handbagType.Property(h => h.ModelName);
        handbagType.Property(h => h.Material);
        handbagType.Property(h => h.Color);
        handbagType.Property(h => h.Price);
        handbagType.Property(h => h.Stock);
        handbagType.Property(h => h.ReleaseDate);
        handbagType.Property(h => h.BrandID);

        var brandType = builder.EntityType<Brand>();
        brandType.HasKey(b => b.BrandID);
        brandType.Property(b => b.BrandName);
        brandType.Property(b => b.Country);
        brandType.Property(b => b.FoundedYear);
        brandType.Property(b => b.Website);

        handbagType.HasRequired(h => h.Brand);
        brandType.HasMany(b => b.Handbags);

        return builder.GetEdmModel();
    }
}
