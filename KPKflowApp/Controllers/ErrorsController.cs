using KPKflowApp.Models.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace KPKflowApp.Controllers
{
    public class ErrorsController : Controller
    {
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                if (statusCode == 403 || statusCode == 404)
                {
                    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, StatusCode = statusCode.ToString() });
                }
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
