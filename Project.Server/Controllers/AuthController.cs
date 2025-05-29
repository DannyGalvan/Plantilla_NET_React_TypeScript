using FluentValidation.Results;
using Lombok.NET;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;

namespace Project.Server.Controllers
{
    /// <summary>
    /// Defines the <see cref="AuthController" />
    /// </summary> 
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllArgsConstructor]
    public partial class AuthController : CommonController
    {
        /// <summary>
        /// Defines the _authService
        /// </summary>
        private readonly IAuthService _authService;

        /// <summary>
        /// Defines the _authService
        /// </summary>
        private readonly IMapper _mapper;


        /// <summary>
        /// The Login
        /// </summary>
        /// <param name="model">The model<see cref="LoginRequest"/></param>
        /// <returns>The <see cref="ActionResult"/></returns>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(LoginRequest model)
        {
            var response = _authService.Auth(model);

            if (response.Success)
            {
                Response<AuthResponse> authResponse = new()
                {
                    Data = response.Data,
                    Success = response.Success,
                    Message = response.Message
                };

                return Ok(authResponse);
            }

            Response<List<ValidationFailure>> errorResponse = new()
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            };

            return BadRequest(errorResponse);
        }

        /// <summary>
        /// The Register Users
        /// </summary>
        /// <param name="model">The model<see cref="RegisterRequest"/></param>
        /// <returns>The <see cref="ActionResult"/></returns>
        [AllowAnonymous]
        [HttpPost("Register")]
        public ActionResult Register(RegisterRequest model)
        {
            model.CreatedBy = 1;
            var response = _authService.Register(model);

            if (response.Success)
            {
                Response<UserResponse> authResponse = new()
                {
                    Data = _mapper.Map<User,UserResponse>(response.Data!),
                    Success = response.Success,
                    Message = response.Message
                };

                return Ok(authResponse);
            }

            Response<List<ValidationFailure>> errorResponse = new()
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            };

            return BadRequest(errorResponse);
        }

        /// <summary>
        /// The GetToken
        /// </summary>
        /// <param name="token">The token<see cref="string"/></param>
        /// <returns>The <see cref="ActionResult"/></returns>
        [AllowAnonymous]
        [HttpGet("{token}")]
        public ActionResult GetToken(string token)
        {
            Response<string, List<ValidationFailure>> response = _authService.ValidateToken(token);

            if (response.Success)
            {
                Response<string> tokenResponse = new()
                {
                    Data = response.Data,
                    Success = response.Success,
                    Message = response.Message
                };
                return BadRequest(tokenResponse);
            }

            Response<List<ValidationFailure>> errorResponse = new()
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            };

            return Ok(errorResponse);
        }

        /// <summary>
        /// The ChangePassword
        /// </summary>
        /// <param name="model">The model<see cref="ChangePasswordRequest"/></param>
        /// <returns>The <see>
        ///         <cref>ActionResult{Response{string}}</cref>
        ///     </see>
        /// </returns>
        [AllowAnonymous]
        [HttpPut("ChangePassword")]
        public ActionResult<Response<string>> ChangePassword(ChangePasswordRequest model)
        {
            Response<string, List<ValidationFailure>> response = _authService.ChangePassword(model);

            if (response.Success)
            {
                Response<string> changePasswordResponse = new()
                {
                    Data = response.Data,
                    Success = response.Success,
                    Message = response.Message
                };

                return Ok(changePasswordResponse);

            }

            Response<List<ValidationFailure>> errorResponse = new()
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            };

            return BadRequest(errorResponse);
        }

        /// <summary>
        /// The PostResetPassword
        /// </summary>
        /// <param name="model">The model<see cref="ResetPasswordRequest"/></param>
        /// <returns>The <see cref="ActionResult"/></returns>
        [Authorize]
        [HttpPost("ResetPassword")]
        public ActionResult PostResetPassword([FromBody] ResetPasswordRequest model)
        {
            model.IdUser = GetUserId();
            Response<string, List<ValidationFailure>> response = _authService.ResetPassword(model);

            if (response.Success)
            {
                Response<string> resetPasswordResponse = new()
                {
                    Data = response.Data,
                    Success = response.Success,
                    Message = response.Message
                };

                return Ok(resetPasswordResponse);

            }

            Response<List<ValidationFailure>> errorResponse = new()
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            };

            return BadRequest(errorResponse);
        }

        /// <summary>
        /// The PostRecoveryPassword
        /// </summary>
        /// <param name="model">The model<see cref="RecoveryPasswordRequest"/></param>
        /// <returns>The <see cref="ActionResult"/></returns>
        [AllowAnonymous]
        [HttpPost("RecoveryPassword")]
        public ActionResult PostRecoveryPassword([FromBody] RecoveryPasswordRequest model)
        {
            Response<string, List<ValidationFailure>> response = _authService.RecoveryPassword(model);

            if (response.Success)
            {
                Response<string> recoveryPasswordResponse = new()
                {
                    Data = response.Data,
                    Success = response.Success,
                    Message = response.Message
                };

                return Ok(recoveryPasswordResponse);

            }

            Response<List<ValidationFailure>> errorResponse = new()
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            };

            return BadRequest(errorResponse);
        }
    }
}
