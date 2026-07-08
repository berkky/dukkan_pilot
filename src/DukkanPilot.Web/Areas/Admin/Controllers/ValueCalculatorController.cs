using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Value;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/ValueCalculator")]
public class ValueCalculatorController : AdminBaseController
{
    private readonly AppDbContext _context;

    public ValueCalculatorController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] int? businessId,
        [FromQuery] decimal? monthlySoftwareCost,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Satış Değer Hesaplayıcı";
        ViewData["ActiveMenu"] = "value-calculator";

        var businesses = await LoadBusinessOptionsAsync(cancellationToken);
        ValueCalculatorPrefillViewModel? prefill = null;
        var input = ValueCalculatorHelper.CreatePublicDefaults(monthlySoftwareCost);
        string? businessName = null;

        if (businessId.HasValue && businessId.Value > 0)
        {
            var business = businesses.FirstOrDefault(b => b.Id == businessId.Value);
            if (business is not null)
            {
                businessName = business.Name;
                prefill = await ValueCalculatorHelper.BuildBusinessPrefillAsync(_context, businessId.Value, cancellationToken);
                input = ValueCalculatorHelper.ApplyPrefill(input, prefill);
            }
        }

        input = ValueCalculatorHelper.SanitizeInput(input);

        var model = new ValueCalculatorPageViewModel
        {
            PageTitle = "Satış görüşmesi değer hesaplayıcı",
            Intro = "Demo veya satış görüşmesinde işletmenin varsayımlarıyla tahmini değer senaryosu oluşturun. " +
                    "Sonuçlar garanti değildir; görüşmede temkinli senaryoyu önce gösterin.",
            FormAction = "/Admin/ValueCalculator",
            Input = input,
            SelectedBusinessId = businessId,
            SelectedBusinessName = businessName,
            Businesses = businesses,
            Prefill = prefill,
            Context = "Admin"
        };

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ValueCalculatorInputViewModel input,
        [FromQuery] int? businessId,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Satış Değer Hesaplayıcı";
        ViewData["ActiveMenu"] = "value-calculator";

        var businesses = await LoadBusinessOptionsAsync(cancellationToken);
        ValueCalculatorPrefillViewModel? prefill = null;
        string? businessName = null;

        if (businessId.HasValue && businessId.Value > 0)
        {
            var business = businesses.FirstOrDefault(b => b.Id == businessId.Value);
            if (business is not null)
            {
                businessName = business.Name;
                prefill = await ValueCalculatorHelper.BuildBusinessPrefillAsync(_context, businessId.Value, cancellationToken);
            }
        }

        var sanitized = ValueCalculatorHelper.SanitizeInput(input);
        var result = ValueCalculatorHelper.Calculate(sanitized, "Admin");

        var model = new ValueCalculatorPageViewModel
        {
            PageTitle = "Satış görüşmesi değer hesaplayıcı",
            Intro = "Demo veya satış görüşmesinde işletmenin varsayımlarıyla tahmini değer senaryosu oluşturun.",
            FormAction = "/Admin/ValueCalculator" + (businessId.HasValue ? $"?businessId={businessId.Value}" : string.Empty),
            Input = sanitized,
            Result = result,
            ShowResult = true,
            SelectedBusinessId = businessId,
            SelectedBusinessName = businessName,
            Businesses = businesses,
            Prefill = prefill,
            Context = "Admin"
        };

        return View(model);
    }

    private async Task<List<ValueCalculatorBusinessOptionViewModel>> LoadBusinessOptionsAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new ValueCalculatorBusinessOptionViewModel
            {
                Id = b.Id,
                Name = b.Name
            })
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}
