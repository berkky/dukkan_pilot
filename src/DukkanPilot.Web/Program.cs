using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Data.Seed;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();

builder.Services.AddDataProtection();
builder.Services.AddSingleton<PasswordResetTokenHelper>();
builder.Services.AddSingleton<PublicOrderTrackingTokenHelper>();
builder.Services.AddScoped<BusinessSubscriptionStatusHelper>();
builder.Services.AddScoped<BusinessPlanLimitHelper>();
builder.Services.AddScoped<GoLiveHelper>();
builder.Services.AddScoped<PublicOrderPricingHelper>();
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
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
