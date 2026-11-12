using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BillFlow.Api.Domain;
using BillFlow.Api.Services;
using Microsoft.IdentityModel.Tokens;

namespace BillFlow.Api.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;
    private readonly IClock _clock;

    public JwtTokenGenerator(IConfiguration config, IClock clock)
    {
        _config = config;
        _clock = clock;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(Customer customer)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = _clock.UtcNow.AddMinutes(jwtSection.GetValue("ExpiryMinutes", 60));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, customer.Email),
            new Claim(ClaimTypes.Name, customer.Name),
            new Claim(ClaimTypes.Role, customer.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
