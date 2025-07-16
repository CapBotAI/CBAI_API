using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using App.DAL.Context;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using CapBot.api.Middlewares;
using CapBot.api.OData;
using CapBot.api.ServiceConfiguration;
using Microsoft.AspNetCore.OData;

namespace CapBot.api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        // ==== Add Serilog ======
        var hookApi = configuration.GetValue<string>("Serilog:HookAPI");
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Hand Bag Summer 2025", "Hand Bag Summer 2025 Logger")
            .WriteTo.Http(
                hookApi,
                batchFormatter: new Serilog.Sinks.Http.BatchFormatters.ArrayBatchFormatter(),
                queueLimitBytes: null,
                httpClient: new CustomHttpClient(configuration))
            .CreateLogger();
        builder.Host.UseSerilog();

        // Add services to the container.
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        }).AddOData(options =>
        {
            options.Select().Filter().OrderBy().Expand().SetMaxTop(null).Count();
            options.AddRouteComponents("odata", EdmModelBuilder.GetEdmModel());
        });

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Cap Bot Capstone API", Version = "v1" });
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            });

        //<=====Set up policy=====>
        builder.Services.AddCors(opts =>
        {
            opts.AddPolicy("corspolicy",
                build => { build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader(); });
        });

        //<=====Add Database=====>
        var connectionString = builder.Configuration.GetConnectionString("AppDb");
        builder.Services.AddDbContext<MyDbContext>(opts => opts.UseSqlServer(connectionString,
            options => { options.MigrationsAssembly("App.DAL"); }));

        //<=====Add Session=====>
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddDistributedMemoryCache();


        //<=====Add JWT Authentication=====>
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidAudience = configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"])),
                ClockSkew = TimeSpan.Zero
            };
        });


        //<=====Add Authorization=====>
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireRole("Admin"));
        });

        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "X-CSRF-TOKEN";
            options.HeaderName = "X-CSRF-TOKEN";
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        //<=====Add SignalR=====>
        builder.Services.AddSignalR();


        //<=====Register Service=====>
        ServiceConfig.Register(builder.Services, builder.Configuration);

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                c.RoutePrefix = "";
            });
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors("corspolicy");
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
