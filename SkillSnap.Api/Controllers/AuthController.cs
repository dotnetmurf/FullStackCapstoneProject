using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SkillSnap.Api.Models;
using SkillSnap.Shared.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillSnap.Api.Controllers;

/// <summary>
/// API controller for user authentication and registration.
/// Handles JWT token generation and user identity management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration request containing email and password.</param>
    /// <returns>An authentication response with JWT token on success.</returns>
    /// <response code="200">User registered successfully with JWT token.</response>
    /// <response code="400">Invalid registration data or user already exists.</response>
    /// <response code="500">Internal server error during registration.</response>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid registration data"
                });
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                });
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = $"Registration failed: {errors}"
                });
            }

            // Assign default User role
            await _userManager.AddToRoleAsync(user, "User");

            // Generate JWT token for the new user
            var token = await GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                Email = user.Email,
                Expiration = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60"))
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Authenticates a user and generates a JWT token.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>An authentication response with JWT token on success.</returns>
    /// <response code="200">User authenticated successfully with JWT token.</response>
    /// <response code="401">Invalid credentials or account locked.</response>
    /// <response code="400">Invalid login data.</response>
    /// <response code="500">Internal server error during login.</response>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid login data"
                });
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            // Generate JWT token
            var token = await GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                Email = user.Email,
                Expiration = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60"))
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Generates a JWT token for the authenticated user.
    /// Includes user ID, email, and role claims in the token payload.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>A JWT token string valid for the configured expiry period.</returns>
    /// <exception cref="InvalidOperationException">Thrown when JWT Key is not configured.</exception>
    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SkillSnapApi";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "SkillSnapClient";
        var expiryInMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
