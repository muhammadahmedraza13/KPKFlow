using KPKflowApp.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPKflowApp.Controllers
{
    [Authorize(AuthenticationSchemes = "ASPXAUTH")]
    [TypeFilter(typeof(AuthenticationAccess))]
    public class DashboardController : Controller
    {
        public IActionResult DailyTasks()
        {
            return View();
        }
    }
}
