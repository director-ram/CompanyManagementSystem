using System.Text.Json.Serialization;

namespace CompanyManagementSystem.Models
{
    public class LoginModel
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }
        
        [JsonPropertyName("password")]
        public required string Password { get; set; }
    }
}