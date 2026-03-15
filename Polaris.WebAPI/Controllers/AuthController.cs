using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Response;
using Polaris.Application.Features.Auth.Commands.ChangePassword;
using Polaris.Application.Features.Auth.Commands.ConfirmEmail;
using Polaris.Application.Features.Auth.Commands.ForgotPassword;
using Polaris.Application.Features.Auth.Commands.Login;
using Polaris.Application.Features.Auth.Commands.Logout;
using Polaris.Application.Features.Auth.Commands.RefreshToken;
using Polaris.Application.Features.Auth.Commands.ResendConfirmation;
using Polaris.Application.Features.Auth.Commands.ResendConfirmationEmail;
using Polaris.Application.Features.Auth.Commands.ResendResetPassToken;
using Polaris.Application.Features.Auth.Commands.ResetPassword;
using Polaris.Application.Features.Auth.Commands.SocialLogin;
using Polaris.WebAPI.Models.Auth;
using System.Security.Claims;

namespace Polaris.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IMapper _mapper;

        public AuthController(IMapper mapper)
        {
            _mapper = mapper;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var command = _mapper.Map<LoginCommand>(request);
                command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                command.UserAgent = Request.Headers["User-Agent"].ToString();

                var result = await Mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Unauthorized(new ApiResponse(401, result.Errors?.FirstOrDefault() ?? "Invalid email or password"));
                }

                Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/",
                    IsEssential = true
                });

                var response = new LoginResponse
                {
                    AccessToken = result.AccessToken!,
                    ExpiresAt = result.ExpiresAt!.Value,
                    User = _mapper.Map<UserDto>(result.User)
                };

                return Ok(new ApiResponse(200, "Login successful", response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"Login failed: {ex.Message}"));
            }
        }

        [HttpPost("social-login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
        {
            try
            {
                // Map request to command
                var command = _mapper.Map<SocialLoginCommand>(request);

                // Send to mediator
                var result = await Mediator.Send(command);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse(400, result.Errors?.FirstOrDefault() ?? "Social login failed"));
                }

                // Set refresh token in HTTP-only cookie (same as normal login)
                Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/",
                    IsEssential = true
                });

                // Prepare response (same format as LoginResponse)
                var response = new LoginResponse
                {
                    AccessToken = result.AccessToken!,
                    ExpiresAt = result.ExpiresAt!.Value,
                    User = _mapper.Map<UserDto>(result.User)
                };

                return Ok(new ApiResponse(200, "Social login successful", response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"Social login failed: {ex.Message}"));
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(new ApiResponse(400, "Refresh token not found"));
                }

                var command = new RefreshTokenCommand { RefreshToken = refreshToken };
                var result = await Mediator.Send(command);

                if (!result.Succeeded)
                {
                    Response.Cookies.Delete("refreshToken");
                    return Unauthorized(new ApiResponse(401, result.Errors?.FirstOrDefault() ?? "Invalid refresh token"));
                }

                Response.Cookies.Append("refreshToken", result.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/",
                    IsEssential = true
                });

                var response = new RefreshTokenResponse
                {
                    AccessToken = result.AccessToken!,
                    ExpiresAt = result.ExpiresAt!.Value
                };

                return Ok(new ApiResponse(200, "Token refreshed successfully", response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"Refresh token failed: {ex.Message}"));
            }
        }
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                var userId = userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;

                var command = new LogoutCommand
                {
                    UserId = userId,
                    RefreshToken = refreshToken,
                    LogoutAllDevices = request?.LogoutAllDevices ?? false
                };

                await Mediator.Send(command);

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });

                if (request?.LogoutAllDevices == true)
                {

                }

                return Ok(new ApiResponse(200, "Logout successful"));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"Logout failed: {ex.Message}"));
            }
        }

        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public IActionResult ValidateToken()
        {
            return Ok(new ApiResponse(200, "Token is valid"));
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
        {
            var decodedToken = Uri.UnescapeDataString(token);

            var command = new ConfirmEmailCommand { UserId = userId, Token = decodedToken };
            var result = await Mediator.Send(command);

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "ConfirmEmail");

            if (result.Succeeded)
            {
                var html = await System.IO.File.ReadAllTextAsync(Path.Combine(templatePath, "Success.html"));
                return Content(html, "text/html");
            }

            var failedHtml = await System.IO.File.ReadAllTextAsync(Path.Combine(templatePath, "Failed.html"));
            failedHtml = failedHtml.Replace(
                "The confirmation link is invalid or expired.",
                result.Message ?? "Email confirmation failed"
            );

            return Content(failedHtml, "text/html");
        }

        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            var command = new ResendConfirmationCommand { Email = request.Email };
            var result = await Mediator.Send(command);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse(200, result.Message!));
            }

            return BadRequest(new ApiResponse(400, result.Message!));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var command = new ForgotPasswordCommand { Email = request.Email };
            var result = await Mediator.Send(command);

            // Always return 200 OK for security
            return Ok(new ApiResponse(200, result.Message!));
        }

        /*[HttpGet("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordPage([FromQuery] string email, [FromQuery] string token)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "PasswordReset", "ResetPasswordPage.html");
            var html = await System.IO.File.ReadAllTextAsync(templatePath);
            return Content(html, "text/html");
        }*/

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var command = _mapper.Map<ResetPasswordCommand>(request);
            var result = await Mediator.Send(command);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse(200, result.Message!));
            }

            return BadRequest(new ApiResponse(400, result.Message!));
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Get user ID from claims
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new ApiResponse(401, "User not found"));
                }

                var command = new ChangePasswordCommand
                {
                    UserId = userId,
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword,
                    ConfirmPassword = request.ConfirmPassword
                };

                var result = await Mediator.Send(command);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse(200, result.Message!));
                }

                return BadRequest(new ApiResponse(400, result.Message!));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
        }

        [HttpPost("resend-reset-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> ResendResetToken([FromBody] ResendResetTokenRequest request)
        {
            var command = new ResendResetTokenCommand { Email = request.Email };
            var result = await Mediator.Send(command);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse(200, result.Message!));
            }
            else
            {
                return BadRequest(new ApiResponse(400, result.Message!));
            }
        }
    }
}