namespace LimpiezaUrbana.API.DTOs;

public sealed class EstadisticasFiltroQuery
{
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public int? Mes { get; set; }
    public int? Anio { get; set; }
    public int[]? CategoriaIds { get; set; }
}

public sealed class TotalesAcumuladosDto
{
    public long TotalRegistros { get; set; }
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}

public sealed class EstadisticaCategoriaDto
{
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public long TotalRegistros { get; set; }
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}

public sealed class EstadisticaActividadDto
{
    public int ActividadId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Actividad { get; set; } = string.Empty;
    public long TotalRegistros { get; set; }
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}

public sealed class SerieFechaDto
{
    public DateOnly Fecha { get; set; }
    public long TotalRegistros { get; set; }
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}

public sealed class SerieMesDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public long TotalRegistros { get; set; }
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}

public sealed class CategoriaCatalogoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public sealed class EstadisticasResponse
{
    public TotalesAcumuladosDto Totales { get; set; } = new();
    public List<EstadisticaCategoriaDto> PorCategoria { get; set; } = new();
    public List<EstadisticaActividadDto> PorActividad { get; set; } = new();
    public List<SerieFechaDto> SeriesPorFecha { get; set; } = new();
    public List<SerieMesDto> SeriesPorMes { get; set; } = new();
}

public sealed class RegistroOperativoConsultaDto
{
    public long Id { get; set; }
    public int CargaId { get; set; }
    public DateOnly Fecha { get; set; }
    public short? NoCuadrilla { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string SolicitadoRaw { get; set; } = string.Empty;
    public string? NumeroOficio { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public List<string> Actividades { get; set; } = new();
    public decimal Ml { get; set; }
    public decimal M2 { get; set; }
    public decimal M3 { get; set; }
    public decimal Kg { get; set; }
}
