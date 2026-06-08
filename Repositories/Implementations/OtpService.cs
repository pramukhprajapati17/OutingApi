using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class OtpService : IOtpService
    {
        private readonly IUserInterface _userRepository;
        private readonly IRedisService _redisService;
        private readonly IMailService _mailService;

        public OtpService(IUserInterface userRepository, IRedisService redisService, IMailService mailService)
        {
            _userRepository = userRepository;
            _redisService = redisService;
            _mailService = mailService;
        }

        public async Task<OtpRequestResult> RequestOtpAsync(TrainerForgotPasswordRequest user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return new OtpRequestResult { Success = false, Message = "Email is required" };
            }

            var email = user.Email.Trim();

            var existingUser = await _userRepository.GetUserByEmail(email);
            if (existingUser == null)
            {
                return new OtpRequestResult { Success = false, Message = "Email does not exist" };
            }

            var otp = GenerateOtp();

            var pendingUser = new UserRegister
            {
                Email = existingUser.Email,
                Username = existingUser.Username,
                Password = existingUser.Password,
                Role = existingUser.Role,
                IsActive = existingUser.IsActive
            };

            // store pending registration with ttl
            await _redisService.SetPendingRegistrationAsync(email, pendingUser, otp, TimeSpan.FromMinutes(2));

            var displayName = ResolveDisplayName(user.Name, email);
            await _mailService.SendOtpEmailAsync(email, displayName, otp, 2);

            return new OtpRequestResult { Success = true, Message = "OTP sent to your email" };
        }

        private static string GenerateOtp()
        {
            // generate a secure 6-digit OTP
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
            return value.ToString("D6");
        }

        private static string ResolveDisplayName(string? name, string email)
        {
            if (!string.IsNullOrWhiteSpace(name)) return name.Trim();
            var idx = email.IndexOf('@');
            return idx > 0 ? email.Substring(0, idx) : email;
        }
    }
}
