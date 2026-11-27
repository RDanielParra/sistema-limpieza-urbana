using System.ComponentModel.DataAnnotations;

namespace LimpiezaUrbana.API.DTOs;

public sealed class ConfirmarReporteRequest
{
    [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12.")]
    public int? Mes { get; set; }

    [Range(2020, 2100, ErrorMessage = "El año debe ser válido (>= 2020).")]
    public int? Anio { get; set; }

    [Required(ErrorMessage = "El nombre del archivo es obligatorio.")]
    [MaxLength(255)]
    public string NombreArchivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe incluir al menos un registro.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un registro.")]
    public List<RegistroConfirmarDto> Registros { get; set; } = new();
}

public sealed class RegistroConfirmarDto
{
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateOnly Fecha { get; set; }

    [Range(1, 15, ErrorMessage = "El número de cuadrilla debe estar entre 1 y 15.")]
    public int? NoCuadrilla { get; set; }

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    public string Ubicacion { get; set; } = string.Empty;

    public List<string> Actividades { get; set; } = new();

    [Required(ErrorMessage = "El campo Solicitado es obligatorio.")]
    [MaxLength(200)]
    public string SolicitadoRaw { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NumeroOficio { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "ml no puede ser negativo.")]
    public decimal Ml { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "m2 no puede ser negativo.")]
    public decimal M2 { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "m3 no puede ser negativo.")]
    public decimal M3 { get; set; }
}

public sealed class ConfirmarReporteResponse
{
    public int CargaId { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public int TotalRegistrosGuardados { get; set; }
    public decimal TotalKgCalculado { get; set; }
}

public sealed class CargaMensualDto
{
    public int Id { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public DateTime FechaCarga { get; set; }
    public long TotalRegistros { get; set; }
}
