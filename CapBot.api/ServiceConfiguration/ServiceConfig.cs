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
        services.AddScoped<IAccountService, AccountService>();

        //SignalR Service
        services.AddSignalR();

        //Jwt Service
        services.AddScoped<IJwtService, JwtService>();

        //Semester Service
        services.AddScoped<ISemesterService, SemesterService>();

        //Topic Category Service
        services.AddScoped<ITopicCategoryService, TopicCategoryService>();

        //Topic Service
        services.AddScoped<ITopicService, TopicService>();

        //Topic Version Service
        services.AddScoped<ITopicVersionService, TopicVersionService>();

        //Phase Type Service
        services.AddScoped<IPhaseTypeService, PhaseTypeService>();

        //Phase Service
        services.AddScoped<IPhaseService, PhaseService>();

        //Submission Service
        services.AddScoped<ISubmissionService, SubmissionService>();

        //File Service
        services.AddScoped<IFileService, FileService>();

        //Data Seeder Service
        services.AddScoped<IDataSeederService, DataSeederService>();

        services.AddScoped<IReviewerAssignmentService, ReviewerAssignmentService>();

        services.AddScoped<ISkillMatchingService, SkillMatchingService>();

        services.AddScoped<IPerformanceMatchingService, PerformanceMatchingService>();
        // EvaluationCriteria Service
        services.AddScoped<IEvaluationCriteriaService, EvaluationCriteriaService>();

        // Review Service
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ISubmissionReviewService, SubmissionReviewService>();
        services.AddScoped<IReviewCommentService, ReviewCommentService>();
        services.AddScoped<IElasticsearchService, ElasticsearchService>();

    }
}
