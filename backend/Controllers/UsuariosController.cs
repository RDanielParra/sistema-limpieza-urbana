using Dapper;
using LimpiezaUrbana.API.Data;
using LimpiezaUrbana.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace LimpiezaUrbana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Desarrollador")]
public sealed class UsuariosController : ControllerBase
{
    private static readonly HashSet<string> RolesPermitidos = new(StringComparer.Ordinal)
    {
        "Capturador",
        "Coordinador",
        "Desarrollador"
    };

    private readonly IDbConnectionFactory _db;

    public UsuariosController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> Listar()
    {
        const string sql = """
            SELECT id, nombre_completo, correo, rol, activo
            FROM usuarios
            ORDER BY id ASC;
            """;

        using var conn = _db.CreateConnection();
        var usuarios = await conn.QueryAsync<UsuarioDto>(sql);
        return Ok(usuarios);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear([FromBody] CrearUsuarioRequest request)
    {
        if (!RolesPermitidos.Contains(request.Rol))
        {
            return BadRequest(new
            {
                mensaje = "Rol inválido. Los roles permitidos son: Capturador, Coordinador, Desarrollador."
            });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        const string sql = """
            INSERT INTO usuarios (nombre_completo, correo, password_hash, rol, activo)
            VALUES (@NombreCompleto, @Correo, @PasswordHash, @Rol, TRUE)
            RETURNING id, nombre_completo, correo, rol, activo;
            """;

        try
        {
            using var conn = _db.CreateConnection();
            var creado = await conn.QuerySingleAsync<UsuarioDto>(sql, new
            {
                NombreCompleto = request.NombreCompleto.Trim(),
                Correo = request.Correo.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                request.Rol
            });

            return CreatedAtAction(nameof(Listar), new { id = creado.Id }, creado);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "Ya existe un usuario registrado con ese correo." });
        }
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoUsuarioRequest request)
    {
        const string sql = """
            UPDATE usuarios
            SET activo = @Activo
            WHERE id = @Id
            RETURNING id, nombre_completo, correo, rol, activo;
            """;

        using var conn = _db.CreateConnection();
        var actualizado = await conn.QueryFirstOrDefaultAsync<UsuarioDto>(sql, new
        {
            Id = id,
            request.Activo
        });

        if (actualizado is null)
        {
            return NotFound(new { mensaje = $"No se encontró el usuario con id {id}." });
        }

        return Ok(actualizado);
    }
}
