using AngleSharp.Io;
using KPKflowApi.Context;
using KPKflowApi.Models.PurchaseRequest;
using KPKflowApi.RateLimiting;
using KPKflowApi.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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
    public partial class PurchaseRequestController(DataAccessLayer _DAL, ILogger<PurchaseRequestController> _logger , SendEmail _sendemail , IConfiguration _configuration) : ControllerBase
    {

        #region Activity Log
        public void SystemActivityLog(int? activityId, string? activityDetails)
        {
            bool result = false;

            try
            {
                ClaimsPrincipal? claimsPrincipal = HttpContext?.User;
                string? userId = claimsPrincipal?.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("UserID claim not found.");
                    return;
                }

                string hostName = Dns.GetHostName();
                string ipAddress = Dns.GetHostEntry(hostName).AddressList.FirstOrDefault()?.ToString() ?? "UnknownIP";

                var routeData = HttpContext?.Request?.RouteValues;
                string controllerName = routeData?["controller"]?.ToString() ?? "UnknownController";
                string actionName = routeData?["action"]?.ToString() ?? "UnknownAction";

                string formName = $"{controllerName}/{actionName}";
                string activityDetailsComplete = $"{ipAddress} {activityDetails ?? "NoDetails"} {formName}";

                var nv = new NameValueCollection
                {
                    ["FormID-INT"] = "0",
                    ["ActivityID-INT"] = activityId?.ToString() ?? "0",
                    ["UserID-INT"] = userId,
                    ["ActivityDetails-VARCHAR"] = activityDetailsComplete
                };

                result = _DAL.InsertData("sp_insert_activitylog", nv, _DAL.CSManagementPortalDatabase);

                _logger.LogInformation("{Controller} {Method} {Details}", controllerName, MethodBase.GetCurrentMethod()?.Name, activityDetailsComplete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Controller} {Method} with Details: {Details}",
                                 HttpContext?.Request?.RouteValues?["controller"]?.ToString() ?? "UnknownController",
                                 MethodBase.GetCurrentMethod()?.Name,
                                 activityDetails ?? "NoDetails");
            }
        }
        #endregion


        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetPurchaseRequestItems()
        {
            try
            {
                var nv = new NameValueCollection();
                DataTable dt = _DAL.GetData("sp_GetPruchaseRequestItems", nv, _DAL.CSManagementPortalDatabase);

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
                _logger.LogError("Error in GetPurchaseRequestItems: {0}", ex.Message);
                return BadRequest("Something Went Wrong.");
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult GetVendorsbyCategories([FromBody] CategoryByInstance obj)
        {
            if (string.IsNullOrEmpty(obj.CategoryId.ToString()))
                return BadRequest(new { success = false, message = "CategoryId is required." });

            try
            {
                var nv = new NameValueCollection {
                    { "@CategoryId-INT", obj.CategoryId.ToString() } ,
                    { "@InstanceId-INT", obj.InstanceId.ToString() }
                };

                DataSet dt = _DAL.GetDataSet("sp_GetApprovedVendorsByCategory", nv, _DAL.CSManagementPortalDatabase);

                var result = dt.Tables.Cast<DataTable>()
                    .Select(table => new
                    {
                        TableName = table.TableName,
                        Rows = table.AsEnumerable()
                                    .Select(row => table.Columns.Cast<DataColumn>()
                                        .ToDictionary(col => col.ColumnName, col => row[col]))
                    });

                return Ok(new { success = true, data = result });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVendorsbyCategories");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #region Save & Get PurchaseRequest

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public IActionResult SavePurchaseInitiateRequest([FromBody] PurchaseRequest purchaseRequest)
        {
            if (purchaseRequest == null) return BadRequest("Request data is null.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nv = new NameValueCollection
                {
                    { "workflow-VARCHAR", HttpUtility.HtmlEncode(purchaseRequest.workflow) },
                    { "instanceid-INT", (purchaseRequest.instanceid ?? 0).ToString() },
                    { "createdby-INT", (purchaseRequest.userid ?? 0).ToString() },
                    { "requestDate-DATE", purchaseRequest.requestDate?.ToString("yyyy-MM-dd") },
                    { "justification-VARCHAR", HttpUtility.HtmlEncode(purchaseRequest.justification ?? "") }
                };

                var dt = _DAL.GetData("sp_insert_PurchaseRequest", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    int newInstanceId = Convert.ToInt32(dt.Rows[0]["InstanceID"]);

                    foreach (var item in purchaseRequest.items)
                    {
                        var nvItem = new NameValueCollection
                        {
                            { "instanceid-INT", newInstanceId.ToString() },
                            { "itemId-VARCHAR", HttpUtility.HtmlEncode(item.id) },
                            { "qty-INT", item.qty.ToString() },
                            { "fileName-VARCHAR", item.fileName ?? "" },
                            { "createdby-INT", (purchaseRequest.userid ?? 0).ToString() },
                        };
                        _DAL.GetData("sp_insert_PurchaseItems", nvItem, _DAL.CSManagementPortalDatabase);
                    }

                    SystemActivityLog(ActivityLog.ActivityID_Insert, $"Saved Purchase Request ID: {newInstanceId}");
                }

                 return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "SavePurchaseInitiateRequest", MethodBase.GetCurrentMethod()?.Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod()?.Name + "  " + ex.Message);
                return StatusCode(500, "Something went wrong. Please contact your system administrator.");
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetPurchaseRequestDetailByInstanceId(int instanceid, int userId) 
        {
            try
            {
                var nv = new NameValueCollection
                {
                    ["instanceid-INT"] = instanceid.ToString(),
                    ["UserId-INT"] = userId.ToString() 
                };

                DataSet ds = _DAL.GetDataSet("sp_select_PurchaseRequestDetailByInstanceId", nv, _DAL.CSManagementPortalDatabase);

                if (ds != null && ds.Tables.Count > 0)
                {
                    var result = new
                    {
                        Master = ds.Tables[0],
                        Items = ds.Tables[1],
                        VendorQuote = ds.Tables[2],
                        VendorQuotes = ds.Tables[3],
                        PurchaseOrder = ds.Tables[4],
                        GateEntry = ds.Tables[5],
                        QAQC = ds.Tables[6],
                        GRNRecord = ds.Tables[7],
                        PaymentRecord = ds.Tables[8]
                    };
                    return Ok(result);
                }
                return BadRequest("No record found.");
            }
            catch (Exception ex)
            {
                return BadRequest("System error occurred.");
            }
        }

        [HttpPost]
        public IActionResult SubmitRFQIssuancetoVendorsRequest([FromBody] RFQIssuanceRequest request)
        {
            if (request == null) return BadRequest("Request data is null.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nv = new NameValueCollection
                {
                    { "category-VARCHAR", HttpUtility.HtmlEncode(request.category) },
                    { "startDate-DateTime", request.startDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "" },
                    { "endDate-DateTime", request.endDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "" },
                    { "description-VARCHAR", HttpUtility.HtmlEncode(request.description ?? "") },
                    { "AttachmentPath-VARCHAR", request.fileName ?? "" },
                    { "instanceid-INT", request.instanceid?.ToString() ?? "0" },
                    { "reissuanceduration-INT", request.ReIssuanceDuration?.ToString() ?? "0" },
                    { "reissuancecycle-INT", request.ReIssuanceCycle?.ToString() ?? "1" },
                    { "createdby-INT", request.userid?.ToString() ?? "0" }
                };

                var ds = _DAL.GetData("sp_Insert_RFQIssuance", nv, _DAL.CSManagementPortalDatabase);

                DataTable dtVendor = null;

                if (ds != null && ds.Rows.Count > 0)
                {
                    int rfqId = Convert.ToInt32(ds.Rows[0]["RFQID"]);
                    string instanceId = ds.Rows[0]["instanceid"].ToString();

                    if (request.vendors != null && request.vendors.Any())
                    {
                        string? baseUrl = _configuration["AM_TARGET_APP"];
                        string rfqLink = $"{baseUrl}/PurchaseRequest/Initiate?instanceid={instanceId}";

                        foreach (var vendorId in request.vendors)
                        {
                            int idx = request.vendors.IndexOf(vendorId);
                            string currentEmail = request.vendorEmails?[idx] ?? "";

                            var nvVendor = new NameValueCollection
                            {
                                { "rfqId-INT", rfqId.ToString() },
                                { "vendorId-INT", vendorId },
                                { "instanceid-INT", instanceId },
                                { "createdby-INT", request.userid?.ToString() ?? "0" }
                            };

                            dtVendor = _DAL.GetData("sp_Insert_RFQVendors", nvVendor, _DAL.CSManagementPortalDatabase);

                            if (dtVendor != null && dtVendor.Rows.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(currentEmail))
                                {
                                    string emailBody = ActivityLog.EmailBodyForRFQRequest(request.categoryName, request.endDate?.ToString("f"), rfqLink);
                                    _sendemail.SendEmailToVendors(new List<string> { currentEmail }, emailBody, $"Action Required for Bid: Instance No:  {request.instanceid}", _DAL);
                                }
                            }
                        }
                    }

                    SystemActivityLog(ActivityLog.ActivityID_Insert, $"RFQ Issued: ID {rfqId} to {request.vendors?.Count} vendors.");
                    return dtVendor != null && dtVendor.Rows.Count > 0 ? Ok(dtVendor) : BadRequest(dtVendor);
                }

                return BadRequest("Failed to create RFQ record.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitRFQIssuancetoVendorsRequest");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet]
        public IActionResult AutoProcurement()
        {
            try
            {
                DataTable dt = _DAL.GetData("sp_move_procurement", null, _DAL.CSManagementPortalDatabase);

                if (dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_move_procurement");

                    foreach (DataRow dr in dt.Rows)
                    {
                        string? baseUrl = _configuration["AM_TARGET_APP"];
                        string instanceId = dr["InstanceId"].ToString();
                        string CategoryName = dr["CategoryName"].ToString();
                        string EndDate = dr["EndDate"].ToString();
                        string rfqLink = $"{baseUrl}/PurchaseRequest/Initiate?instanceid={instanceId}";
                        string currentEmail = dr["UserEmail"].ToString();
                        string UserName = dr["UserName"].ToString();
                        string FlowDirection = dr["FlowDirection"].ToString();
                        string emailBody = "";

                        if (FlowDirection == "Forward")
                        {
                            emailBody = ActivityLog.EmailBodyForProcurementMoveForward(instanceId, UserName, CategoryName, EndDate?.ToString(), rfqLink);
                        }
                        else
                        {
                            emailBody = ActivityLog.EmailBodyForProcurementExtended(instanceId, UserName, CategoryName, EndDate?.ToString(), rfqLink);
                        }

                        _sendemail.SendEmailToVendors(new List<string> { currentEmail }, emailBody, $"Action Required for Bid: Instance No:  {instanceId}", _DAL);
                    }
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_move_procurement");
                }

                return Ok(new { data = dt });
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}",
                                 "BMRController", MethodBase.GetCurrentMethod()?.Name, ex?.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod()?.Name + "  " + ex?.Message);
                return BadRequest("Something Went Wrong. Please Contact Your System Administrator");
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetRFQsTasks(string wfcode, int roleid, int userid)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("wfcode-VARCHAR", InputSanitizer.Sanitize(wfcode));
                nv.Add("roleid-INT", roleid.ToString());
                nv.Add("userid-INT", userid.ToString());
                dt = _DAL.GetData("sp_select_RFQmytask", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_RFQmytask");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_RFQmytask");
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
        [HttpPost]
        public IActionResult SubmitRFQsRequestbkp([FromBody] RFQsRequestbkp request)
        {
            if (request == null) return BadRequest("Request data is null.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nv = new NameValueCollection
                {
                    { "RFQRemarks-VARCHAR", HttpUtility.HtmlEncode(request.RFQRemarks ?? "") },
                    { "QuotedPrice-VARCHAR", HttpUtility.HtmlEncode(request.QuotedPrice ?? "") },
                    { "fileName-VARCHAR", request.fileName ?? "" },
                    { "instanceid-INT", request.instanceid?.ToString() ?? "0" },
                    { "createdby-INT", request.userid?.ToString() ?? "0" }
                };

                var dt = _DAL.GetData("sp_Insert_RFQSubmit", nv, _DAL.CSManagementPortalDatabase);
                SystemActivityLog(ActivityLog.ActivityID_Insert, $"Saved Vendor Quotaion Request ID: {request.userid}");
                return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest(dt);

            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "RFQController", "SubmitRFQsRequest", ex.Message);
                return StatusCode(500, "Internal Server Error during database operation.");
            }
        }
        [RateLimitMiddleware(100, 5)]
        [HttpPost]
        public IActionResult SubmitRFQsRequest([FromBody] RFQsRequest request)
        {
            if (request == null)
                return BadRequest("Request data is null.");

            try
            {
                var quotationData =
                    JsonConvert.DeserializeObject<List<RFQQuotationModel>>(request.QuotationData ?? "");

                if (quotationData == null || !quotationData.Any())
                    return BadRequest("Quotation data not found.");

                foreach (var item in quotationData)
                {
                    var nv = new NameValueCollection
                    {
                        { "RFQRemarks-VARCHAR", HttpUtility.HtmlEncode(request.RFQRemarks ?? "") },

                        { "QuotedPrice-VARCHAR", item.QuotedPrice ?? "" },

                        { "fileName-VARCHAR", request.fileName ?? "" },

                        { "InstanceID-INT", request.instanceid?.ToString() ?? "0" },

                        { "CreatedBy-INT", request.userid?.ToString() ?? "0" }
                    };

                    var dt = _DAL.GetData(
                        "sp_Insert_RFQSubmit",
                        nv,
                        _DAL.CSManagementPortalDatabase);

                    int quotationId = Convert.ToInt32(dt.Rows[0]["quotationid"]);

                    // SAVE DETAILS
                    foreach (var detail in item.Details)
                    {
                        var detailNV = new NameValueCollection
                        {
                            { "quatationid-INT", quotationId.ToString() },
                            { "tenorid-INT", detail.tenorid.ToString() },
                            { "value-FLOAT", detail.value.ToString() }
                        };

                        _DAL.InsertData(
                            "sp_Insert_RFQSubmitDetail",
                            detailNV,
                            _DAL.CSManagementPortalDatabase);
                    }
                }

                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}",
                    "RFQController",
                    "SubmitRFQsRequest",
                    ex.Message);

                return StatusCode(500,
                    "Internal Server Error during database operation.");
            }
        }
        [RateLimitMiddleware(100, 5)]
        [HttpPost]
        public IActionResult SubmitVendorSelection([FromBody] VendorSelectionRequest request)
        {
            if (request == null) return BadRequest("Request data is null.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nv = new NameValueCollection
                {
                    { "WinnerVendorID-INT", request.winnerVendorId.ToString() },
                    { "WinnerName-VARCHAR", HttpUtility.HtmlEncode(request.winnerName ?? "") },
                    { "WinnerEmail-VARCHAR", HttpUtility.HtmlEncode(request.winnerEmail ?? "") },
                    { "WinnerPrice-VARCHAR", request.winnerPrice?.Replace(",", "") ?? "0" },
                    { "Justification-VARCHAR", HttpUtility.HtmlEncode(request.justification ?? "") },
                    { "fileName-VARCHAR", HttpUtility.HtmlEncode(request.fileName ?? "") },
                    { "InstanceID-INT", request.instanceid?.ToString() ?? "0" },
                    { "SelectedBy-INT", request.userid?.ToString() ?? "0" }
                };

                var dt = _DAL.GetData("sp_Finalize_VendorSelection", nv, _DAL.CSManagementPortalDatabase);

                SystemActivityLog(ActivityLog.ActivityID_Insert,$"Vendor Selected: {request.winnerName} (ID: {request.winnerVendorId}) for Instance: {request.instanceid}");
                if (dt != null && dt.Rows.Count > 0)
                {
                    if (!string.IsNullOrEmpty(request.winnerEmail))
                    {
                        string emailSubject = $"Notification: Vendor Selection Awarded - Instance No#{request.instanceid}";
                        string emailBody = ActivityLog.EmailBodyForVendorSelection(request.winnerName, request.instanceid?.ToString());
                        _sendemail.SendEmailToVendors(new List<string> { request.winnerEmail }, emailBody, emailSubject, _DAL);
                    }

                    return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest(dt);

                }
                else
                {
                    return BadRequest("Failed to process vendor selection.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "RFQController", "SubmitVendorSelection", ex.Message);
                return StatusCode(500, "Internal Server Error during vendor selection process.");
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public async Task<IActionResult> UploadPurchaseOrder([FromBody] UploadPO data)
        {
            try
            {
                var nv = new NameValueCollection
                {
                    { "instanceid-INT", data.instanceid.ToString() },
                    { "poNumber-VARCHAR", !string.IsNullOrEmpty(data.poNumber?.ToString()) ? data.poNumber.ToString() : "0" },
                    { "poDescription-VARCHAR", HttpUtility.HtmlEncode(data.poDescription ?? "") },
                    { "originalfilename-VARCHAR", HttpUtility.HtmlEncode(data.originalFileName ?? "") },
                    { "newfilename-VARCHAR", HttpUtility.HtmlEncode(data.fileName ?? "") },
                    { "fileextension-VARCHAR", data.fileExtension ?? "" },
                    { "filesize-BIGINT", data.fileSize.ToString() },
                    { "userid-INT", data.userid.ToString() }
                };

                var dt = _DAL.GetData("sp_insert_PurchaseOrderRequest", nv, _DAL.CSManagementPortalDatabase);

                SystemActivityLog( ActivityLog.ActivityID_Insert,(dt != null && dt.Rows.Count > 0 ? ActivityLog.ActivityDetails_Insert : ActivityLog.ActivityDetails_Insert2) + " sp_insert_PurchaseOrderRequest");

                return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "UploadPurchaseOrder", MethodBase.GetCurrentMethod()?.Name, ex?.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, $"{MethodBase.GetCurrentMethod()?.Name}  {ex.Message}");
                return BadRequest("Something went wrong. Please contact your system administrator.");
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public async Task<IActionResult> SaveGateEntryRecordPurchaseRequest([FromBody] GateEntryRecord GateEntryRecord)
        {
            try
            {
                var nv = new NameValueCollection
                {
                    { "instanceid-INT", (GateEntryRecord.instanceid ?? 0).ToString() },
                    { "createdby-INT", GateEntryRecord.userid.ToString() },
                    { "vehicleNumber-VARCHAR", GateEntryRecord.vehicleNumber },
                    { "driverIdentity-VARCHAR", GateEntryRecord.driverIdentity },
                    { "liveTimestamp-VARCHAR", GateEntryRecord.liveTimestamp },
                };

                var dt = _DAL.GetData("sp_insert_GateEntryRecordPurchaseRequest", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + " sp_insert_GateEntryRecordPurchaseRequest");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + " sp_insert_GateEntryRecordPurchaseRequest");
                }

                return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "PurchaseRequestController", MethodBase.GetCurrentMethod()?.Name, ex?.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod()?.Name + "  " + ex?.Message);
                return BadRequest("Something went wrong. Please contact your system administrator.");
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpPost]
        public IActionResult SaveQAQCDetails([FromBody] QAQCRequestModel request)
        {
            if (request == null) return BadRequest("Request data is null.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nv = new NameValueCollection
                {
                    { "VendorId-INT", request.VendorId?.ToString() ?? "0" },
                    { "InstanceID-INT", request.instanceid?.ToString() ?? "0" },
                    { "Remarks-VARCHAR", HttpUtility.HtmlEncode(request.qaqcRemarks ?? "") },
                    { "AttachmentPath-VARCHAR", request.fileName ?? "" },
                    { "CreatedBy-INT", request.userid?.ToString() ?? "0" }
                };

                var dt = _DAL.GetData("sp_Insert_QAQCApproval", nv, _DAL.CSManagementPortalDatabase);

                SystemActivityLog(ActivityLog.ActivityID_Insert, $"QA/QC Approval Saved for Instance: {request.instanceid} by User: {request.userid}");

                return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest("Failed to save QA/QC details.");
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "PurchaseRequestController", "SaveQAQCDetails", ex.Message);
                return StatusCode(500, "Internal Server Error during QA/QC database operation.");
            }
        }

        [RateLimitMiddleware(50, 5)]
        [HttpPost]
        public async Task<IActionResult> SaveGRNRecordPurchaseRequest([FromBody] GRNRecord GRNRecord)
        {
            try
            {
                var nv = new NameValueCollection
                {
                    { "instanceid-INT", (GRNRecord.instanceid ?? 0).ToString() },
                    { "createdby-INT", GRNRecord.userid.ToString() },
                    { "grnNumber-VARCHAR", GRNRecord.grnNumber },
                    { "receivedQuantity-INT", GRNRecord.receivedQuantity.ToString()},
                    { "paymentDueDate-VARCHAR", GRNRecord.paymentDueDate },
                    { "Remarks-VARCHAR", GRNRecord.Remarks },
                };

                var dt = _DAL.GetData("sp_insert_GRNRecordPurchaseRequest", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert + " sp_insert_GRNRecordPurchaseRequest");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Insert, ActivityLog.ActivityDetails_Insert2 + " sp_insert_GRNRecordPurchaseRequest");
                }

                return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "PurchaseRequestController", MethodBase.GetCurrentMethod()?.Name, ex?.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod()?.Name + "  " + ex?.Message);
                return BadRequest("Something went wrong. Please contact your system administrator.");
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpPost]
        public IActionResult SavePaymentDetailsPurchaseRequest([FromBody] PaymentRequestModel request)
        {
            if (request == null) return BadRequest("Request data is null.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var nv = new NameValueCollection
                {
                    { "InstanceID-INT", request.instanceid?.ToString() ?? "0" },
                    { "AmountReceived-INT", request.amountReceived.ToString() ?? "0"},
                    { "PaymentRemarks-VARCHAR", HttpUtility.HtmlEncode(request.paymentRemarks ?? "") },
                    { "AttachmentPath-VARCHAR", request.fileName ?? "" },
                    { "CreatedBy-INT", request.userid?.ToString() ?? "0" }
                };

                var dt = _DAL.GetData("sp_InsertPaymentScheduling", nv, _DAL.CSManagementPortalDatabase);

                SystemActivityLog(ActivityLog.ActivityID_Insert, $"Payments Details Saved for Instance: {request.instanceid} by User: {request.userid}");

                return dt != null && dt.Rows.Count > 0 ? Ok(dt) : BadRequest("Failed to save Payments details.");
            }
            catch (Exception ex)
            {
                _logger.LogError("{Controller} {Method} {Error}", "PurchaseRequestController", "SavePaymentDetailsPurchaseRequest", ex.Message);
                return StatusCode(500, "Internal Server Error during QA/QC database operation.");
            }
        }

        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetFundName()
        {
            try
            {
                var nv = new NameValueCollection();
                DataTable dt = _DAL.GetData("sp_select_fundname", nv, _DAL.CSManagementPortalDatabase);

                var list = dt.AsEnumerable().Select(row => new {
                    Id = row["Id"],
                    Text = row["Text"]
                }).ToList();

                if (list.Any())
                {
                    return Ok(new { success = true, data = list });
                }

                return Ok(new { success = false, message = "No funds found." });
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2}", "FormsController", MethodBase.GetCurrentMethod().Name, ex.Message);
                SystemActivityLog(ActivityLog.ActivityID_Error, MethodBase.GetCurrentMethod().Name + " " + ex.Message);
                return BadRequest("Something Went Wrong Please Contact Your Sysmtem Adminsitrator");
            }
        }
       
        [RateLimitMiddleware(100, 5)]
        [HttpGet]
        public IActionResult GetBankRateByInstanceId(int instanceid)
        {
            DataTable dt = new DataTable();
            try
            {
                NameValueCollection? nv = new NameValueCollection();
                nv.Clear();
                nv.Add("instanceid-INT", instanceid.ToString());
                dt = _DAL.GetData("sp_select_banratecomparison", nv, _DAL.CSManagementPortalDatabase);

                if (dt != null && dt.Rows.Count > 0)
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get + "sp_select_banratecomparison");
                }
                else
                {
                    SystemActivityLog(ActivityLog.ActivityID_Get, ActivityLog.ActivityDetails_Get2 + "sp_select_banratecomparison");
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
        #endregion

    }
}