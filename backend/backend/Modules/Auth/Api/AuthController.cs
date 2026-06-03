using backend.Modules.Auth.Contracts;
using backend.Shared.Contracts;
using backend.Shared.Kernel;
using backend.Infrastructure.Auth;
using backend.Modules.Auth.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Modules.Auth.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new MessageResponse { Message = "Request body is required." });
        }

        try
        {
            return Ok(await _auth.RegisterAsync(request, GetIpAddress(), GetUserAgent(), cancellationToken));
        }
        catch (AuthServiceException ex)
        {
            return BadRequest(new MessageResponse { Message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Unauthorized(new MessageResponse { Message = "Invalid credentials." });
        }

        var response = await _auth.LoginAsync(request, GetIpAddress(), GetUserAgent(), cancellationToken);
        return response is null ? Unauthorized(new MessageResponse { Message = "Invalid credentials." }) : Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Unauthorized(new MessageResponse { Message = "Invalid refresh token." });
        }

        var response = await _auth.RefreshAsync(request, GetIpAddress(), GetUserAgent(), cancellationToken);
        return response is null ? Unauthorized(new MessageResponse { Message = "Invalid refresh token." }) : Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request ?? new LogoutRequest(), GetIpAddress(), cancellationToken);
        return Ok(new MessageResponse { Message = "Logged out." });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> Me(CancellationToken cancellationToken)
    {
        var current = _currentUser.GetRequiredUser();
        var account = await _auth.GetAccountAsync(current.AccountId, cancellationToken);
        return account is null ? NotFound(new MessageResponse { Message = "Account not found." }) : Ok(account);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
    }
}
