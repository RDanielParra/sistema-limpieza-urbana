using System.Security.Claims;
using Dapper;
using LimpiezaUrbana.API.Data;
using LimpiezaUrbana.API.DTOs;
using LimpiezaUrbana.API.Models;
using LimpiezaUrbana.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimpiezaUrbana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly IJwtService _jwtService;

    public AuthController(IDbConnectionFactory db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        const string sql = """
            SELECT id, nombre_completo, correo, password_hash, rol, activo, creado_en
            FROM usuarios
            WHERE LOWER(correo) = LOWER(@Correo)
            LIMIT 1;
            """;

        using var conn = _db.CreateConnection();
        var usuario = await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { Correo = request.Correo.Trim() });

        if (usuario is null || !usuario.Activo)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas o cuenta inactiva." });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas o cuenta inactiva." });
        }

        var token = _jwtService.GenerarToken(usuario);

        return Ok(new LoginResponse
        {
            Token = token,
            Usuario = new UsuarioDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Correo,
                Rol = usuario.Rol,
                Activo = usuario.Activo
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<UsuarioDto> Me()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var userId))
        {
            return Unauthorized();
        }

        return Ok(new UsuarioDto
        {
            Id = userId,
            NombreCompleto = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Correo = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            Activo = true
        });
    }
}
