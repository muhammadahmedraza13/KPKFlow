using KPKflowApp.Models.Base;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using KPKflowApp.Utility;
using KPKflowApp.Models.Authentication;
using Microsoft.AspNetCore.Authorization;
using KPKflowApp.Extensions;
using KPKflowApp.Models.FileUpload;
using System.Reflection;
using KPKflowApi.Models.Workflow;
using KPKflowApp.Middleware;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KPKflowApp.Controllers
{
    [Authorize(AuthenticationSchemes = "ASPXAUTH")]
    public class BaseController : Controller
    {
        private readonly RequestClient _requestClient;
        private readonly ILogger<BaseController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly DataEncryptor _dataEncryptor;
        public BaseController(RequestClient requestClient, IConfiguration config, ILogger<BaseController> logger, DataEncryptor dataEncryptor, IWebHostEnvironment environment)
        {
            _requestClient = requestClient;
            _logger = logger;
            _dataEncryptor = dataEncryptor;
            _environment = environment;
        }



        #region Session
        [HttpGet]
        public List<RolesMapping> GetSessionForHtml(string RouteValues)
        {
            List<RolesMapping> tlistFiltered = new List<RolesMapping>();

            try
            {
                tlistFiltered = _requestClient.GetSessionForHtml(RouteValues);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("BaseController/GetSession {1}", ex.Message);
            }

            return tlistFiltered;
        }
        #endregion
      
        #region Settings

        #region UserMaster

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetUserType()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID, "GetUserType", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetUsers()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID + "&UserID=" + _info.UserID, "GetUsersByType", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetForms()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetForms", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetUserRoles()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetRoles", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetMapping(string RoleId)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?RoleId=" + RoleId, "GetMapping", "Settings");
            return new HttpResponseMessageResult(response);
        }


        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult FetchAdUsers()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "FetchAdUsers", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult EditUsers([FromForm] User Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.EditUserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "EditUsers", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveUsers([FromForm] User Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            if (userinfo.UserTypeID.ToString() == Data.UserTypeID) return BadRequest("Cannot Create Account For Type " + userinfo.UserType);
            
            Data.EditUserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveUsers", "Settings");
            
            return new HttpResponseMessageResult(response);

        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteUsers([FromBody] DeleteFromDB Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.EditUserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "DeleteUsers", "Settings");
            return new HttpResponseMessageResult(response);
        }

        #endregion

        #region RoleMaster

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetRoles()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetRoles", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetRolesByID(string RoleID)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?RoleID=" + RoleID, "GetRolesByID", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult EditRoles([FromForm] Role Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "EditRoles", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveRoles([FromForm] Role Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveRoles", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteRoles([FromBody] DeleteFromDB Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "DeleteRoles", "Settings");
            return new HttpResponseMessageResult(response);
        }
        #endregion

        #region SystemPriviliges

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetMapping_2(string RoleId)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?RoleId=" + RoleId, "GetMapping_2", "Settings");
            return new HttpResponseMessageResult(response);
        }
        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult UpdateRolesMapping([FromBody] RoleMapping Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "UpdateRolesMapping", "Settings");
            return new HttpResponseMessageResult(response);
        }

        #endregion

        #region SmtpSetting
        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SmtpSettingUpdate([FromBody] SMTPSettings smtpSetting)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            
            string encryptpassword = _dataEncryptor.EncryptPassword(smtpSetting.SmtpPassword);
            smtpSetting.SmtpPassword = encryptpassword;
            string jsonString = JsonConvert.SerializeObject(smtpSetting);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SmtpSettingUpdate", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SmtpSettingGet()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "SmtpSettingGet", "Settings");
            return new HttpResponseMessageResult(response);
        }
        #endregion

        #region UserProfile
        [TypeFilter(typeof(AllowedExtensionsAttribute))]
        //[TypeFilter(typeof(MaxFileSizeAttribute))]
        [TypeFilter(typeof(AllowedApiAccess))]
        [HttpPost]
        public IActionResult UpdateProfile([FromForm] UserProfile profile)
        {


            string fname_partial = null;
            string wwwPath = this._environment.WebRootPath;
            string contentPath = this._environment.ContentRootPath;


            if (profile._ImageFile != null)
            {
                string fname = null;

                if (Request.Headers["User-Agent"].ToString().ToUpper() == "IE" || Request.Headers["User-Agent"].ToString().ToUpper() == "INTERNETEXPLORER")
                {
                    string[] testfiles = profile._ImageFile.FileName.Split(new char[] { '\\' });
                    fname = testfiles[testfiles.Length - 1];
                }
                else
                {
                    fname = profile._ImageFile.FileName;
                }

                string extension = Path.GetExtension(profile._ImageFile.FileName);

                fname_partial = DateTime.Now.Millisecond.ToString() + "_" + extension;

                fname = Path.Combine(this._environment.WebRootPath + "\\Public\\image\\UserImage", fname_partial);

                using (FileStream stream = new FileStream(fname, FileMode.Create))
                {
                    profile._ImageFile.CopyTo(stream);
                    _logger.LogInformation("{0} {1} {2}", MethodBase.GetCurrentMethod().Name, fname, "FileSaved");
                }
            }

            profile.ImageFile = fname_partial;

            string jsonString = JsonConvert.SerializeObject(profile);

            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "UpdateProfile", "Settings");

            return new HttpResponseMessageResult(response);

        }

        #endregion

        #region Organization Chart

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetAllUsers()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetAllUsers", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveOrganizationChart([FromForm] OrganizationChart Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.createdby = userinfo.UserID;
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveOrganizationChart", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetOrganizationChart()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetOrganizationChart", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetEmployeeListByManagerID(int managerid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?managerid=" + managerid, "GetEmployeeListByManagerID", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetOrganizationHierarchy()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetOrganizationHierarchy", "Settings");
            return new HttpResponseMessageResult(response);
        }
        #endregion
        #endregion

        #region Data
        #region Instrument
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetInstruments()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID, "GetInstruments", "Data");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetInstrumentTypes()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID, "GetInstrumentTypes", "Data");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetTenures()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID, "GetTenures", "Data");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetFrequency()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID, "Frequency", "Data");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetBenchmark()
        {
            UserInfo _info = _requestClient.GetUserInformation();

            HttpResponseMessage response = _requestClient.UseHttpClientGet("?TypeID=" + _info.UserTypeID, "GetBenchmark", "Data");
            return new HttpResponseMessageResult(response);
        }
        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult EditInstrument([FromForm] Role Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "EditInstrument", "Data");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveInstrument([FromForm] Role Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveInstrument", "Data");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteInstrument([FromBody] DeleteFromDB Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.UserID = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "DeleteInstrument", "Data");
            return new HttpResponseMessageResult(response);
        }
        #endregion
        #endregion

        #region Workflow

        #region WorkflowMethod


        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflow()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetWorkflow", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowByID(string workflowid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?workflowid=" + workflowid, "GetWorkflowByID", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult EditWorkflow([FromForm] Workflow workflow)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            workflow.editdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(workflow);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "EditWorkflow", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveWorkflow([FromForm] Workflow workflow)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            workflow.createdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(workflow);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveWorkflow", "Workflow");
            return new HttpResponseMessageResult(response);
        }



        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteWorkflow([FromBody] Step step)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            step.editdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(step);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "DeleteWorkflow", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult MyRequest(string wfcode)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&roleid=" + userinfo.RoleID + "&userid=" + userinfo.UserID, "MyRequest", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult MyApproval(string wfcode)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&roleid=" + userinfo.RoleID + "&userid=" + userinfo.UserID, "MyApproval", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult MyTask(string wfcode)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&roleid=" + userinfo.RoleID + "&userid=" + userinfo.UserID, "MyTask", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult AllRequests(string wfcode)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&roleid=" + userinfo.RoleID + "&userid=" + userinfo.UserID, "AllRequests", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult IsTaskAllowed(int instanceid)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?instanceid=" + instanceid + "&roleid=" + userinfo.RoleID + "&userid=" + userinfo.UserID, "IsTaskAllowed", "Workflow");
            return new HttpResponseMessageResult(response);

        }

    #endregion

        #region  WorkflowStep

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetApprovalType()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetApprovalType", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveWorkflowStep([FromForm] Step step)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            step.createdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(step);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveWorkflowStep", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult UpdateWorkflowStep([FromForm] Step step)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            step.editdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(step);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "UpdateWorkflowStep", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteWorkflowStep([FromBody] Step step)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            step.editdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(step);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "DeleteWorkflowStep", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowStep()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetWorkflowStep", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowStepsByID(string workflowstepid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?workflowstepid=" + workflowstepid, "GetWorkflowStepsByID", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetSectionPermission(int workflowid, int stepid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet($"?workflowid={workflowid}&stepid={stepid}", "GetSectionPermission", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult PerformVisibleTask([FromBody] SectionPermission section)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            section.editby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(section);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "PerformVisibleTask", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult PerformEnableTask([FromBody] SectionPermission section)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            section.editby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(section);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "PerformEnableTask", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        #endregion

        #region WorkFlowStepAction

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveWorkflowStepAction([FromForm] Models.Base.Action action)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            action.createdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(action);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveWorkflowStepAction", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult UpdateWorkflowStepAction([FromForm] Models.Base.Action action)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            action.editdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(action);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "UpdateWorkflowStepAction", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteWorkflowStepAction([FromBody] Models.Base.Action action)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            action.editdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(action);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "DeleteWorkflowStepAction", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowStepAction()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetWorkflowStepAction", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowStepsActionByID(string workflowstepid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?workflowstepid=" + workflowstepid, "GetWorkflowStepsActionByID", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowCode()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetWorkflowCode", "Workflow");
            return new HttpResponseMessageResult(response);
        }


        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetDynamicFunction()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetDynamicFunction", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowStepbyWorkflowId(int workflowid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?workflowid=" + workflowid, "GetWorkflowStepbyWorkflowId", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        #endregion

        #region Generic Workflow
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetFormsByInstanceId(string wfcode, int instanceid, string formName)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&instanceid=" + instanceid + "&formName=" + formName, "GetFormsByInstanceId", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetCurrentWorkflowAction(string wfcode, int instanceid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&instanceid=" + instanceid, "GetCurrentWorkflowAction", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetWorkflowLog(int instanceid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?instanceid=" + instanceid, "GetWorkflowLog", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult MoveWorkflow([FromForm] WorkflowMove move)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            move.userid = userinfo.UserID;
            string jsonString = JsonConvert.SerializeObject(move);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "MoveWorkflow", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveSelfAssignUser([FromForm] SelfAssign selfAssign)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            selfAssign.createdby = userinfo.UserID.ToString();
            string jsonString = JsonConvert.SerializeObject(selfAssign);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveSelfAssignUser", "Workflow");
            return new HttpResponseMessageResult(response);
        }     
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetSelfAssignUser(int instanceid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?instanceid=" + instanceid, "GetSelfAssignUser", "Workflow");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult DeleteSelfAssignUser(int primaryid, int instanceid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?primaryid=" + primaryid + "&instanceid=" + instanceid, "DeleteSelfAssignUser", "Workflow");
            return new HttpResponseMessageResult(response);
        }

        #endregion

        #region TaskDashboard
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetDailyTasks()
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?userid=" + userinfo.UserID + "&roleid=" + userinfo.RoleID, "GetDailyTasks", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetDailyOverdueTasks()
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?userid=" + userinfo.UserID, "GetDailyOverdueTasks", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetDailyMyRequests()
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?userid=" + userinfo.UserID, "GetDailyMyRequests", "Workflow");
            return new HttpResponseMessageResult(response);

        }      
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetDailyOverdueMyRequests()
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?userid=" + userinfo.UserID, "GetDailyOverdueMyRequests", "Workflow");
            return new HttpResponseMessageResult(response);

        }        
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetDailyMyApprovals()
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?userid=" + userinfo.UserID, "GetDailyMyApprovals", "Workflow");
            return new HttpResponseMessageResult(response);

        }
        #endregion

        #endregion

        #region Budget

        [HttpPost]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult SaveInitiateRequest([FromForm] Budget Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.userid = userinfo.UserID;
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveInitiateRequest", "Budget");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetBudgetDetailByInstanceId(int instanceid)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?instanceid=" + instanceid, "GetBudgetDetailByInstanceId", "Budget");
            return new HttpResponseMessageResult(response);
        }

        #endregion

        #region GetUserDetails
        [HttpGet]
        [TypeFilter(typeof(AllowedApiAccess))]
        public IActionResult GetUserDetails()
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            return Ok(userinfo);
        }
        #endregion



        [HttpGet]
        public IActionResult DownloadFile(string fileName)
        {
            string[] folders = { "VendorDocs", "purchaserequest_files", "RFQIssuance_files", "RFQSubmission_files", "PurchaseOrder_files", "QAQC_Inspections", "PaymentsDocs" };

            var filePath = folders
                .Select(f => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", f, fileName))
                .FirstOrDefault(System.IO.File.Exists);

            if (filePath == null) return Content("File not found.");

            string contentType = fileName.EndsWith(".pdf") ? "application/pdf" :
                                 (fileName.EndsWith(".jpg") || fileName.EndsWith(".png") ? "image/jpeg" : "application/octet-stream");

            return File(System.IO.File.ReadAllBytes(filePath), contentType, fileName);
        }

        #region Vendor

        [HttpPost]
        public async Task<IActionResult> SendInvitationtoVendors([FromForm] InviteVendors Data)
        {
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SendInvitationtoVendors", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        public IActionResult GetVendors()
        {

            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetVendors", "Settings");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public IActionResult UpdateVendorStatus(int VendorId, string Status)
        {
            UserInfo _info = _requestClient.GetUserInformation();
            var model = new VendorStatusUpdateDto
            {
                VendorId = VendorId,
                Status = Status,
                UpdatedBy = _info.UserID
            };
            string jsonString = JsonConvert.SerializeObject(model);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "UpdateVendorRegistrationStatus", "Settings");
            return new HttpResponseMessageResult(response);
        }

        #endregion

        #region PurchaseRequest

        [HttpGet]
        public IActionResult GetPurchaseRequestItems()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetPurchaseRequestItems", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public IActionResult GetVendorsbyCategories([FromForm] CategoryByInstance Data)
        {
            string jsonString = JsonConvert.SerializeObject(Data);
            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "GetVendorsbyCategories", "PurchaseRequest");

            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> SavePurchaseInitiateRequest([FromForm] PurchaseRequest Data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (Data.items == null || !Data.items.Any()) return BadRequest("Items list cannot be empty.");

            try
            {
                UserInfo userinfo = _requestClient.GetUserInformation();
                Data.userid = userinfo.UserID;

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/purchaserequest_files");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var item in Data.items)
                {
                    if (item.file != null && item.file.Length > 0)
                    {
                        var extension = Path.GetExtension(item.file.FileName).ToLower();
                        var allowedExtensions = new[] { ".jpg", ".png", ".pdf", ".docx" };
                        if (!allowedExtensions.Contains(extension)) return BadRequest("Invalid file type.");

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + item.file.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await item.file.CopyToAsync(fileStream);
                        }
                        item.fileName = uniqueFileName;
                    }
                }

                string jsonString = JsonConvert.SerializeObject(Data);
                HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SavePurchaseInitiateRequest", "PurchaseRequest");

                return new HttpResponseMessageResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed");
                return StatusCode(500, "Internal Server Error during file processing");
            }
        }

        [HttpGet]
        public IActionResult GetPurchaseRequestDetailByInstanceId(int instanceid)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?instanceid=" + instanceid + "&userid=" + userinfo.UserID, "GetPurchaseRequestDetailByInstanceId", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        public IActionResult GetRFQsTasks(string wfcode)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?wfcode=" + wfcode + "&roleid=" + userinfo.RoleID + "&userid=" + userinfo.UserID, "GetRFQsTasks", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRFQIssuancetoVendorsRequest([FromForm] RFQIssuanceRequest data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            UserInfo userinfo = _requestClient.GetUserInformation();
            data.userid = userinfo.UserID;

            if (data.file != null && data.file.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/RFQIssuance_files");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(data.file.FileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".docx" };

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only PDF and DOCX files are allowed.");

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + data.file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await data.file.CopyToAsync(fileStream);
                }

                data.fileName = uniqueFileName;
            }

            string jsonString = JsonConvert.SerializeObject(data);

            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SubmitRFQIssuancetoVendorsRequest", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRFQsRequest([FromForm] RFQsRequest data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            UserInfo userinfo = _requestClient.GetUserInformation();
            data.userid = userinfo.UserID;

            if (data.file != null && data.file.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/RFQSubmission_files");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(data.file.FileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".docx" };

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only PDF and DOCX files are allowed.");

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + data.file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await data.file.CopyToAsync(fileStream);
                }

                data.fileName = uniqueFileName;
            }

            string jsonString = JsonConvert.SerializeObject(data);

            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SubmitRFQsRequest", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitVendorSelection([FromForm] VendorSelectionRequest data)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data submitted." });

            try
            {
                UserInfo userinfo = _requestClient.GetUserInformation();
                data.userid = userinfo.UserID;

                string jsonString = JsonConvert.SerializeObject(data);

                HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SubmitVendorSelection", "PurchaseRequest");
                return new HttpResponseMessageResult(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadPurchaseOrder([FromForm] UploadPO data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            data.userid = userinfo.UserID;

            if (data.File != null && data.File.Length > 0)
            {
                data.originalFileName = data.File.FileName;
                data.fileSize = data.File.Length;
                data.fileExtension = Path.GetExtension(data.File.FileName).ToLower();

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/PurchaseOrder_files");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var allowedExtensions = new[] { ".pdf" };
                if (!allowedExtensions.Contains(data.fileExtension))
                    return BadRequest("Only PDF files are allowed.");

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + data.originalFileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await data.File.CopyToAsync(fileStream);
                }

                data.fileName = uniqueFileName;
            }

            string jsonString = JsonConvert.SerializeObject(data);

            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "UploadPurchaseOrder", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public IActionResult SaveGateEntryRecordPurchaseRequest([FromForm] GateEntryRecord Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.userid = userinfo.UserID;
            string jsonString = JsonConvert.SerializeObject(Data);

            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveGateEntryRecordPurchaseRequest", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> SaveQAQCPurchaseRequest([FromForm] QAQCRequestModel data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            data.userid = _requestClient.GetUserInformation().UserID;
            var allowedExtensions = new[] { ".pdf", ".docx", ".jpg", ".png" };
            List<string> savedFiles = new();

            if (data.qaqcFiles?.Any() == true)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/QAQC_Inspections");
                Directory.CreateDirectory(folder); 

                foreach (var file in data.qaqcFiles.Where(f => allowedExtensions.Contains(Path.GetExtension(f.FileName).ToLower())))
                {
                    string name = $"{Guid.NewGuid()}_{file.FileName}";
                    using (var stream = new FileStream(Path.Combine(folder, name), FileMode.Create))
                        await file.CopyToAsync(stream);
                    savedFiles.Add(name);
                }
                data.fileName = string.Join(",", savedFiles);
            }

            string json = JsonConvert.SerializeObject(new
            {
                data.instanceid,
                data.VendorId,
                data.qaqcRemarks,
                data.fileName,
                data.userid
            });

            return new HttpResponseMessageResult(_requestClient.UseHttpClientPost(json, "SaveQAQCDetails", "PurchaseRequest"));
        }

        [HttpPost]
        public IActionResult SaveGRNRecordPurchaseRequest([FromForm] GRNRecord Data)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            Data.userid = userinfo.UserID;
            string jsonString = JsonConvert.SerializeObject(Data);

            HttpResponseMessage response = _requestClient.UseHttpClientPost(jsonString, "SaveGRNRecordPurchaseRequest", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> SavePaymentDetailsPurchaseRequest([FromForm] PaymentRequestModel data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            data.userid = _requestClient.GetUserInformation().UserID;
            var allowedExtensions = new[] { ".pdf", ".docx", ".jpg", ".png" };
            List<string> savedFiles = new();

            if (data.paymentAttachment?.Any() == true)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/PaymentsDocs");
                Directory.CreateDirectory(folder);

                foreach (var file in data.paymentAttachment.Where(f => allowedExtensions.Contains(Path.GetExtension(f.FileName).ToLower())))
                {
                    string name = $"{Guid.NewGuid()}_{file.FileName}";
                    using (var stream = new FileStream(Path.Combine(folder, name), FileMode.Create))
                        await file.CopyToAsync(stream);
                    savedFiles.Add(name);
                }
                data.fileName = string.Join(",", savedFiles);
            }

            string json = JsonConvert.SerializeObject(new
            {
                data.instanceid,
                data.amountReceived,
                data.paymentRemarks,
                data.fileName,
                data.userid
            });

            return new HttpResponseMessageResult(_requestClient.UseHttpClientPost(json, "SavePaymentDetailsPurchaseRequest", "PurchaseRequest"));
        }
       
        [HttpGet]
        public IActionResult GetFundName()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "GetFundName", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet]
        public IActionResult GetBankRateByInstanceId(int instanceid)
        {
            UserInfo userinfo = _requestClient.GetUserInformation();
            HttpResponseMessage response = _requestClient.UseHttpClientGet("?instanceid=" + instanceid, "GetBankRateByInstanceId", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }
      
        #endregion

        #region Procurement Console
        [HttpGet]
        public IActionResult AutoProcurement()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGet(null, "AutoProcurement", "PurchaseRequest");
            return new HttpResponseMessageResult(response);
        }
        #endregion
        #region RFQ Email
        [HttpGet]
        public IActionResult Matchthehighestbidder(int vendorid, string bankname, string bankemail, int instanceId, string[] rate)
        {
            // Array ko dynamic query string ke bajaye ek hi comma-separated string bana kar pass karenge
            UserInfo userinfo = _requestClient.GetUserInformation();
            int? userid = userinfo.UserID;
            var ratesJoined = rate != null ? string.Join(",", rate) : "";

            var queryParams = $"?vendorid={vendorid}&bankname={Uri.EscapeDataString(bankname)}&bankemail={Uri.EscapeDataString(bankemail)}&instanceId={instanceId}&requestBy={userid}&rate={Uri.EscapeDataString(ratesJoined)}";

            HttpResponseMessage response = _requestClient.UseHttpClientGet(
                queryParams,
                "Matchthehighestbidder",
                "PurchaseRequest"
            );

            return new HttpResponseMessageResult(response);
        }
        #endregion
    }

}
