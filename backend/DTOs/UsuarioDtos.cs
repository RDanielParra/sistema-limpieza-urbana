using System.ComponentModel.DataAnnotations;

namespace LimpiezaUrbana.API.DTOs;

public sealed class CrearUsuarioRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    [MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Rol { get; set; } = string.Empty;
}

public sealed class ActualizarEstadoUsuarioRequest
{
    public bool Activo { get; set; }
}
