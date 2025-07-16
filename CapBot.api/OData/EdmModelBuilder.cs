using System;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace CapBot.api.OData;

public static class EdmModelBuilder
{
    public static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();

        return builder.GetEdmModel();
    }
}
