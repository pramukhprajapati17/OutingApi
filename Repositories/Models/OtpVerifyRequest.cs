using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class OtpVerifyRequest
    {
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? Otp { get; set; }
    }
}
