using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Repositories.Context;
using ShuttleMate.Services;
using ShuttleMate.Services.Services;
using System.Reflection;
using System.Text;
using Wanvi.Repositories.SeedData;
using static ShuttleMate.Services.Services.HistoryTicketService;

namespace ShuttleMate.API
{
    public static class DependencyInjection
    {
        public static void AddConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigRoute();
            services.AddMemoryCache();
            services.AddInfrastructure(configuration);
            services.AddHangfireConfig(configuration);
            services.AddEmailConfig(configuration);
            services.AddAutoMapper();
            services.ConfigSwagger();
            services.AddAuthenJwt(configuration);
            //services.AddGoogleAuthentication(configuration);
            //services.AddFacebookAuthentication(configuration);
            services.AddDatabase(configuration);
            services.AddServices();
            services.ConfigCors();
            services.JwtSettingsConfig(configuration);
            services.AddZaloPayConfig(configuration);
            services.AddVietMapConfig(configuration);
            services.IntSeedData();
        }
        public static void JwtSettingsConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(option =>
            {
                JwtSettings jwtSettings = new JwtSettings
                {
                    SecretKey = configuration.GetValue<string>("JwtSettings:SecretKey"),
                    Issuer = configuration.GetValue<string>("JwtSettings:Issuer"),
                    Audience = configuration.GetValue<string>("JwtSettings:Audience"),
                    //AccessTokenExpirationMinutes = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes"),
                    //RefreshTokenExpirationDays = configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays")
                };
                jwtSettings.IsValid();
                return jwtSettings;
            });
        }

        public static void RabbitMQConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(option =>
            {
                RabbitMQSettings settings = new()
                {
                    HostName = configuration.GetValue<string>("RabbitMQ:HostName"),
                    UserName = configuration.GetValue<string>("RabbitMQ:UserName"),
                    Password = configuration.GetValue<string>("RabbitMQ:Password"),
                    Port = configuration.GetValue<int>("RabbitMQ:Port"),
                    QueueChannel = new QueueChannel
                    {
                        QaQueue = configuration.GetValue<string>("RabbitMQ:QueueChannel:QaQueue"),
                        TaskProductQueue = configuration.GetValue<string>("RabbitMQ:QueueChannel:TaskProductQueue"),
                    }
                };
                settings.IsValid();
                return settings;
            });
        }
        public static void ConfigCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
            builder => builder
                .AllowAnyOrigin()  
                .AllowAnyMethod()  
                .AllowAnyHeader());
            });
        }
        public static void ConfigCorsSignalR(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:7016")
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();
                    });
            });
        }
        public static void ConfigRoute(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });
        }

        public static void AddHangfireConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddHangfireServer();
        }

        public static void AddAuthenJwt(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings1 = configuration.GetSection("JwtSettings");
            services.AddAuthentication(e =>
            {
                e.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                e.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(e =>
            {
                e.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings1["Issuer"],
                    ValidAudience = jwtSettings1["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings1["SecretKey"])),
                    ClockSkew = TimeSpan.Zero  // Không cho phép gia hạn 5 phút

                };
                e.SaveToken = true;
                e.RequireHttpsMetadata = true;
                e.Events = new JwtBearerEvents();
            });
        }

        public static void ConfigSwagger(this IServiceCollection services)
        {
            // config swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Version = "v1",
                    Title = "API"

                });
                //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //c.IncludeXmlComments(xmlPath);
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "JWT Authorization header sử dụng scheme Bearer.",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Name = "Authorization",
                    Scheme = "bearer"
                });
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
            });

        }

        public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseLazyLoadingProxies()
                       .UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                                     sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                                         maxRetryCount: 5,
                                         maxRetryDelay: TimeSpan.FromSeconds(10),
                                         errorNumbersToAdd: null));
            });
        }

        public static void AddAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
        }

        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IStopService, StopService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<ITicketService, TicketService>();
            services.AddScoped<IHistoryTicketService, HistoryTicketService>();
            services.AddScoped<IShuttleService, ShuttleService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ISupportRequestService, SupportRequestService>();
            services.AddScoped<IPromotionService, PromotionService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IRecordService, RecordService>();
            services.AddScoped<INotiRecipientService, NotiRecipientService>();
            services.AddScoped<IStopEstimateService, StopEstimateService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IRouteStopService, RouteStopService>();
            services.AddScoped<IResponseSupportService, ResponseSupportService>();
            services.AddScoped<ISchoolService, SchoolService>();
            services.AddScoped<IWardService, WardService>();
            services.AddScoped<ITripService, TripService>();
            services.AddScoped<ISchoolShiftService, SchoolShiftService>();
            services.AddScoped<IWithdrawalRequestService, WithdrawalRequestService>();
            services.AddScoped<IFirebaseService, FirebaseService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<ISupabaseService, SupabaseService>();
            services.AddScoped<IUserDeviceService, UserDeviceService>();
            services.AddScoped<IUserPromotionService, UserPromotionService>();
            services.AddScoped<IScheduleOverrideService, ScheduleOverrideService>();
            services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
            services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
            services.AddSingleton<FirestoreService>();
        }

        public static void AddEmailConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        }
        public static void AddZaloPayConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ZaloPaySettings>(configuration.GetSection("ZaloPay"));
        }

        public static void AddVietMapConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<VietMapSettings>(configuration.GetSection("VietMap"));
        }

        public static void IntSeedData(this IServiceCollection services)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var initialiser = new ApplicationDbContextInitialiser(context);
            initialiser.Initialise();
        }
    }
}
