using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Data.Seed;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Middleware;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "DukkanPilot.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();

builder.Services.AddDataProtection();
builder.Services.AddSingleton<PasswordResetTokenHelper>();
builder.Services.AddSingleton<PublicOrderTrackingTokenHelper>();
builder.Services.AddScoped<BusinessSubscriptionStatusHelper>();
builder.Services.AddScoped<BusinessPlanLimitHelper>();
builder.Services.AddScoped<GoLiveHelper>();
builder.Services.AddScoped<CustomerOnboardingHelper>();
builder.Services.AddScoped<CustomerSuccessHealthHelper>();
builder.Services.AddScoped<PublicOrderPricingHelper>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISalesRequestService, SalesRequestService>();
builder.Services.AddScoped<IBillingOperationsService, BillingOperationsService>();
builder.Services.AddScoped<RequireActiveSubscriptionFilter>();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}
else
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
