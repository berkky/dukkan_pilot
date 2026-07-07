using DukkanPilot.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = nameof(UserRole.SuperAdmin))]
public abstract class AdminBaseController : Controller
{
}
