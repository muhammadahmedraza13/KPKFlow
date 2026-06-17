using System.ComponentModel.DataAnnotations;

namespace KPKflowApp.Models.Authentication
{
    public class ForgetPassword
    {
        [Required]
        public string UserEmail { get; set; }
    }
}