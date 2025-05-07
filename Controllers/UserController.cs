using Microsoft.AspNetCore.Mvc;
using CompanyManagementSystem.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using CompanyManagementSystem.Data;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CompanyManagementSystem.Models
{
    public class RegisterModel
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }
        
        [JsonPropertyName("password")]
        public required string Password { get; set; }
    }
}

namespace CompanyManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, IConfiguration configuration, ILogger<UserController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginModel loginModel)
        {
            try
            {
                if (loginModel == null || string.IsNullOrEmpty(loginModel.Username) || string.IsNullOrEmpty(loginModel.Password))
                {
                    _logger.LogWarning("Invalid login request: missing username or password");
                    return BadRequest(new { message = "Invalid login request" });
                }

                var user = _context.Users
                    .Where(u => string.Equals(u.Username, loginModel.Username, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (user == null)
                {
                    _logger.LogWarning("Login failed: user not found - {Username}", loginModel.Username);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                if (!VerifyPassword(loginModel.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: invalid password for user - {Username}", loginModel.Username);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                try
                {
                    var token = GenerateJwtToken(user);
                    _logger.LogInformation("Successfully generated JWT token for user: {Username}", user.Username);

                    return Ok(new LoginResponse
                    {
                        Message = "Logged in successfully",
                        AuthToken = token,
                        UserId = user.Id,
                        Username = user.Username
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating JWT token for user: {Username}", user.Username);
                    return StatusCode(500, new { message = "Error generating authentication token" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user: {Username}", loginModel?.Username);
                return StatusCode(500, new { message = "An unexpected error occurred during login" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            try
            {
                var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration");
                var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found in configuration");
                var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not found in configuration");

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.Now.AddHours(24),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token");
                throw;
            }
        }

        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        [HttpPost("register")]
        public ActionResult<RegisterResponse> Register([FromBody] RegisterModel registerModel)
        {
            try
            {
                if (registerModel == null || string.IsNullOrEmpty(registerModel.Username) || string.IsNullOrEmpty(registerModel.Password))
                {
                    _logger.LogWarning("Invalid registration request: missing username or password");
                    return BadRequest(new { message = "Invalid registration request" });
                }

                if (_context.Users.Any(u => u.Username == registerModel.Username))
                {
                    _logger.LogWarning("Registration failed: username already exists - {Username}", registerModel.Username);
                    return BadRequest(new { message = "Username already exists" });
                }

                var user = new User
                {
                    Username = registerModel.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerModel.Password)
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                _logger.LogInformation("User registered successfully: {Username}", registerModel.Username);
                return Ok(new RegisterResponse { Message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Username}", registerModel?.Username);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }
    }

    public class LoginResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("authToken")]
        public string AuthToken { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    public class RegisterResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}