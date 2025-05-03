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
        private readonly AppDbContext _context; // Your DbContext (Ensure AppDbContext is defined and configured)
        private readonly IConfiguration _configuration; // To access JWT settings (Ensure JWT settings are in appsettings.json)

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] LoginModel loginModel)
        {
            // Basic validation for the incoming model
            if (loginModel == null || string.IsNullOrEmpty(loginModel.Username) || string.IsNullOrEmpty(loginModel.Password))
            {
                return BadRequest(new { message = "Invalid login request" });
            }

            // Validate user credentials
            var user = _context.Users.SingleOrDefault(u => 
                string.Equals(u.Username, loginModel.Username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                Console.WriteLine($"User not found: {loginModel.Username}");
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (!VerifyPassword(loginModel.Password, user.PasswordHash))
            {
                Console.WriteLine($"Password verification failed for user: {loginModel.Username}");
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            // Return token and user info
            return Ok(new { 
                message = "Logged in successfully",
                authToken = token,
                userId = user.Id,
                username = user.Username
            });
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "defaultSecretKey12345678901234567890123456789012"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "CompanyManagementSystem",
                audience: _configuration["Jwt:Audience"] ?? "CompanyManagementSystemUsers",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // You'll need your password verification logic here
        // This method MUST correctly compare the enteredPassword with the storedPasswordHash
        // using the same hashing algorithm used during user registration.
        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            try
            {
                // Log the password verification attempt (without showing the actual password)
                Console.WriteLine($"Verifying password for hash: {storedPasswordHash.Substring(0, 10)}...");
                
                // Use BCrypt to verify the password
                bool result = BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash);
                
                Console.WriteLine($"Password verification result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during password verification
                Console.WriteLine($"Error verifying password: {ex.Message}");
                return false;
            }
        }

        // Other user-related endpoints
        [HttpPost("register")]
        public ActionResult Register([FromBody] RegisterModel registerModel)
        {
            // Basic validation for the incoming model
            if (registerModel == null || string.IsNullOrEmpty(registerModel.Username) || string.IsNullOrEmpty(registerModel.Password))
            {
                return BadRequest(new { message = "Invalid registration request" });
            }

            // Check if username already exists (case-insensitive)
            if (_context.Users.Any(u => string.Equals(u.Username, registerModel.Username, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Username already exists: {registerModel.Username}");
                return Conflict(new { message = "Username already exists" });
            }

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerModel.Password);

            // Create new user
            var user = new User
            {
                Username = registerModel.Username,
                PasswordHash = passwordHash,
                PurchaseOrders = new List<PurchaseOrder>()
            };

            // Save user to database
            _context.Users.Add(user);
            _context.SaveChanges();

            // Return success response
            return Ok(new { message = "Registration successful" });
        }
    }
}