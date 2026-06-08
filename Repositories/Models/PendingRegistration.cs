using System;
using Repositories.Models;

namespace Repositories.Models
{
    public class PendingRegistration
    {
        public UserRegister? User { get; set; }
        public string? Otp { get; set; }
    }
}
