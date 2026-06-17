using KPKflowApi.Context;
using KPKflowApi.Controllers;
using KPKflowApi.Extensions;
using KPKflowApi.Models.Settings;
using KPKflowApi.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Data;
using System.Web;

[ApiController]
[Route("api/[controller]")]
public class VendorRegisterController : ControllerBase
{
    private readonly DataAccessLayer _DAL;
    private readonly SendEmail _sendemail;
    private readonly ILogger<SettingsController> _logger;
    private readonly RandomStringGenerator _randomstringgenerator;
    private readonly CommonMethods _CommonMethods;

    public VendorRegisterController(DataAccessLayer DAL, ILogger<SettingsController> logger, SendEmail sendemail,  RandomStringGenerator randomstringgenerator, CommonMethods commonMethods)
    {
        _DAL = DAL;
        _logger = logger;
        _sendemail = sendemail;
        _randomstringgenerator = randomstringgenerator;
        _CommonMethods = commonMethods;
    }

    [AllowAnonymous]
    [HttpGet("ValidateVendorToken")]
    public IActionResult ValidateVendorToken([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { success = false, message = "Token is required." });

        try
        {
            var nv = new NameValueCollection { { "@Token-VARCHAR", token } };
            DataTable dt = _DAL.GetData("sp_ValidateVendorToken", nv, _DAL.CSManagementPortalDatabase);

            if (dt?.Rows.Count > 0)
            {
                int status = Convert.ToInt32(dt.Rows[0]["Status"]);
                string email = dt.Rows[0]["Email"].ToString();

                var result = status switch
                {
                    1 => (Success: true, Msg: "Valid Token"),
                    2 => (Success: false, Msg: "This link has expired."),
                    3 => (Success: false, Msg: "This link has been used multiple times."),
                    4 => (Success: false, Msg: "You have already registered using this link."), 
                    _ => (Success: false, Msg: "Invalid or unauthorized token.")
                };

                if (status == 1)
                    _DAL.InsertData("sp_MarkVendorTokenUsed", nv, _DAL.CSManagementPortalDatabase);

                return Ok(new { success = (status == 1), message = result.Msg, email = email });
            }
            return Ok(new { success = false, message = "Invalid or unauthorized token." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValidateVendorToken");
            return StatusCode(500, "Internal Server Error");
        }
    }


    [HttpGet("GetCategories")]
    public IActionResult GetCategories()
    {
        try
        {
            var nv = new NameValueCollection();
            DataTable dt = _DAL.GetData("SP_Dropdown_GetCategories", nv, _DAL.CSManagementPortalDatabase);

            var list = dt.AsEnumerable().Select(row => new {
                Id = row["Id"],
                Text = row["Text"]
            }).ToList();

            if (list.Any())
            {
                return Ok(new { success = true, data = list });
            }

            return Ok(new { success = false, message = "No categories found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCategories");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("GetSubCategories")]
    public IActionResult GetSubCategories([FromQuery] string CategoryId)
    {
        if (string.IsNullOrEmpty(CategoryId))
            return BadRequest(new { success = false, message = "CategoryId is required." });

        try
        {
            var nv = new NameValueCollection { { "@CategoryId-INT", CategoryId } };
            DataTable dt = _DAL.GetData("SP_Dropdown_GetSubCategories", nv, _DAL.CSManagementPortalDatabase);

            var list = dt.AsEnumerable().Select(row => new {
                Id = row["Id"],
                Text = row["Text"]
            }).ToList();

            if (list.Any())
            {
                return Ok(new { success = true, data = list });
            }

            return Ok(new { success = false, message = "No sub-categories found for this category." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSubCategories");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }



    [HttpPost("InitiateRegistration")]
    public IActionResult InitiateRegistration([FromBody] VendorRegistrationModel model)
    {
        if (model == null || model.User == null)
            return Ok(new { status = "error", message = "Invalid data submitted." });

        NameValueCollection nvCheck = new NameValueCollection();
        nvCheck.Add("Email-VARCHAR", model.User.UserEmail);
        nvCheck.Add("Mobile-VARCHAR", model.User.MobileNumber);
        nvCheck.Add("LoginName-VARCHAR", model.User.LoginName);

        DataTable dtCheck = _DAL.GetData("sp_CheckVendorExistence", nvCheck, _DAL.CSManagementPortalDatabase);
        if (dtCheck?.Rows.Count > 0 && dtCheck.Rows[0]["Conflict"].ToString() != "None")
            return Ok(new { status = "error", message = dtCheck.Rows[0]["Message"].ToString() });

        string emailOtp = new Random().Next(100000, 999999).ToString();
        string token = model.InvitationToken;

        string jsonData = JsonConvert.SerializeObject(model);

        NameValueCollection nv = new NameValueCollection();
        nv.Add("RegistrationToken-NVARCHAR", token);
        nv.Add("SerializedData-VARCHAR", jsonData);
        nv.Add("EmailOTP-VARCHAR", emailOtp);
        nv.Add("MobileOTP-VARCHAR", "1234"); 

        _DAL.InsertData("sp_save_temp_registration", nv, _DAL.CSManagementPortalDatabase);

        string emailBody = ActivityLog.EmailBodyForVerification(emailOtp);

        _sendemail.SendEmailToVendors(new List<string> { model.User.UserEmail }, emailBody, "Action Required: Verify Your Email", _DAL);

        return Ok(new { status = "success", token = token });
    }

    [HttpPost("FinalRegister")]
    public IActionResult FinalRegister([FromBody] FinalVerificationModel model)
    {
        try
        {
            if (model == null || string.IsNullOrEmpty(model.RegistrationToken))
            {
                return Ok(new { status = "error", message = "Registration token is missing." });
            }

            if (string.IsNullOrEmpty(model.EmailOTP) || string.IsNullOrEmpty(model.MobileOTP))
            {
                return Ok(new { status = "error", message = "Please enter both Email and Mobile OTPs." });
            }

            NameValueCollection nv = new NameValueCollection();
            nv.Add("Token-NVARCHAR", model.RegistrationToken);
            nv.Add("EmailOTP-VARCHAR", model.EmailOTP);
            nv.Add("MobileOTP-VARCHAR", model.MobileOTP);

            DataTable dt = _DAL.GetData("sp_verify_otp", nv, _DAL.CSManagementPortalDatabase);

            if (dt != null && dt.Rows.Count > 0)
            {
                string status = dt.Rows[0]["Status"].ToString();
                string userEmail = dt.Rows[0]["UserEmail"].ToString();

                switch (status)
                {
                    case "INVALID_TOKEN":
                        return Ok(new { status = "error", message = "Invalid or missing registration session." });

                    case "ALREADY_USED":
                        return Ok(new { status = "error", message = "This OTP has already been used. Please start over." });

                    case "EXPIRED":
                        return Ok(new { status = "error", message = "Your OTP has expired. Please request a new one." });

                    case "MAX_ATTEMPTS_REACHED":
                        return Ok(new { status = "error", message = "Maximum attempts reached. This OTP is now invalid. Please request a new one." });

                    case "BOTH_OTP_WRONG":
                        return Ok(new { status = "error", message = "Both Email and Mobile OTPs are incorrect." });

                    case "EMAIL_OTP_WRONG":
                        return Ok(new { status = "error", message = "The Email OTP you entered is incorrect." });

                    case "MOBILE_OTP_WRONG":
                        return Ok(new { status = "error", message = "The Mobile OTP you entered is incorrect." });

                    case "SUCCESS":
                        NameValueCollection nvFinal = new NameValueCollection();
                        nvFinal.Add("Token-NVARCHAR", model.RegistrationToken);

                        DataTable dtFinal = _DAL.GetData("sp_FinalizeVendorRegistration", nvFinal, _DAL.CSManagementPortalDatabase);

                        if (dtFinal != null && dtFinal.Rows.Count > 0 && dtFinal.Rows[0]["Status"].ToString() == "SUCCESS")
                        {
                            string emailBody = ActivityLog.EmailBodyForSuccessRegistration();
                            _sendemail.SendEmailToVendors(new List<string> { userEmail }, emailBody, "Registration Received - Pending Approval", _DAL);

                            return Ok(new { status = "success", message = "Registration submitted successfully!" });
                        }
                        else
                        {
                            return Ok(new { status = "error", message = "OTP verified but registration finalization failed." });
                        }

                    default:
                        return Ok(new { status = "error", message = "An unexpected error occurred during verification." });
                }
            }

            return Ok(new { status = "error", message = "Could not verify OTP. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"FinalRegister Error: {ex.Message}");
            return Ok(new { status = "error", message = "An internal server error occurred." });
        }
    }

    [HttpPost("ResendOTP")]
    public IActionResult ResendOTP([FromBody] dynamic data)
    {
        string token = data.RegistrationToken;
        string newEmailOtp = new Random().Next(100000, 999999).ToString();
        string newMobileOtp = "1234"; 

        NameValueCollection nv = new NameValueCollection();
        nv.Add("Token-NVARCHAR", token);
        nv.Add("NewEmailOTP-VARCHAR", newEmailOtp);
        nv.Add("NewMobileOTP-VARCHAR", newMobileOtp);

        DataTable dt = _DAL.GetData("sp_resend_vendor_otp", nv, _DAL.CSManagementPortalDatabase);

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["Status"].ToString() == "SUCCESS")
        {
            string userEmail = dt.Rows[0]["UserEmail"].ToString();

            string emailBody = ActivityLog.EmailBodyForResendOtp(newEmailOtp);
            _sendemail.SendEmailToVendors(new List<string> { userEmail }, emailBody, "New OTP Requested", _DAL);

            return Ok(new { status = "success", message = "New OTP sent successfully!" });
        }

        return Ok(new { status = "error", message = "Resend failed. Please try again." });
    }

}