using App.Commons.Email.Implementations;
using App.Commons.Email.Interfaces;
using App.Commons.Email.Options;
using Microsoft.Extensions.DependencyInjection;

namespace App.Commons.Email.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailTemplateServices(
        this IServiceCollection services,
        Action<EmailTemplateOptions> configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<EmailTemplateOptions>(options => {});
        }

        services.AddScoped<IPathProvider, PathProvider>();
        
        return services;
    }
}