using Dapper;
using LimpiezaUrbana.API.Data;
using LimpiezaUrbana.API.DTOs;

namespace LimpiezaUrbana.API.Services;

public interface IEstadisticasService
{
    Task<EstadisticasResponse> ObtenerEstadisticasAsync(EstadisticasFiltroQuery filtro);
    Task<IEnumerable<CategoriaCatalogoDto>> ObtenerCategoriasAsync();
    Task<IEnumerable<RegistroOperativoConsultaDto>> ListarRegistrosAsync(EstadisticasFiltroQuery filtro, int limite, int offset);
}

public sealed class EstadisticasService : IEstadisticasService
{
    private readonly IDbConnectionFactory _db;

    public EstadisticasService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<EstadisticasResponse> ObtenerEstadisticasAsync(EstadisticasFiltroQuery filtro)
    {
        var parametros = CrearParametrosFiltro(filtro);

        const string spArgs = "CAST(@FechaInicio AS DATE), CAST(@FechaFin AS DATE), CAST(@Mes AS INT), CAST(@Anio AS INT), CAST(@CategoriaIds AS INT[])";

        using var conn = _db.CreateConnection();

        var totales = await conn.QuerySingleAsync<TotalesAcumuladosDto>(
            $"SELECT * FROM sp_estadisticas_totales({spArgs});", parametros);

        var porCategoria = (await conn.QueryAsync<EstadisticaCategoriaDto>(
            $"SELECT * FROM sp_estadisticas_por_categoria({spArgs});", parametros)).ToList();

        var porActividad = (await conn.QueryAsync<EstadisticaActividadDto>(
            $"SELECT * FROM sp_estadisticas_por_actividad({spArgs});", parametros)).ToList();

        var seriesFechaRaw = await conn.QueryAsync<SerieFechaRow>(
            $"SELECT * FROM sp_estadisticas_series_fecha({spArgs});", parametros);

        var seriesMes = (await conn.QueryAsync<SerieMesDto>(
            $"SELECT * FROM sp_estadisticas_series_mes({spArgs});", parametros)).ToList();

        return new EstadisticasResponse
        {
            Totales = totales,
            PorCategoria = porCategoria,
            PorActividad = porActividad,
            SeriesPorFecha = seriesFechaRaw
                .Select(r => new SerieFechaDto
                {
                    Fecha = DateOnly.FromDateTime(r.FechaDt),
                    TotalRegistros = r.TotalRegistros,
                    Ml = r.Ml,
                    M2 = r.M2,
                    M3 = r.M3,
                    Kg = r.Kg
                })
                .ToList(),
            SeriesPorMes = seriesMes
        };
    }

    public async Task<IEnumerable<CategoriaCatalogoDto>> ObtenerCategoriasAsync()
    {
        const string sql = """
            SELECT id, nombre, descripcion
            FROM categorias
            ORDER BY id ASC;
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<CategoriaCatalogoDto>(sql);
    }

    public async Task<IEnumerable<RegistroOperativoConsultaDto>> ListarRegistrosAsync(
        EstadisticasFiltroQuery filtro,
        int limite,
        int offset)
    {
        var baseParams = CrearParametrosFiltro(filtro);
        const string sql = """
            SELECT * FROM sp_listar_registros(
                CAST(@FechaInicio AS DATE),
                CAST(@FechaFin AS DATE),
                CAST(@Mes AS INT),
                CAST(@Anio AS INT),
                CAST(@CategoriaIds AS INT[]),
                @Limite,
                @Offset
            );
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<RegistroRow>(sql, new
        {
            baseParams.FechaInicio,
            baseParams.FechaFin,
            baseParams.Mes,
            baseParams.Anio,
            baseParams.CategoriaIds,
            Limite = limite,
            Offset = offset
        });

        return rows.Select(r => new RegistroOperativoConsultaDto
        {
            Id = r.Id,
            CargaId = r.CargaId,
            Fecha = DateOnly.FromDateTime(r.FechaDt),
            NoCuadrilla = r.NoCuadrilla,
            Ubicacion = r.Ubicacion,
            SolicitadoRaw = r.SolicitadoRaw,
            NumeroOficio = r.NumeroOficio,
            Categoria = r.Categoria,
            Actividades = string.IsNullOrWhiteSpace(r.Actividades)
                ? new List<string>()
                : r.Actividades.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
            Ml = r.Ml,
            M2 = r.M2,
            M3 = r.M3,
            Kg = r.Kg
        });
    }

    private static FiltroSqlParams CrearParametrosFiltro(EstadisticasFiltroQuery filtro) => new()
    {
        FechaInicio = filtro.FechaInicio?.ToDateTime(TimeOnly.MinValue),
        FechaFin = filtro.FechaFin?.ToDateTime(TimeOnly.MinValue),
        Mes = filtro.Mes,
        Anio = filtro.Anio,
        CategoriaIds = filtro.CategoriaIds is { Length: > 0 } ? filtro.CategoriaIds : null
    };

    private sealed class FiltroSqlParams
    {
        public DateTime? FechaInicio { get; init; }
        public DateTime? FechaFin { get; init; }
        public int? Mes { get; init; }
        public int? Anio { get; init; }
        public int[]? CategoriaIds { get; init; }
    }

    private sealed class SerieFechaRow
    {
        public DateTime FechaDt { get; set; }
        public long TotalRegistros { get; set; }
        public decimal Ml { get; set; }
        public decimal M2 { get; set; }
        public decimal M3 { get; set; }
        public decimal Kg { get; set; }
    }

    private sealed class RegistroRow
    {
        public long Id { get; set; }
        public int CargaId { get; set; }
        public DateTime FechaDt { get; set; }
        public short? NoCuadrilla { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string SolicitadoRaw { get; set; } = string.Empty;
        public string? NumeroOficio { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Actividades { get; set; } = string.Empty;
        public decimal Ml { get; set; }
        public decimal M2 { get; set; }
        public decimal M3 { get; set; }
        public decimal Kg { get; set; }
    }
}
