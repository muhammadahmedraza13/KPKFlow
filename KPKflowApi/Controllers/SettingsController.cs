using AngleSharp.Dom;
using KPKflowApi.Context;
using KPKflowApi.Extensions;
using KPKflowApi.Models.Authentication;
using KPKflowApi.Models.Settings;
using KPKflowApi.RateLimiting;
using KPKflowApi.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Data;
using System.DirectoryServices;
using System.Net;
using System.Reflection;
using System.Security.Claims;

namespace KPKflowApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/{controller}/{action}/{id:int?}")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly DataAccessLayer _DAL;
        private readonly SendEmail _sendemail;
        private readonly ILogger<SettingsController> _logger;
        private readonly DataEncryptor _dataencryptor;
        private readonly RandomStringGenerator _randomstringgenerator;
        private readonly CommonMethods _CommonMethods;
        private readonly IConfiguration _configuration;

        public SettingsController(DataAccessLayer DAL,ILogger<SettingsController> logger, SendEmail sendemail, DataEncryptor dataencryptor, RandomStringGenerator randomstringgenerator, CommonMethods commonMethods, IConfiguration configuration)
        {
            _DAL = DAL;
            _logger = logger;
            _sendemail = sendemail;
            _dataencryptor = dataencryptor;
            _randomstringgenerator = randomstringgenerator;
            _CommonMethods = commonMethods;
            _configuration = configuration;
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
   
        #region RolesMaster
        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetRoles()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_roles", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_roles");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_roles");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetRolesByID(string RoleID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", RoleID);
                dt = _DAL.GetData("sp_select_roles_byid", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_roles_byid");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_roles_byid");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditRoles([FromBody] Role role)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", role.RoleID);
                nv.Add("RoleName-VARCHAR", InputSanitizer.Sanitize(role.RoleName));
                nv.Add("IsActive-BIT", role.IsActive == null ? "0" : "1");
                nv.Add("UserID-INT", role.UserID);
                Result = _DAL.InsertData("sp_update_role", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_role");

                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_role");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveRoles([FromBody] Role role)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleName-VARCHAR", InputSanitizer.Sanitize(role.RoleName));
                nv.Add("IsActive-BIT", role.IsActive == null ? "0" : "1");
                nv.Add("UserID-INT", role.UserID);
                Result = _DAL.InsertData("sp_insert_role", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_role");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_role");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteRoles([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", deleteFromDB_.RoleID);
                nv.Add("UserID-INT", deleteFromDB_.UserID);
                Result = _DAL.InsertData("sp_delete_role", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_role");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_role");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }
        #endregion

        #region UserMaster
        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetUserType(string TypeID)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TypeID-INT", TypeID);
                dt = _DAL.GetData("sp_select_usertype", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_usertype");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_usertype");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(dt);

        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetUsersByType(string TypeID,string UserID)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("TypeID-INT", TypeID);
                nv.Add("UserID-INT", UserID);
                dt = _DAL.GetData("sp_select_users", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_users");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_users");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " sp_select_users" + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetForms()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_forms", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_forms");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_forms");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                BadRequest(ex.Message);
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult EditUsers([FromBody] User user)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", user.UserID);
                nv.Add("UserName-VARCHAR", InputSanitizer.Sanitize(user.UserName));
                nv.Add("UserEmail-VARCHAR", InputSanitizer.Sanitize(user.UserEmail));
                nv.Add("RoleID-INT", user.RoleID);
                nv.Add("DefaultFormID-INT", user.FormID);
                nv.Add("UserTypeID-INT", user.UserTypeID);
                nv.Add("MobileNumber-VARCHAR", InputSanitizer.Sanitize(user.MobileNumber));
                nv.Add("IsActive-BIT", user.IsActive == null ? "0" : "1");
                nv.Add("EditUserID-INT", user.EditUserID);
                nv.Add("IsAdUser-BIT", user.IsAdUser == null ? "0" : "1");
                nv.Add("Designation-VARCHAR", user.Designation == null ? "NULL" : InputSanitizer.Sanitize(user.Designation));
                nv.Add("Department-VARCHAR", user.Department == null ? "NULL" : InputSanitizer.Sanitize(user.Department));
                nv.Add("DomainName-VARCHAR", user.DomainName == null ? "NULL" : InputSanitizer.Sanitize(user.DomainName));
                nv.Add("LoginName-VARCHAR", InputSanitizer.Sanitize(user.LoginName));
                Result = _DAL.InsertData("sp_update_user", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_user");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveUsers([FromBody] User user)
        {
            bool Result = false;
            DataTable dt;
            try
            {
                
                string encrypted_password = string.Empty;
                string isAdUser = user.IsAdUser == null ? "0" : "1";
                if (isAdUser == "0")
                {
                    string RandomStrings = _randomstringgenerator.GetRandomString();
                    string EmailBody = ActivityLog.EmailBody + " RandomPassword : " + RandomStrings;
                    bool SendMail = _sendemail.SendEmailToUsers(new List<string> { user.UserEmail }, EmailBody, ActivityLog.EmailSubject, _DAL);

                    if (SendMail)
                    {
                        encrypted_password = _CommonMethods.HashPassword(RandomStrings);
                    }
                }

                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", (user.UserID == null ? "NULL" : user.UserID));
                nv.Add("UserName-VARCHAR", InputSanitizer.Sanitize(user.UserName));
                nv.Add("UserEmail-VARCHAR", (user.UserEmail == null ? "NULL" : InputSanitizer.Sanitize(user.UserEmail)));
                nv.Add("UserPassword-VARCHAR", encrypted_password);
                nv.Add("RoleID-INT", user.RoleID);
                nv.Add("DefaultFormID-INT", user.FormID);
                nv.Add("UserTypeID-INT", user.UserTypeID);
                nv.Add("MobileNumber-VARCHAR", InputSanitizer.Sanitize(user.MobileNumber));
                nv.Add("IsActive-INT", user.IsActive == null ? "0" : "1");
                nv.Add("CreateUserID-INT", user.EditUserID);
                nv.Add("IsAdUser-BIT", user.IsAdUser == null ? "0" : "1");
                nv.Add("Designation-VARCHAR", user.Designation == null ? "NULL" : InputSanitizer.Sanitize(user.Designation));
                nv.Add("Department-VARCHAR", user.Department == null ? "NULL" : InputSanitizer.Sanitize(user.Department));
                nv.Add("DomainName-VARCHAR", user.DomainName == null ? "NULL" : InputSanitizer.Sanitize(user.DomainName));
                nv.Add("LoginName-VARCHAR", InputSanitizer.Sanitize(user.LoginName));
                dt = _DAL.GetData("sp_insert_user", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_user");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_user");
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

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult DeleteUsers([FromBody] DeleteFromDB deleteFromDB_)
        {
            bool Result = false;

            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", deleteFromDB_.UserID);
                nv.Add("EditUserID-INT", deleteFromDB_.EditUserID);
                Result = _DAL.InsertData("sp_delete_user", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_user");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            return Ok(Result);
        }

        #endregion

        #region RolesMapping

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetMapping(string RoleId)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();

                nv.Clear();
                nv.Add("RoleID-INT", RoleId);
                dt = _DAL.GetData("sp_select_rolesmapping", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_rolesmapping");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_rolesmapping");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetMapping_2(string RoleId)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();

                nv.Clear();
                nv.Add("RoleID-INT", RoleId);
                dt = _DAL.GetData("sp_select_rolesmapping_2", nv, _DAL.CSManagementPortalDatabase);
                nv = null;
                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_rolesmapping_2");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_rolesmapping_2");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult UpdateRolesMapping([FromBody] RoleMapping mapping)
        {
            bool Result = false;
            bool Result2 = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();

                nv.Clear();
                nv.Add("RoleID-INT", mapping.RoleID);
                Result = _DAL.InsertData("sp_delete_rolemapping", nv, _DAL.CSManagementPortalDatabase);

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete + "sp_delete_rolemapping");

                    foreach (RoleMappingCollection item in mapping.RoleMappingCollection)
                    {
                        nv.Clear();
                        nv.Add("RoleID-INT", mapping.RoleID);
                        nv.Add("UserID-INT", mapping.UserID);
                        nv.Add("FormsID-INT", item.FormsID);
                        nv.Add("AllowInsert-BIT", item.AllowInsert);
                        nv.Add("AllowUpdate-BIT", item.AllowUpdate);
                        nv.Add("AllowDelete-BIT", item.AllowDelete);
                        nv.Add("IsMenu-BIT", item.IsMenu);
                        Result2 = _DAL.InsertData("sp_update_rolesmapping", nv, _DAL.CSManagementPortalDatabase);

                        if (Result2)
                        {
                            SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_rolesmapping");
                        }
                        else
                        {
                            SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_rolesmapping");
                        }
                    }
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Delete, ActivityLog.ActivityDetails_Delete2 + "sp_delete_rolemapping");
                }
                nv = null;
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        #endregion

        #region API Acess
        [HttpGet]
        public IActionResult GetAllowedApiByRole(string RoleID)
        {
            DataTable dt = new DataTable();
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("RoleID-INT", RoleID);
                dt = _DAL.GetData("sp_select_roles_byid", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_roles_byid");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_roles_byid");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " sp_select_roles_byid" + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
            return Ok(dt);
        }
        #endregion

        #region AdUsers

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult FetchAdUsers()
        {
            DataTable dt = new DataTable();
            DataTable dtUsers = new DataTable();
            try
            {

                dt = _DAL.GetData("sp_select_ldapsettings2", null, _DAL.CSManagementPortalDatabase);
                string Ldap = dt.Rows[0]["LdapPath"].ToString();
                string userid = dt.Rows[0]["LdapUser"].ToString();
                string password = dt.Rows[0]["LdapPassword"].ToString();

                dtUsers = LoadAdUsers(Ldap, userid, password);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_ldapsettings2");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_ldapsettings2");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok(dtUsers);

        }

        private DataTable LoadAdUsers(string Ldap, string userid, string password)
        {

            DirectoryEntry myLdapConnection = new DirectoryEntry(Ldap, userid, _CommonMethods.DecryptPassword(password));
            DirectorySearcher search = new DirectorySearcher(myLdapConnection) { Filter = ("(objectClass=user)") };
            search.CacheResults = true;

            SearchResultCollection allResults = search.FindAll();
            string varStr = myLdapConnection.Name.ToString();
            string[] varDomain = varStr.Split('=');

            DataTable table = new DataTable();
            table.TableName = "AdUsers";
            table.Columns.Add("UserName");
            table.Columns.Add("UserID");
            table.Columns.Add("EmailID");
            table.Columns.Add("Designation");
            table.Columns.Add("Department");
            table.Columns.Add("TelephoneNo");
            table.Columns.Add("DomainName");
            foreach (SearchResult searchResult in allResults)
            {
                string UserName = "";
                string UserID = "";
                string EmailID = "";
                string Department = "";
                string Designation = "";
                string TelephoneNo = "";
                string DomainName = varDomain[1];
                if (searchResult.Properties["name"] != null && searchResult.Properties["name"].Count > 0)
                {
                    UserName = searchResult.Properties["name"][0].ToString();
                }
                if (UserName == "MettisDev2")
                {

                }
                if (searchResult.Properties["department"] != null && searchResult.Properties["department"].Count > 0)
                {
                    Department = searchResult.Properties["department"][0].ToString();
                }
                if (searchResult.Properties["title"] != null && searchResult.Properties["title"].Count > 0)
                {
                    Designation = searchResult.Properties["title"][0].ToString();
                }
                if (searchResult.Properties["samAccountName"] != null && searchResult.Properties["samAccountName"].Count > 0)
                {
                    UserID = searchResult.Properties["samAccountName"][0].ToString();
                }
                if (searchResult.Properties["mail"] != null && searchResult.Properties["mail"].Count > 0)
                {
                    EmailID = searchResult.Properties["mail"][0].ToString();
                }
                if (searchResult.Properties["telephonenumber"] != null && searchResult.Properties["telephonenumber"].Count > 0)
                {
                    TelephoneNo = searchResult.Properties["telephonenumber"][0].ToString();
                }

                DataRow NewRow = table.NewRow();
                NewRow["UserName"] = UserName; ;
                NewRow["UserID"] = UserID;
                NewRow["EmailID"] = EmailID;
                NewRow["Designation"] = Designation;
                NewRow["Department"] = Department;
                NewRow["TelephoneNo"] = TelephoneNo;
                NewRow["DomainName"] = DomainName;
                table.Rows.Add(NewRow);
            }
            return table;
        }
        #endregion
        
        #region SmtpSetting

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SmtpSettingUpdate([FromBody] SMTPSettings smtpSetting)
        {

            bool Result = false;

            DataTable dt;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("Smtp-VARCHAR", InputSanitizer.Sanitize(smtpSetting.Smtp));
                nv.Add("SenderEmailID-VARCHAR", InputSanitizer.Sanitize(smtpSetting.SenderEmailID));
                nv.Add("SmtpPassword-VARCHAR", smtpSetting.SmtpPassword);
                nv.Add("SmtpPort-VARCHAR", InputSanitizer.Sanitize(smtpSetting.SmtpPort));
                nv.Add("DisplayName-VARCHAR", InputSanitizer.Sanitize(smtpSetting.DisplayName));
                nv.Add("EnableSSL-BIT", smtpSetting.EnableSSL.ToString());

                Result = _DAL.InsertData("sp_insert_smtpsetting", nv, _DAL.CSManagementPortalDatabase);

                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_smtpsetting");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_smtpsetting");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok(Result);


        }

        [HttpGet]
        public IActionResult SmtpSettingGet()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_SmtpSettings", null, _DAL.CSManagementPortalDatabase);
                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_SmtpSettings");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_SmtpSettings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok(dt);
        }
        #endregion

        #region UserProfile
        [RateLimitMiddleware(10000, 5)]
        [HttpPost]
        public IActionResult UpdateProfile([FromBody] UserProfile UserProfile)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("UserID-INT", UserProfile.UserID);
                nv.Add("UserName-VARCHAR", InputSanitizer.Sanitize(UserProfile.UserName));
                nv.Add("DefaultFormID-INT", UserProfile.FormID);
                nv.Add("UserImage-VARCHAR", UserProfile.ImageFile == null ? "NULL" : InputSanitizer.Sanitize(UserProfile.ImageFile));
                Result = _DAL.InsertData("sp_update_profile", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update + "sp_update_profile");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Update, ActivityLog.ActivityDetails_Update2 + "sp_update_profile");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest(ex.Message);
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }
        #endregion

        #region Organization Chart

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            DataTable dt = new DataTable();
            try
            {
                dt = _DAL.GetData("sp_select_allusers", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_allusers");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_allusers");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                BadRequest(ex.Message);
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SaveOrganizationChart([FromBody] OrganizationChart chart)
        {
            bool Result = false;
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("organizationcode-VARCHAR", InputSanitizer.Sanitize(chart.organizationname));
                nv.Add("organizationname-VARCHAR", InputSanitizer.Sanitize(chart.organizationname));
                nv.Add("isactive-BIT", chart.isactive == true ? "1" : "0");
                nv.Add("employeeids-VARCHAR", string.Join(",", chart.employeeid));
                nv.Add("managerid-INT", chart.managerid.ToString());
                nv.Add("createdby-INT", chart.createdby.ToString());
                nv.Add("isedit-BIT", chart.isedit.ToString());

                Result = _DAL.InsertData("sp_insert_organizationchart", nv, _DAL.CSManagementPortalDatabase);
                nv = null;

                if (Result)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + "sp_insert_organizationchart");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + "sp_insert_organizationchart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your System Adminsitrator");
            }

            if (Result)
            {
                return Ok(Result);
            }
            else
            {
                return BadRequest(Result);
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetOrganizationChart()
        {
            DataTable dt = new DataTable();

            try
            {
                dt = _DAL.GetData("sp_select_organizationchart", null, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_organizationchart");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_organizationchart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your System Adminsitrator");
            }
            return Ok(dt);
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetEmployeeListByManagerID(int managerid)
        {
            DataTable dt = new DataTable();

            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("managerid-INT", managerid.ToString());
                dt = _DAL.GetData("sp_select_employeelistbymanagerid", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_organizationchart");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_organizationchart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your System Adminsitrator");
            }
            return Ok(dt);
        }
        #endregion

        #region Vendors

        [HttpPost]
        public async Task<IActionResult> SendInvitationToVendors([FromBody] InviteVendors data)
        {
            if (data?.Emails == null || !data.Emails.Any())
            {
                return BadRequest(new { success = false, message = "No email addresses provided." });
            }

            var results = new
            {
                Sent = new List<string>(),
                AlreadyRegistered = new List<string>(),
                InvalidFormat = new List<string>(),
                Failed = new List<string>()
            };

            var uniqueEmails = data.Emails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();

            foreach (var email in uniqueEmails)
            {
                try
                {
                    if (!IsValidEmail(email))
                    {
                        results.InvalidFormat.Add(email);
                        continue;
                    }

                    NameValueCollection nvCheck = new NameValueCollection { { "@Email-VARCHAR", email } };
                    DataTable dtCheck = _DAL.GetData("sp_CheckVendorEmailExists", nvCheck, _DAL.CSManagementPortalDatabase);
                    bool alreadyExists = dtCheck != null && dtCheck.Rows.Count > 0 && dtCheck.Rows[0]["IsExists"].ToString() == "1";

                    if (alreadyExists)
                    {
                        results.AlreadyRegistered.Add(email);
                        continue;
                    }

                    var token = Guid.NewGuid().ToString("N");
                    NameValueCollection nvSave = new NameValueCollection
                    {
                        { "@Email-VARCHAR", email },
                        { "@Token-VARCHAR", token },
                        { "@ExpiryDate-DATETIME", DateTime.UtcNow.AddHours(24).ToString("yyyy-MM-dd HH:mm:ss") }
                    };

                    if (_DAL.InsertData("sp_SaveVendorInvitationToken", nvSave, _DAL.CSManagementPortalDatabase))
                    {
                        string? baseUrl = _configuration["AM_TARGET_APP"];
                        string registrationLink = $"{baseUrl?.TrimEnd('/')}/VendorRegister/VendorRegister?token={token}";
                        string emailBody = ActivityLog.EmailBodyForInviteVendor(registrationLink);

                        bool emailSent = await Task.Run(() =>
                            _sendemail.SendEmailToVendors(new List<string> { email }, emailBody, ActivityLog.EmailSubjectForVendorSupplier, _DAL)
                        );

                        if (emailSent)
                        {
                            results.Sent.Add(email);
                            _DAL.InsertData("sp_UpdateVendorInvitationStatus", new NameValueCollection { { "@Token-VARCHAR", token }, { "@Status-VARCHAR", "Sent" } }, _DAL.CSManagementPortalDatabase);
                        }
                        else
                        {
                            results.Failed.Add($"{email} (SMTP Error)");
                        }
                    }
                    else
                    {
                        results.Failed.Add($"{email} (Database Error)");
                    }
                }
                catch (Exception ex)
                {
                    results.Failed.Add($"{email} ({ex.Message})");
                }
            }

            bool processExecuted = results.Sent.Any() || results.AlreadyRegistered.Any() || results.InvalidFormat.Any();

            bool isPartial = results.Sent.Any() && (results.AlreadyRegistered.Any() || results.InvalidFormat.Any() || results.Failed.Any());
            return Ok(new
            {
                success = processExecuted,
                isPartial = isPartial,
                message = processExecuted ? "Invitation process completed." : "Failed to process invitations.",
                details = results
            });
        }
        private bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch { return false; }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetVendors()
        {
            try
            {
                var nv = new NameValueCollection();
                DataTable dt = _DAL.GetData("sp_select_vendors", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    return Ok(new { data = dt });
                }
                else
                {
                    return Ok(new { data = new List<object>() });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in GetVendors: {0}", ex.Message);
                return BadRequest("Something Went Wrong.");
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpPost]
        public IActionResult UpdateVendorRegistrationStatus([FromBody] VendorStatusUpdateDto data)
        {
            if (data == null) return BadRequest("Invalid payload.");

            try
            {
                string vendorEmail = "";
                string vendorName = "";
                string newUserId = "";

                if (data.Status == "Approved")
                {
                    DataTable dt = _DAL.GetData("sp_get_vendor_details", new NameValueCollection { { "VendorID-INT", data.VendorId.ToString() } }, _DAL.CSManagementPortalDatabase);

                    if (dt?.Rows.Count > 0)
                    {
                        var r = dt.Rows[0];
                        vendorEmail = r["UserEmail"]?.ToString() ?? "";
                        vendorName = r["UserName"]?.ToString() ?? "Partner";
                        var userResult = SaveUsers(new User
                        {
                            UserName = r["UserName"]?.ToString() ?? "",
                            UserEmail = r["UserEmail"]?.ToString() ?? "",
                            LoginName = r["LoginName"]?.ToString() ?? "",
                            MobileNumber = r["MobileNumber"]?.ToString() ?? "",
                            Designation = r["Designation"]?.ToString() ?? "",
                            Department = r["Department"]?.ToString() ?? "",

                            RoleID = "1017",
                            UserTypeID = "4",
                            IsActive = "on",               
                            IsAdUser = null,               
                            FormID = "1046",
                            EditUserID = data.UpdatedBy.ToString()
                        });

                        if (userResult is OkObjectResult okResult)
                        {
                            DataTable resultData = (DataTable)okResult.Value;
                            newUserId = resultData.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            return BadRequest(new { status = "Error", message = "Account creation failed." });
                        }
                    }
                    else return BadRequest("Vendor details not found.");
                }

                _DAL.GetData("sp_update_vendor_status", new NameValueCollection
                {
                    { "VendorID-INT", data.VendorId.ToString() },
                    { "Status-VARCHAR", data.Status },
                    { "UpdatedBy-VARCHAR", data.UpdatedBy.ToString() },
                    { "UserID-INT", newUserId }
                }, _DAL.CSManagementPortalDatabase);

                string emailBody = "";
                string subject = $"Bank Registration {data.Status}";

                if (data.Status == "Approved")
                    emailBody = ActivityLog.EmailBodyForVendorApproval(vendorName);
                else if (data.Status == "Rejected")
                    emailBody = ActivityLog.EmailBodyForVendorRejection(vendorName);

                if (!string.IsNullOrEmpty(emailBody) && !string.IsNullOrEmpty(vendorEmail))
                {
                    _sendemail.SendEmailToVendors(new List<string> { vendorEmail }, emailBody, subject, _DAL);

                }
                return Ok(new { status = "Success", message = $"Vendor {data.Status} successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "Error", message = ex.Message });
            }
        }

        #endregion

    }
}
