using KPKflowApp.Middleware;
using KPKflowApp.Models.Authentication;
using KPKflowApp.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPKflowApp.Controllers;

[Authorize(AuthenticationSchemes = "ASPXAUTH")]
[TypeFilter(typeof(AuthenticationAccess))]
public class PurchaseRequestController(RequestClient requestClient) : Controller
{
    private readonly RequestClient _requestClient = requestClient;

    public async Task<IActionResult> Initiate(int instanceid = 0)
    {
        ViewBag.wfCode = "BR";

        if (instanceid > 0)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();

            HttpResponseMessage response =
                _requestClient.UseHttpClientGet(
                    "?instanceid=" + instanceid +
                    "&roleid=" + userinfo.RoleID +
                    "&userid=" + userinfo.UserID,
                    "IsTaskAllowed",
                    "Workflow");

            if (response.IsSuccessStatusCode)
            {
                var resultContent = await response.Content.ReadAsStringAsync();

                bool isAllowed = resultContent.Trim() == "true";

                if (isAllowed)
                {
                    return View();
                }
            }

            return Redirect("/" + userinfo.DefaultFormName);
        }

        return View();
    }

    public IActionResult MyTask() => View();

    public IActionResult MyRequest() => View();

    public IActionResult MyApproval() => View();

    public IActionResult ViewMyRequest() => View();

    public IActionResult ControlCopyIssuance() => View();

    public IActionResult ViewMasterBatchDocument() => View();

    public IActionResult ViewControlCopyIssuance() => View();
    public IActionResult BankRateComparison() => View();
}
