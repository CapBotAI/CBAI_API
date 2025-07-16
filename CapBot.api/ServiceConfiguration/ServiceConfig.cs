using App.BLL.Implementations;
using App.BLL.Interfaces;
using App.BLL.Mapper;
using App.DAL.Implementations;
using App.DAL.Interfaces;
using App.DAL.UnitOfWork;

namespace CapBot.api.ServiceConfiguration;

public class ServiceConfig
{
    public static void Register(IServiceCollection services, IConfiguration configuration)
    {

        //register mapper
        services.AddAutoMapper(typeof(MapperProfile));

        //register repo
        services.AddScoped(typeof(IRepoBase<>), typeof(RepoBase<>));

        //register UOW
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        //register service
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHandbagService, HandbagService>();

        //SignalR Service
        services.AddSignalR();

        //Jwt Service
        services.AddScoped<IJwtService, JwtService>();

    }
}
