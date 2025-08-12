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
using App.BLL.Interfaces;
using Microsoft.AspNetCore.Identity;
using App.Entities.Entities.Core;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Mvc;
using App.BLL.Implementations;

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

        // Add services to the container
        

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }).AddOData(options =>
        {
            options.Select().Filter().OrderBy().Expand().SetMaxTop(null).Count();
            options.AddRouteComponents("odata", EdmModelBuilder.GetEdmModel());
        });

        // Hoặc configure ApiBehaviorOptions để custom response
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true; // Disable auto validation

            // Hoặc custom response nếu muốn giữ auto validation
            // options.InvalidModelStateResponseFactory = actionContext =>
            // {
            //     var errors = actionContext.ModelState
            //         .Where(m => m.Value.Errors.Count > 0)
            //         .ToDictionary(
            //             kvp => kvp.Key.ToCamelCase(),
            //             kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).First());
            //
            //     var response = new FSResponse
            //     {
            //         Errors = errors,
            //         StatusCode = System.Net.HttpStatusCode.UnprocessableEntity,
            //         Message = ConstantModel.ModelInvalid
            //     };
            //
            //     return new ObjectResult(response)
            //     {
            //         StatusCode = StatusCodes.Status422UnprocessableEntity
            //     };
            // };
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

        //<=====Add Identity Services=====>
        builder.Services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequiredUniqueChars = 0;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<MyDbContext>()
        .AddDefaultTokenProviders();

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

        // ===== CẤU HÌNH SWAGGER =====
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Cap Bot Capstone API",
                Description = "API Documentation for Cap Bot Capstone System",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "dev@capbot.com"
                }
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            // Bật annotations nếu có sử dụng
            c.EnableAnnotations();
        });

        var app = builder.Build();

        //<=====Seed Base data system=====>
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var dataSeederService = services.GetRequiredService<IDataSeederService>();
                dataSeederService.SeedDefaultDataAsync().Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error seeding default data");
            }
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = false;
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cap Bot Capstone API v1");
                c.RoutePrefix = string.Empty;
                c.DisplayRequestDuration();
                c.DocExpansion(DocExpansion.None);
            });
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors("corspolicy");
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers();

        app.Run();
    }
}
