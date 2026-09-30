using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Options;
using TaskFlow.Api.Security;
using TaskFlow.Api.Services;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskFlowDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("TaskFlow")
            ?? throw new InvalidOperationException("Connection string 'TaskFlow' is not configured.");

        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString,
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskSummaryQuery, SqlTaskSummaryQuery>();
        return services;
    }

    /// <summary>Identity (users, hashing, lockout, password rules) and the account services. Shared with the tests.</summary>
    public static IServiceCollection AddTaskFlowIdentity(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AuthOptions>().Bind(config.GetSection(AuthOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        var auth = config.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new AuthOptions();

        services.AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.User.AllowedUserNameCharacters = string.Empty; // user name = email, validated separately

                // NIST SP 800-63B: length over composition rules; common and personal passwords are rejected by validators.
                o.Password.RequiredLength = AccountRules.PasswordMinLength;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 1;

                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = auth.Lockout.MaxFailedAttempts;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(auth.Lockout.Minutes);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddUserValidator<EmailDomainValidator>()
            .AddPasswordValidator<CommonPasswordValidator>()
            .AddPasswordValidator<PersonalInfoPasswordValidator>();

        services.AddOptions<JwtOptions>().Bind(config.GetSection(JwtOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<RefreshCookie>();
        return services;
    }

    public static IServiceCollection AddTaskFlowAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // keep "sub", "email", "name" as issued
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = JwtTokenService.KeyFrom(jwt.Value.SigningKey),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
            });

        // Default-deny: every endpoint needs a signed-in user unless it opts out with [AllowAnonymous].
        services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    public static IServiceCollection AddTaskFlowSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken from /api/auth/login.",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            o.SchemaFilter<EnumNamesSchemaFilter>();
            o.AddSecurityDefinition("Bearer", bearer);
            o.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() });

            var xml = Path.Combine(AppContext.BaseDirectory, "TaskFlow.Api.xml");
            if (File.Exists(xml))
            {
                o.IncludeXmlComments(xml);
            }
        });
        return services;
    }
}
