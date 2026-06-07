using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;

namespace API.Controllers
{
    // [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserInterface _user;
        public UserController(IUserInterface user)
        {
            _user = user;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(UserRegister user)
        {
            var result = await _user.RegisterUser(user);
            return result switch
            {
                1 => Ok("User registered successfully"),
                -1 => Conflict("User with this email already exists"),
                _ => StatusCode(500, "Error registering user")
            };
        }
    }
}