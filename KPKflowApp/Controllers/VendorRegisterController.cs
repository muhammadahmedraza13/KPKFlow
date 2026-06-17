using KPKflowApp.Middleware;
using KPKflowApp.Models.Authentication;
using KPKflowApp.Models.Base;
using KPKflowApp.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace KPKflowApp.Controllers
{
    public class VendorRegisterController : Controller
    {
        private readonly RequestClient _requestClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public VendorRegisterController(RequestClient requestClient, IWebHostEnvironment webHostEnvironment)
        {
            _requestClient = requestClient;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> VendorRegister(string token)
        {
            if (string.IsNullOrEmpty(token)) return Content("Token is missing!");

            HttpResponseMessage response = _requestClient.UseHttpClientGetPublic($"?token={token}", "ValidateVendorToken", "VendorRegister");

            string errorMessage = "Access Denied: Link is invalid or expired.";

            if (response != null && response.IsSuccessStatusCode)
            {
                var contentTask = response.Content.ReadAsStringAsync();
                contentTask.Wait(); 

                var data = JsonConvert.DeserializeObject<dynamic>(contentTask.Result);

                if (data?.success == true)
                {
                    ViewBag.Token = token;
                    ViewBag.UserEmail = data?.email;
                    return View();
                }
                else
                {
                    errorMessage = data?.message ?? errorMessage;
                }
            }
            else
            {
                errorMessage = response != null ? $"API Error: {response.StatusCode}" : "Service Unavailable";
            }

            return Content($"<div style='text-align:center;'><h2>Access Denied</h2><p>{errorMessage}</p></div>", "text/html");
        }

        [HttpGet]
        public IActionResult GetCategories()
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGetPublic(null, "GetCategories", "VendorRegister");
            return new HttpResponseMessageResult(response);
        }

        [HttpGet]
        public IActionResult GetSubCategories(string CategoryId)
        {
            HttpResponseMessage response = _requestClient.UseHttpClientGetPublic("?CategoryId=" + CategoryId, "GetSubCategories", "VendorRegister");
            return new HttpResponseMessageResult(response);
        }


        [HttpPost]
        public IActionResult InitiateRegistration([FromForm] VendorRegistrationModel model, IFormFile TaxDocument, IFormFile CompanyProfile, IFormFile AdditionalDocs)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { status = "error", message = errors });
            }

            try
            {
                var allowedDocs = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".docx" };
                var fileSizeLimit = 10 * 1024 * 1024;

                if (!IsFileValid(TaxDocument, allowedDocs, fileSizeLimit))
                    return Json(new { status = "error", message = "Invalid Tax Document" });

                if (!IsFileValid(CompanyProfile, new[] { ".pdf", ".docx" }, fileSizeLimit))
                    return Json(new { status = "error", message = "Invalid Company Profile" });

                if (AdditionalDocs != null && !IsFileValid(AdditionalDocs, allowedDocs, fileSizeLimit))
                    return Json(new { status = "error", message = "Invalid Additional Document" });

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads/VendorDocs");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePrefix = $"{model.InvitationToken}_{DateTime.Now:yyyyMMddHHmmss}";

                model.TaxDocumentPath = SaveFile(TaxDocument, uploadsFolder, "Tax_" + filePrefix);
                model.CompanyProfilePath = SaveFile(CompanyProfile, uploadsFolder, "Profile_" + filePrefix);
                model.AdditionalDocsPath = SaveFile(AdditionalDocs, uploadsFolder, "Extra_" + filePrefix);

                string jsonString = JsonConvert.SerializeObject(model);

                var response = _requestClient.UseHttpClientPostPublic(jsonString, "InitiateRegistration", "VendorRegister");

                return new HttpResponseMessageResult(response);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "File upload failed: " + ex.Message });
            }
        }
        private bool IsFileValid(IFormFile file, string[] allowedExtensions, int maxSize)
        {
            if (file == null) return true; 
            var ext = Path.GetExtension(file.FileName).ToLower();
            return allowedExtensions.Contains(ext) && file.Length <= maxSize;
        }
        private string SaveFile(IFormFile file, string folder, string customName)
        {
            if (file == null) return null;

            string extension = Path.GetExtension(file.FileName);
            string uniqueName = customName + extension;
            string filePath = Path.Combine(folder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return uniqueName;
        }

        [HttpPost]
        public IActionResult FinalRegister([FromForm] FinalVerificationModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { status = "error", message = "Invalid OTP or Token" });

            string jsonString = JsonConvert.SerializeObject(model);
            var response = _requestClient.UseHttpClientPostPublic(jsonString, "FinalRegister", "VendorRegister");

            return new HttpResponseMessageResult(response);
        }

        [HttpPost]
        public IActionResult ResendOTP([FromForm] string RegistrationToken)
        {
            if (string.IsNullOrEmpty(RegistrationToken))
                return Json(new { status = "error", message = "Invalid Session" });

            var response = _requestClient.UseHttpClientPostPublic(JsonConvert.SerializeObject(new { RegistrationToken = RegistrationToken }),"ResendOTP", "VendorRegister");
            return new HttpResponseMessageResult(response);
        }
    }
}
