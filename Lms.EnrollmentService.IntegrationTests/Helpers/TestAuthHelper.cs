using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Lms.EnrollmentService.IntegrationTests.Helpers;

public static class TestAuthHelper
{
    // MUST match appsettings.Development.json EXACTLY
    private const string Secret = "dev-secret-key-at-least-32-characters-long-!!";
    private const string Issuer = "lms-auth-service";
    private const string Audience = "lms-api";
    public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");


    public static string GenerateToken(string sub, string role = "Student", string email = "test@lms.com")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var claims = new[]
        {
            new Claim("sub", sub),
            new Claim("role", role),
            new Claim("email", email),
            new Claim("name", $"Test {role}")
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}