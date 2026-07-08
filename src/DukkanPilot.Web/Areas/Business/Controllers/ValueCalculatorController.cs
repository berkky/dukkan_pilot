using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Value;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/ValueCalculator")]
public class ValueCalculatorController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public ValueCalculatorController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (GetCurrentBusinessIdOrForbid(out var businessId) is not null)
        {
            return Forbid();
        }

        ViewData["Title"] = "Değer Senaryosu";
        ViewData["ActiveMenu"] = "value-calculator";

        var isOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        var prefill = await ValueCalculatorHelper.BuildBusinessPrefillAsync(_context, businessId, cancellationToken);
        var input = ValueCalculatorHelper.ApplyPrefill(ValueCalculatorHelper.CreatePublicDefaults(), prefill);
        input = ValueCalculatorHelper.SanitizeInput(input);

        var model = BuildPageModel(input, isOwner, prefill, showResult: false, result: null);
        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ValueCalculatorInputViewModel input,
        CancellationToken cancellationToken)
    {
        if (GetCurrentBusinessIdOrForbid(out var businessId) is not null)
        {
            return Forbid();
        }

        ViewData["Title"] = "Değer Senaryosu";
        ViewData["ActiveMenu"] = "value-calculator";

        var isOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        var prefill = await ValueCalculatorHelper.BuildBusinessPrefillAsync(_context, businessId, cancellationToken);
        var sanitized = ValueCalculatorHelper.SanitizeInput(input);
        var result = ValueCalculatorHelper.Calculate(sanitized, "Business");

        var model = BuildPageModel(sanitized, isOwner, prefill, showResult: true, result);
        return View(model);
    }

    private ValueCalculatorPageViewModel BuildPageModel(
        ValueCalculatorInputViewModel input,
        bool isOwner,
        ValueCalculatorPrefillViewModel prefill,
        bool showResult,
        ValueCalculatorResultViewModel? result)
    {
        return new ValueCalculatorPageViewModel
        {
            PageTitle = "Kendi verilerinizle değer senaryosu",
            Intro = "Son 30 günlük sipariş verilerinizden önerilen başlangıç değerleriyle tahmini senaryo oluşturun. " +
                    "Tüm alanları manuel olarak değiştirebilirsiniz.",
            FormAction = "/Business/ValueCalculator",
            Input = input,
            Result = result,
            ShowResult = showResult,
            Prefill = prefill,
            IsBusinessOwner = isOwner,
            ShowOwnerCtas = isOwner,
            Context = "Business"
        };
    }
}
