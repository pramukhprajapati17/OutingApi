using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IUserInterface
    {
        Task<int>RegisterUser(UserRegister user);
        Task<UserRegister?> GetUserByEmail(string email);
    }
}