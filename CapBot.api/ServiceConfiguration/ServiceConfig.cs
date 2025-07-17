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
        services.AddScoped<IIdentityRepository, IdentityRepository>();

        //register UOW
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        //register service
        services.AddScoped<IAuthService, AuthService>();

        //SignalR Service
        services.AddSignalR();

        //Jwt Service
        services.AddScoped<IJwtService, JwtService>();

        //Semester Service
        services.AddScoped<ISemesterService, SemesterService>();

        //Topic Category Service
        services.AddScoped<ITopicCategoryService, TopicCategoryService>();

        //Data Seeder Service
        services.AddScoped<IDataSeederService, DataSeederService>();

    }
}
