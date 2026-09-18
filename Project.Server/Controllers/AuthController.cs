using System.Security.Cryptography;
using System.Text;
using FluentValidation.Results;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Project.Server.Attributes;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Security.Authorization;
using Project.Server.Services.Core;
using Project.Server.Services.Interfaces;

namespace Project.Server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [ModuleInfo(
        DisplayName = "Auth",
        Description = "Gestión de autenticación en la aplicación",
        Icon = "bi-shield-lock",
        Path = "auth",
        Order = 1,
        IsVisible = false
    )]
    public class AuthController : CommonController
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public AuthController(IAuthService authService, IMapper mapper)
        {
            _authService = authService;
            _mapper = mapper;
        }

        // -- Anonymous endpoints ------------------------------------------------

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model, CancellationToken ct = default)
        {
            var response = await _authService.AuthAsync(model, ct);
            return response.Success
                ? Ok(new Response<AuthWithRefreshResponse>
                {
                    Success = true,
                    Message = response.Message,
                    Data = response.Data
                })
                : StatusCode(StatusCodeFor(response.Status), new Response<List<ValidationFailure>>
                {
                    Success = false,
                    Message = response.Message,
                    Data = response.Errors
                });
        }

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpPost("Refresh")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh(CancellationToken ct = default)
        {
            // 3.9 — double-submit CSRF: the SPA echoes the XSRF-TOKEN cookie
            // back as the X-XSRF-TOKEN header. We require both to be present
            // and to match before the refresh endpoint proceeds.
            var cookieToken = Request.Cookies["XSRF-TOKEN"];
            var headerToken = Request.Headers["X-XSRF-TOKEN"].ToString();
            if (string.IsNullOrEmpty(cookieToken) ||
                string.IsNullOrEmpty(headerToken) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(cookieToken),
                    Encoding.UTF8.GetBytes(headerToken)))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new Response<List<ValidationFailure>>
                {
                    Success = false,
                    Message = "CSRF token missing or mismatched.",
                });
            }

            var cookie = Request.Cookies[AuthService.RefreshCookieName];
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.RefreshAsync(cookie, ip, ct);
            return response.Success
                ? Ok(new Response<AuthWithRefreshResponse>
                {
                    Success = true,
                    Message = response.Message,
                    Data = response.Data
                })
                : StatusCode(StatusCodeFor(response.Status), new Response<List<ValidationFailure>>
                {
                    Success = false,
                    Message = response.Message,
                    Data = response.Errors
                });
        }

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            var cookie = Request.Cookies[AuthService.RefreshCookieName];
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _authService.LogoutAsync(cookie, ip, ct);
            return ProjectSingle(response);
        }

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model, CancellationToken ct = default)
        {
            model.CreatedBy = 1;
            var response = await _authService.RegisterAsync(model, ct);
            return response.Success
                ? Ok(new Response<UserResponse>
                {
                    Success = true,
                    Message = response.Message,
                    Data = _mapper.Map<User, UserResponse>(response.Data!)
                })
                : BadRequest(new Response<List<ValidationFailure>>
                {
                    Success = false,
                    Message = response.Message,
                    Data = response.Errors
                });
        }

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpGet("{token}")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ValidateToken(string token, CancellationToken ct = default)
        {
            var response = await _authService.ValidateTokenAsync(token, ct);
            return response.Success
                ? Ok(new Response<string>
                {
                    Success = true,
                    Message = response.Message,
                    Data = response.Data
                })
                : BadRequest(new Response<List<ValidationFailure>>
                {
                    Success = false,
                    Message = response.Message,
                    Data = response.Errors
                });
        }

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model, CancellationToken ct = default)
        {
            var response = await _authService.ChangePasswordAsync(model, ct);
            return ProjectSingle(response);
        }

        [ExcludeFromSync]
        [AllowAnonymous]
        [HttpPost("RecoveryPassword")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> PostRecoveryPassword([FromBody] RecoveryPasswordRequest model, CancellationToken ct = default)
        {
            var response = await _authService.RecoveryPasswordAsync(model, ct);
            return ProjectSingle(response);
        }

        // -- Authenticated endpoints ---------------------------------------------

        [Authorize]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> PostResetPassword([FromBody] ResetPasswordRequest model, CancellationToken ct = default)
        {
            model.IdUser = GetUserId();
            var response = await _authService.ResetPasswordAsync(model, ct);
            return ProjectSingle(response);
        }

        // ----------------------------------------------------------------

        private IActionResult ProjectSingle(Response<string, List<ValidationFailure>> response)
        {
            return ProjectSingle<string>(response);
        }

        private IActionResult ProjectSingle<T>(Response<T, List<ValidationFailure>> response)
        {
            if (response.Success)
            {
                return Ok(new Response<T>
                {
                    Success = true,
                    Message = response.Message,
                    Data = response.Data
                });
            }
            return BadRequest(new Response<List<ValidationFailure>>
            {
                Success = false,
                Message = response.Message,
                Data = response.Errors
            });
        }
    }
}