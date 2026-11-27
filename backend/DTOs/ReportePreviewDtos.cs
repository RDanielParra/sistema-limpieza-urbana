namespace LimpiezaUrbana.API.DTOs;

public sealed class RegistroPrevisualizacionDto
{
    public string IdTemporal { get; set; } = string.Empty;
    public string Hoja { get; set; } = string.Empty;
    public DateOnly? Fecha { get; set; }
    public int? NoCuadrilla { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public List<string> Actividades { get; set; } = new();
    public string SolicitadoRaw { get; set; } = string.Empty;
    public string? NumeroOficio { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}

public sealed class PrevisualizarReporteResponse
{
    public string NombreArchivo { get; set; } = string.Empty;
    public int? Mes { get; set; }
    public int? Anio { get; set; }
    public int TotalHojasLeidas { get; set; }
    public int TotalRegistros { get; set; }
    public decimal FactorKgPorM3 { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<RegistroPrevisualizacionDto> Registros { get; set; } = new();
}
