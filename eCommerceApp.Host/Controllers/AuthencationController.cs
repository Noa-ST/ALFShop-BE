using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthencationController(IAuthenticationService authenticationService) : ControllerBase
    {
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser(CreateUser user)
        {
            var result = await authenticationService.CreateUser(user);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(LoginUser user)
        {
            var result = await authenticationService.LoginUser(user);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("refresh/{refreshToken}")]
        public async Task<IActionResult> ReviveToken(string refreshToken)
        {
            var result = await authenticationService.ReviveToken(refreshToken);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
