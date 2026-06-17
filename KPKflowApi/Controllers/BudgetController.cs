using KPKflowApi.Context;
using KPKflowApi.RateLimiting;
using KPKflowApi.Utility;
using KPKflowApi.Models.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Data;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Web;

namespace KPKflowApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/{controller}/{action}/{id:int?}")]
    [ApiController]
    public class BudgetController : ControllerBase
    {
        private readonly DataAccessLayer _DAL;
        private readonly ILogger<SettingsController> _logger;
        public BudgetController(DataAccessLayer DAL, ILogger<SettingsController> logger)
        {
            _DAL = DAL;
            _logger = logger;
           
        }


        #region Activity Log
        public void SystemActivityLog(int? ActivityID, string? ActivityDetails)
        {
            bool Result = false;

            ClaimsPrincipal claimsPrincipal = HttpContext.User;
            string HostName = Dns.GetHostName();
            IPHostEntry HostIPs = Dns.GetHostEntry(HostName);
            string IPAddress = HostIPs.AddressList[0].ToString();
            string UserID = (from c in claimsPrincipal.Claims where c.Type == "UserID" select c.Value).FirstOrDefault();
            var routeData = HttpContext.Request.RouteValues;
            string controllerName = routeData["controller"].ToString();
            string actionName = routeData["action"].ToString();
            string FormName = controllerName + "/" + actionName;
            string ActivityDetailsComplete = IPAddress + " " + ActivityDetails + " " + FormName;

            try
            {
                NameValueCollection nv = new NameValueCollection();
                nv.Clear();
                nv.Add("FormID-INT", "0");
                nv.Add("ActivityID-INT", ActivityID.ToString());
                nv.Add("UserID-INT", UserID);
                nv.Add("ActivityDetails-VARCHAR", ActivityDetailsComplete);
                Result = _DAL.InsertData("sp_insert_activitylog", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                _logger.LogInformation("{0} {1} {2}", controllerName, MethodBase.GetCurrentMethod().Name, ActivityDetailsComplete);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2} {3}", controllerName, MethodBase.GetCurrentMethod().Name, ActivityDetailsComplete, ex.Message);
            }
        }

        #endregion

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveInitiateRequest([FromBody] Budget budget)
        {
            bool Result = false;
            DataTable dt;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("workflow-VARCHAR", InputSanitizer.Sanitize(budget.workflow));
                nv.Add("instanceid-INT", budget.instanceid == null ? "0" : budget.instanceid.ToString());
                nv.Add("dept-VARCHAR", InputSanitizer.Sanitize(budget.Department));
                nv.Add("purpose-VARCHAR", InputSanitizer.Sanitize(budget.Purpose));
                nv.Add("estimatedamount-INT", budget.EstimatedAmount.ToString());
                nv.Add("requiredby-DATETIME ", budget.RequiredBy.ToString());
                nv.Add("priority-VARCHAR", InputSanitizer.Sanitize(budget.Priority));
                nv.Add("createdby-INT", budget.userid.ToString());


                dt = _DAL.GetData("sp_insert_budget", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0 )
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_budget");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_budget");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (dt.Rows.Count > 0)
            {
                return Ok(dt);
            }
            else
            {
                return BadRequest(dt);
            }
        }
        [RateLimitMiddleware(50, 5)]
        [HttpGet]
        public IActionResult GetBudgetDetailByInstanceId(int instanceid)
        {
            bool Result = false;
            DataTable dt;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("instanceid-INT", instanceid.ToString());

                dt = _DAL.GetData("sp_select_BudgetDetailByInstanceId", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_select_BudgetDetailByInstanceId");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_select_BudgetDetailByInstanceId");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (dt != null)
            {
                return Ok(dt);
            }
            else
            {
                return BadRequest(dt);
            }
        }

    }

}
