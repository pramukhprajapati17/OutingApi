using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api")]
    public class UserController : ControllerBase
    {
        private readonly IUserInterface _user;
        private readonly Repositories.Interfaces.IRedisService _redis;
        private readonly Repositories.Interfaces.IMailService _mail;
        public UserController(IUserInterface user, Repositories.Interfaces.IRedisService redis, Repositories.Interfaces.IMailService mail)
        {
            _user = user;
            _redis = redis;
            _mail = mail;
        }

        [HttpPost]
        [Route("registerUser")]
        public async Task<IActionResult> RegisterUser([FromForm]UserRegister user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("Email is required");

            // if mail already exists, return conflict
            var existing = await _user.GetUserByEmail(user.Email ?? string.Empty);
            if (existing != null)
                return Conflict("Mail already exists");

            // generate OTP and store pending registration in Redis for 5 minutes
            var otp = Random.Shared.Next(100000, 999999).ToString();
            await _redis.SetPendingRegistrationAsync(user.Email ?? string.Empty, user, otp, TimeSpan.FromMinutes(5));

            // send OTP via email
            try
            {
                await _mail.SendOtpEmailAsync(user.Email ?? string.Empty, user.Username ?? string.Empty, otp, 5);
            }
            catch (Exception ex)
            {
                // sending failed — remove pending registration and return error
                await _redis.RemovePendingRegistrationAsync(user.Email ?? string.Empty);
                return StatusCode(500, "Failed to send OTP email: " + ex.Message);
            }

            return Ok(new { message = "OTP sent to user" });
        }

        [HttpPost]
        [Route("verifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromForm] Repositories.Models.OtpVerifyRequest req)
        {
            var (pendingUser, pendingOtp) = await _redis.GetPendingRegistrationAsync(req.Email ?? string.Empty);
            if (pendingUser == null || pendingOtp == null)
                return NotFound("No pending registration found or OTP expired");

            if (pendingOtp != req.Otp)
                return BadRequest("Invalid OTP");

            // proceed with actual registration
            var result = await _user.RegisterUser(pendingUser);
            if (result == 1)
            {
                await _redis.RemovePendingRegistrationAsync(req.Email ?? string.Empty);
                return Ok("User registered successfully");
            }
            else if (result == -1)
            {
                return Conflict("User with this email already exists");
            }
            return StatusCode(500, "Error registering user");
        }
    }
}