using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Security;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Staff")]
[Authorize(Roles = nameof(UserRole.BusinessOwner))]
[RequireActiveSubscription]
public class StaffController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public StaffController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "staff";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var items = await _context.UserBusinessRoles
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.Role == BusinessRole.Owner)
            .ThenBy(r => r.AppUser.FullName)
            .Select(r => new StaffListViewModel
            {
                Id = r.AppUserId,
                FullName = r.AppUser.FullName,
                Email = r.AppUser.Email,
                BusinessRole = r.Role,
                IsActive = r.AppUser.IsActive,
                CreatedAt = r.AppUser.CreatedAt
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "staff-create";
        return View(new StaffFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffFormViewModel model)
    {
        ViewData["ActiveMenu"] = "staff-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        ValidatePasswordForCreate(model);

        if (!await IsEmailAvailableAsync(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            Email = NormalizeEmail(model.Email),
            PasswordHash = PasswordHelper.HashPassword(model.Password!),
            FullName = model.FullName.Trim(),
            Role = MapToUserRole(model.BusinessRole),
            IsActive = true
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        _context.UserBusinessRoles.Add(new UserBusinessRole
        {
            AppUserId = user.Id,
            BusinessId = businessId,
            Role = model.BusinessRole,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Personel başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "staff";

        var model = await BuildDetailsViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "staff";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffFormViewModel model)
    {
        ViewData["ActiveMenu"] = "staff";

        if (id != model.Id)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        model.IsEdit = true;
        ValidatePasswordForEdit(model);

        if (CurrentUserId == id && !model.IsActive)
        {
            ModelState.AddModelError(nameof(model.IsActive), "Kendi hesabınızı pasif duruma alamazsınız.");
        }

        if (!await IsEmailAvailableAsync(model.Email, id))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var businessStaff = await GetBusinessStaffAsync(businessId, id);
        if (businessStaff is null)
        {
            return NotFound();
        }

        var user = businessStaff.AppUser;
        user.FullName = model.FullName.Trim();
        user.Email = NormalizeEmail(model.Email);
        user.Role = MapToUserRole(model.BusinessRole);
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        businessStaff.Role = model.BusinessRole;
        businessStaff.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = PasswordHelper.HashPassword(model.Password);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Personel bilgileri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "staff";

        var model = await BuildDetailsViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["IsSelf"] = CurrentUserId == id;
        return View(model);
    }

    [HttpPost("Delete/{id:int}")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (CurrentUserId == id)
        {
            TempData["Error"] = "Kendi hesabınızı pasif duruma alamazsınız.";
            return RedirectToAction(nameof(Index));
        }

        var businessStaff = await GetBusinessStaffAsync(businessId, id);
        if (businessStaff is null)
        {
            return NotFound();
        }

        businessStaff.AppUser.IsActive = false;
        businessStaff.AppUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Personel pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserBusinessRole?> GetBusinessStaffAsync(int businessId, int appUserId)
    {
        return await _context.UserBusinessRoles
            .Include(r => r.AppUser)
            .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.AppUserId == appUserId);
    }

    private async Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = NormalizeEmail(email);
        return !await _context.AppUsers.AnyAsync(u =>
            u.Email.ToLower() == normalizedEmail &&
            (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }

    private async Task<StaffFormViewModel?> BuildFormViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var businessStaff = await GetBusinessStaffAsync(businessId.Value, id);
        if (businessStaff is null)
        {
            return null;
        }

        return new StaffFormViewModel
        {
            Id = businessStaff.AppUserId,
            FullName = businessStaff.AppUser.FullName,
            Email = businessStaff.AppUser.Email,
            BusinessRole = businessStaff.Role,
            IsActive = businessStaff.AppUser.IsActive,
            IsEdit = true
        };
    }

    private async Task<StaffDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var businessStaff = await GetBusinessStaffAsync(businessId.Value, id);
        if (businessStaff is null)
        {
            return null;
        }

        return new StaffDetailsViewModel
        {
            Id = businessStaff.AppUserId,
            FullName = businessStaff.AppUser.FullName,
            Email = businessStaff.AppUser.Email,
            BusinessRole = businessStaff.Role,
            IsActive = businessStaff.AppUser.IsActive,
            CreatedAt = businessStaff.AppUser.CreatedAt,
            UpdatedAt = businessStaff.AppUser.UpdatedAt
        };
    }

    private void ValidatePasswordForCreate(StaffFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Şifre zorunludur.");
        }
        else if (model.Password.Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), "Şifre en az 6 karakter olmalıdır.");
        }

        if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Şifre tekrarı zorunludur.");
        }
        else if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Şifreler eşleşmiyor.");
        }
    }

    private void ValidatePasswordForEdit(StaffFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password) && string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Yeni şifre girildiğinde şifre alanı zorunludur.");
        }
        else if (model.Password.Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), "Şifre en az 6 karakter olmalıdır.");
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Şifreler eşleşmiyor.");
        }
    }

    private static UserRole MapToUserRole(BusinessRole businessRole) => businessRole switch
    {
        BusinessRole.Owner => UserRole.BusinessOwner,
        BusinessRole.Staff => UserRole.Staff,
        _ => UserRole.Staff
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
