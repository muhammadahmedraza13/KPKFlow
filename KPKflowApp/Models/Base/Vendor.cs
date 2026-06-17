using System.ComponentModel.DataAnnotations;

namespace KPKflowApp.Models.Base
{
    public class VendorRegistrationModel
    {
        public string? InvitationToken { get; set; }
        public string? BusinessName { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Address { get; set; }
        public string? NTN { get; set; }
        public string? STRN { get; set; }
        public string? Category { get; set; }
        public List<string>? SubCategories { get; set; }
        public string? TaxDocumentPath { get; set; }
        public string? CompanyProfilePath { get; set; }
        public string? AdditionalDocsPath { get; set; }
        public UserRegistrationDetail User { get; set; } = new();
    }
    public class UserRegistrationDetail
    {
        [Required]
        [EmailAddress]
        public string? UserEmail { get; set; }

        [Required(ErrorMessage = "Login Name is required")]
        public string? LoginName { get; set; } 

        [Required]
        public string? MobileNumber { get; set; }

        public string? UserName { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }

    }

    public class VendorCheckModel
    {
        [Required]
        public string? Email { get; set; }

        [Required]
        public string? Mobile { get; set; }

        [Required]
        public string? LoginName { get; set; }
    }

    public class FinalVerificationModel
    {
        [Required]
        public string? RegistrationToken { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 4)] 
        public string? EmailOTP { get; set; }

        public string? MobileOTP { get; set; } 
    }

    public class ApproveSupplier
    {
        public string? InstitutionId { get; set; }
        public string? StatusUpdate { get; set; }
    }

    public class InviteVendors
    {
        public List<string>? Emails { get; set; }
    }
    public class VendorStatusUpdateDto
    {
        public int? VendorId { get; set; }
        public string? Status { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
