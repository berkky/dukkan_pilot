using System.Security.Claims;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Security;
using DukkanPilot.Web.Constants;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Account;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordResetTokenHelper _passwordResetTokenHelper;
    private readonly IAuditLogService _auditLog;

    public AccountController(AppDbContext context, PasswordResetTokenHelper passwordResetTokenHelper, IAuditLogService auditLog)
    {
        _context = context;
        _passwordResetTokenHelper = passwordResetTokenHelper;
        _auditLog = auditLog;
    }

    [HttpGet("/Account/Register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectByRole(User);
        }

        return View(new RegisterViewModel());
    }

    [HttpPost("/Account/Register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = NormalizeEmail(model.Email);
        if (await _context.AppUsers.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kullanılıyor.");
            return View(model);
        }

        var slug = await BusinessSlugHelper.GenerateUniqueSlugAsync(_context, model.BusinessName);

        var business = new Business
        {
            Name = model.BusinessName.Trim(),
            Slug = slug,
            Phone = TrimToNull(model.PhoneNumber),
            IsActive = true,
            Setting = new BusinessSetting
            {
                ThemeColor = "#2563eb",
                Currency = "TRY",
                WhatsAppNumber = NormalizeWhatsAppNumber(model.PhoneNumber)
            }
        };

        var subscriptionPlan = await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ThenBy(p => p.SortOrder)
            .FirstOrDefaultAsync();

        if (subscriptionPlan is not null)
        {
            business.Subscriptions.Add(new BusinessSubscription
            {
                SubscriptionPlanId = subscriptionPlan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(14),
                Status = SubscriptionStatus.Trial,
                IsActive = true
            });
        }

        var user = new AppUser
        {
            Email = normalizedEmail,
            PasswordHash = PasswordHelper.HashPassword(model.Password),
            FullName = model.OwnerFullName.Trim(),
            Role = UserRole.BusinessOwner,
            IsActive = true
        };

        _context.Businesses.Add(business);
        _context.AppUsers.Add(user);
        _context.UserBusinessRoles.Add(new UserBusinessRole
        {
            AppUser = user,
            Business = business,
            Role = BusinessRole.Owner,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Kayıt başarılı. Şimdi giriş yapabilirsiniz.";

        await _auditLog.LogAccountAsync(
            "Account.Registered",
            $"Yeni işletme sahibi kaydoldu: {business.Name}",
            new { businessId = business.Id },
            businessId: business.Id,
            userEmail: user.Email);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/Account/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl) ?? RedirectByRole(User);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("/Account/Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");

            await _auditLog.LogAccountAsync(
                "Account.LoginFailed",
                $"Giriş denemesi başarısız: {normalizedEmail}",
                severity: "Warning",
                userEmail: normalizedEmail);

            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız pasif durumda. Giriş yapamazsınız.");

            await _auditLog.LogAccountAsync(
                "Account.LoginFailed",
                $"Giriş denemesi başarısız (pasif hesap): {normalizedEmail}",
                severity: "Warning",
                userEmail: normalizedEmail);

            return View(model);
        }

        var businessRole = await _context.UserBusinessRoles
            .AsNoTracking()
            .Where(r => r.AppUserId == user.Id && r.IsActive)
            .OrderByDescending(r => r.Role == BusinessRole.Owner)
            .ThenByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (businessRole is not null)
        {
            claims.Add(new Claim(AuthClaimTypes.BusinessId, businessRole.BusinessId.ToString()));
            claims.Add(new Claim(AuthClaimTypes.BusinessRole, businessRole.Role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddHours(8)
            });

        await _auditLog.LogAccountAsync(
            "Account.LoginSuccess",
            $"Giriş başarılı: {user.Email}",
            businessId: businessRole?.BusinessId,
            userEmail: user.Email);

        var localRedirect = RedirectToLocal(returnUrl);
        if (localRedirect is not null)
        {
            return localRedirect;
        }

        return RedirectByRole(user.Role);
    }

    [HttpPost("/Account/Logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userEmail = User.Identity?.Name is not null ? User.FindFirst(ClaimTypes.Email)?.Value : null;

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _auditLog.LogAccountAsync(
            "Account.Logout",
            "Kullanıcı çıkış yaptı.",
            userEmail: userEmail);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/Account/AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet("/Account/ForgotPassword")]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectByRole(User);
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost("/Account/ForgotPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = NormalizeEmail(model.Email);
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // Email enumeration önleme: kullanıcı yok/pasif olsa da aynı confirmation sayfasına git.
        if (user is not null && user.IsActive)
        {
            var token = _passwordResetTokenHelper.GenerateToken(user);
            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, token },
                Request.Scheme);

            // TODO: Production ortamında resetLink e-posta ile gönderilecek.
            TempData["ResetLink"] = resetLink;

            await _auditLog.LogAccountAsync(
                "Account.PasswordResetRequested",
                $"Şifre sıfırlama talebi oluşturuldu: {normalizedEmail}",
                userEmail: normalizedEmail);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet("/Account/ForgotPasswordConfirmation")]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet("/Account/ResetPassword")]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Şifre sıfırlama bağlantısı geçersiz. Lütfen yeni bir talep oluşturun.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost("/Account/ResetPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = NormalizeEmail(model.Email);
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Şifre sıfırlama işlemi gerçekleştirilemedi. Lütfen yeni bir talep oluşturun.");
            return View(model);
        }

        var validationResult = _passwordResetTokenHelper.ValidateToken(model.Token, user, normalizedEmail);
        if (!validationResult.IsValid)
        {
            ModelState.AddModelError(string.Empty, validationResult.ErrorMessage ?? "Şifre sıfırlama bağlantısı geçersiz.");
            return View(model);
        }

        user.PasswordHash = PasswordHelper.HashPassword(model.Password);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAccountAsync(
            "Account.PasswordResetCompleted",
            $"Şifre sıfırlama tamamlandı: {normalizedEmail}",
            userEmail: normalizedEmail);

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet("/Account/ResetPasswordConfirmation")]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return null;
    }

    private IActionResult RedirectByRole(UserRole role)
    {
        return role switch
        {
            UserRole.SuperAdmin => Redirect("/Admin/Dashboard"),
            UserRole.BusinessOwner or UserRole.Staff => Redirect("/Business/Dashboard"),
            _ => RedirectToAction(nameof(AccessDenied))
        };
    }

    private IActionResult RedirectByRole(ClaimsPrincipal principal)
    {
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
        if (Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return RedirectByRole(role);
        }

        return Redirect("/");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizeWhatsAppNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        if (digits.StartsWith('0'))
        {
            digits = "9" + digits;
        }
        else if (!digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = "90" + digits;
        }

        return digits;
    }
}
