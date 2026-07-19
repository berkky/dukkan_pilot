using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Common;
using DukkanPilot.Web.Api.Mobile.V1.Configuration;
using DukkanPilot.Web.Api.Mobile.V1.Services;
using DukkanPilot.Web.Constants;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DukkanPilot.Web.Api.Mobile.V1;

public static class MobileApiServiceCollectionExtensions
{
    public static IServiceCollection AddMobileApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MobileAuthOptions>()
            .Bind(configuration.GetSection(MobileAuthOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MobileAuthOptions>, MobileAuthOptionsValidator>();

        services.AddAuthentication()
            .AddJwtBearer(MobileAuthDefaults.Scheme, options =>
            {
                options.MapInboundClaims = false;
            });

        services.AddOptions<JwtBearerOptions>(MobileAuthDefaults.Scheme)
            .Configure<IOptions<MobileAuthOptions>>((jwtOptions, mobileOptionsAccessor) =>
            {
                var mobileOptions = mobileOptionsAccessor.Value;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = mobileOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = mobileOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(mobileOptions.SigningKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                jwtOptions.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await MobileProblemDetails.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "unauthorized",
                            "A valid mobile access token is required.");
                    },
                    OnForbidden = context => MobileProblemDetails.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "forbidden",
                        "The mobile account does not have permission for this operation.")
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(MobilePolicies.Authenticated, policy =>
            {
                policy.AddAuthenticationSchemes(MobileAuthDefaults.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => HasMobileIdentity(context.User));
            });

            options.AddPolicy(MobilePolicies.OwnerOrStaff, policy =>
            {
                policy.AddAuthenticationSchemes(MobileAuthDefaults.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    HasMobileIdentity(context.User) &&
                    MobilePrincipal.TryGetContext(context.User, out var mobileContext) &&
                    mobileContext.BusinessRole is BusinessRole.Owner or BusinessRole.Staff);
            });

            options.AddPolicy(MobilePolicies.OwnerOnly, policy =>
            {
                policy.AddAuthenticationSchemes(MobileAuthDefaults.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    HasMobileIdentity(context.User) &&
                    MobilePrincipal.TryGetContext(context.User, out var mobileContext) &&
                    mobileContext.BusinessRole == BusinessRole.Owner &&
                    context.User.FindFirst(ClaimTypes.Role)?.Value == nameof(UserRole.BusinessOwner));
            });
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(item => item.Value?.Errors.Count > 0)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                            .ToArray());

                return new BadRequestObjectResult(MobileProblemDetails.Create(
                    context.HttpContext,
                    StatusCodes.Status400BadRequest,
                    "validation_failed",
                    "One or more validation errors occurred.",
                    extensions: new Dictionary<string, object?> { ["errors"] = errors }));
            };
        });

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("mobile-login", context => CreateIpLimiter(context, 5, TimeSpan.FromMinutes(1)));
            options.AddPolicy("mobile-refresh", context => CreateIpLimiter(context, 10, TimeSpan.FromMinutes(1)));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value
                    : TimeSpan.FromMinutes(1);
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                await MobileProblemDetails.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "rate_limit_exceeded",
                    "Too many requests. Try again later.");
            };
        });

        services.AddScoped<IMobileTokenService, MobileTokenService>();
        services.AddScoped<IOrderStatusService, OrderStatusService>();
        services.AddScoped<IMobileOrderQueryService, MobileOrderQueryService>();
        return services;
    }

    private static RateLimitPartition<string> CreateIpLimiter(
        HttpContext context,
        int permitLimit,
        TimeSpan window)
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static bool HasMobileIdentity(ClaimsPrincipal principal)
    {
        var userRole = principal.FindFirst(ClaimTypes.Role)?.Value;
        return principal.FindFirst(MobileAuthDefaults.ClientIdClaim)?.Value == MobileAuthDefaults.ClientId &&
               userRole is nameof(UserRole.BusinessOwner) or nameof(UserRole.Staff) &&
               MobilePrincipal.TryGetContext(principal, out _);
    }
}
