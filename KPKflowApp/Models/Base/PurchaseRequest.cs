using System.ComponentModel.DataAnnotations;

namespace KPKflowApp.Models.Base
{
    public class PurchaseRequest
    {
        [Required] public string? workflow { get; set; }
        public int? userid { get; set; }
        public int? instanceid { get; set; }
        [Required] 
        public DateTime? requestDate { get; set; }
        [Required]
        public float targetAmount { get; set; }
        public string? justification { get; set; }
        public List<PurchaseItem> items { get; set; } = new List<PurchaseItem>();
    }

    public class PurchaseItem
    {
        [Required] public string? id { get; set; }
        [Range(1, int.MaxValue)] public int qty { get; set; }
        public IFormFile? file { get; set; }
        public string? fileName { get; set; }
    }
    public class CategoryByInstance
    {
        public int CategoryId { get; set; }
        public int InstanceId { get; set; }
    }
    //public class RFQIssuanceRequest
    //{
    //    public IFormFile? file { get; set; }
    //    public string? category { get; set; }
    //    public string? categoryName { get; set; }
    //    public DateTime? startDate { get; set; }
    //    public DateTime? endDate { get; set; }
    //    public string? description { get; set; }
    //    public string? instanceid { get; set; }

    //    public List<string>? vendors { get; set; }

    //    public List<string>? vendorEmails { get; set; }
    //    public List<string>? vendorMobiles { get; set; }

    //    public string? fileName { get; set; }
    //    public int? userid { get; set; }
    //}

    public class RFQIssuanceRequest
    {
        public IFormFile? file { get; set; }
        public string? fileName { get; set; }
        public string? category { get; set; }
        public string? categoryName { get; set; }

        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? description { get; set; }
        public string? instanceid { get; set; }

        public List<string>? vendors { get; set; }

        public List<string>? vendorEmails { get; set; }
        public List<string>? vendorMobiles { get; set; }
        public int? userid { get; set; }
        public int? ReIssuanceDuration { get; set; }
        public int? ReIssuanceCycle { get; set; }
    }
    public class RFQsRequest
    {
        public IFormFile? file { get; set; }
        public string? RFQRemarks { get; set; }
        public string? instanceid { get; set; }
        public string? fileName { get; set; }
        public int? userid { get; set; }
        public string? QuotationData { get; set; }
    }
    
    public class VendorSelectionRequest
    {
        public int winnerVendorId { get; set; }
        public string winnerName { get; set; }
        public string winnerEmail { get; set; }
        public string winnerPrice { get; set; }
        public string justification { get; set; }
        public string? fileName { get; set; }
        public string instanceid { get; set; }
        public int? userid { get; set; }
    }

    public class UploadPO
    {
        public IFormFile? File { get; set; }
        public int? instanceid { get; set; }
        public int? userid { get; set; }
        public string? poNumber { get; set; }
        public string? poDescription { get; set; }
        public string? fileName { get; set; }
        public string? originalFileName { get; set; }
        public string? fileExtension { get; set; }
        public long? fileSize { get; set; }

    }

    public class GateEntryRecord
    {
        public int? instanceid { get; set; }
        public int? userid { get; set; }
        public string? vehicleNumber { get; set; }
        public string? driverIdentity { get; set; }
        public string? liveTimestamp { get; set; }
    }
    public class QAQCRequestModel
    {
        public int? instanceid { get; set; }
        public int? VendorId { get; set; }
        public string? qaqcRemarks { get; set; }
        public List<IFormFile>? qaqcFiles { get; set; }
        public string? fileName { get; set; }
        public int? userid { get; set; }
    }
    public class PaymentRequestModel
    {
        public int? instanceid { get; set; }
        public int? amountReceived { get; set; }
        public string? paymentRemarks { get; set; }
        public List<IFormFile>? paymentAttachment { get; set; }
        public string? fileName { get; set; }
        public int? userid { get; set; }
    }
    public class GRNRecord
    {
        public int? instanceid { get; set; }
        public int? userid { get; set; }
        public string? grnNumber { get; set; }
        public int? receivedQuantity { get; set; }
        public string? paymentDueDate { get; set; }
        public string? Remarks { get; set; }

    }
}
