using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IOtpService
    {
        Task<OtpRequestResult> RequestOtpAsync(TrainerForgotPasswordRequest request);
    }
}
