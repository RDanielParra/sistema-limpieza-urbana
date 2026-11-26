using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LimpiezaUrbana.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace LimpiezaUrbana.API.Services;

public interface IJwtService
{
    string GenerarToken(Usuario usuario);
}

public sealed class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerarToken(Usuario usuario)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("No se encontró la configuración 'Jwt:Key'.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "LimpiezaUrbana.API";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "LimpiezaUrbana.Frontend";

        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
