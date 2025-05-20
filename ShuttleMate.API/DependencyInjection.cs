using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Repositories.Context;
using ShuttleMate.Services.Services;
using ShuttleMate.Services;
using System.Reflection;
using ShuttleMate.Contract.Repositories.Base;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Wanvi.Repositories.SeedData;

namespace WanviBE.API
{
    public static class DependencyInjection
    {
        public static void AddConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigRoute();
            services.AddMemoryCache();
            //services.AddIdentity();
            services.AddInfrastructure(configuration);
            //services.AddEmailConfig(configuration);
            services.AddAutoMapper();
            services.ConfigSwagger();
            services.AddAuthenJwt(configuration);
            //services.AddGoogleAuthentication(configuration);
            //services.AddFacebookAuthentication(configuration);
            services.AddDatabase(configuration);
            services.AddServices();
            services.ConfigCors();
            //services.ConfigCorsSignalR();
            //services.RabbitMQConfig(configuration);
            services.JwtSettingsConfig(configuration);
            //services.IntSeedData();
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
                .AllowAnyOrigin()  // Cho phép tất cả nguồn
                .AllowAnyMethod()  // Cho phép tất cả phương thức (GET, POST, PUT, DELETE,...)
                .AllowAnyHeader()); // Cho phép tất cả headers
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
                    ClockSkew = TimeSpan.Zero  // ✅ Không cho phép gia hạn 5 phút

                };
                e.SaveToken = true;
                e.RequireHttpsMetadata = true;
                e.Events = new JwtBearerEvents();
            });
        }

        //public static void AddGoogleAuthentication(this IServiceCollection services, IConfiguration configuration)
        //{
        //    var googleSettings = configuration.GetSection("GoogleOAuth");

        //    services.AddAuthentication(options =>
        //    {
        //        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        //    })
        //    .AddCookie()
        //    .AddGoogle(options =>
        //    {
        //        options.ClientId = googleSettings["ClientId"];
        //        options.ClientSecret = googleSettings["ClientSecret"];
        //        options.CallbackPath = googleSettings["CallbackPath"];
        //    });
        //}

        //public static void AddFacebookAuthentication(this IServiceCollection services, IConfiguration configuration)
        //{
        //    var facebookSettings = configuration.GetSection("FacebookOAuth");

        //    services.AddAuthentication(options =>
        //    {
        //        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //        options.DefaultChallengeScheme = FacebookDefaults.AuthenticationScheme;
        //    })
        //    .AddFacebook(options =>
        //    {
        //        options.AppId = facebookSettings["AppId"];
        //        options.AppSecret = facebookSettings["AppSecret"];
        //        options.CallbackPath = facebookSettings["CallbackPath"];
        //    });
        //}

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

        //public static void AddIdentity(this IServiceCollection services)
        //{
        //    services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        //    {
        //    })
        //     .AddEntityFrameworkStores<DatabaseContext>()
        //     .AddDefaultTokenProviders();
        //}

        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
        }

        //public static void AddEmailConfig(this IServiceCollection services, IConfiguration configuration)
        //{
        //    services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        //}

        public static void IntSeedData(this IServiceCollection services)
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var initialiser = new ApplicationDbContextInitialiser(context);
            initialiser.Initialise();
        }
    }
}
