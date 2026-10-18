using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

public class JwtSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiresMinutes { get; set; } = 60;
}

public interface ITokenService
{
    (string token, DateTime expiresAtUtc) Create(User user);
}

public class TokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _s = options.Value;

    public (string token, DateTime expiresAtUtc) Create(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_s.Key)), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_s.ExpiresMinutes);
        var jwt = new JwtSecurityToken(_s.Issuer, _s.Audience, claims, expires: expires, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
