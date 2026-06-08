using System;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IRedisService
    {
        Task SetPendingRegistrationAsync(string email, UserRegister user, string otp, TimeSpan ttl);
        Task<(UserRegister? user, string? otp)> GetPendingRegistrationAsync(string email);
        Task RemovePendingRegistrationAsync(string email);
    }
}
