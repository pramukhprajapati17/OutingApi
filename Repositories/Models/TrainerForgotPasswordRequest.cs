using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class TrainerForgotPasswordRequest
    {
        [Required]
        public string? Email { get; set; }
        public string? Name { get; set; }
    }
}
