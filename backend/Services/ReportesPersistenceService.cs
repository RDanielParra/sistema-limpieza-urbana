using Dapper;
using LimpiezaUrbana.API.Data;
using LimpiezaUrbana.API.DTOs;

namespace LimpiezaUrbana.API.Services;

public interface IReportesPersistenceService
{
    Task<ConfirmarReporteResponse> ConfirmarAsync(ConfirmarReporteRequest request, int usuarioId);
    Task<IEnumerable<CargaMensualDto>> ListarCargasAsync();
}

public sealed class ReportesPersistenceService : IReportesPersistenceService
{
    private const decimal FactorDefault = 1650m / 14m;
    private readonly IDbConnectionFactory _db;
    private readonly decimal _factorKgPorM3;

    public ReportesPersistenceService(IDbConnectionFactory db, IConfiguration configuration)
    {
        _db = db;
        _factorKgPorM3 = configuration.GetValue<decimal?>("Excel:FactorKgPorM3") ?? FactorDefault;
    }

    public async Task<ConfirmarReporteResponse> ConfirmarAsync(ConfirmarReporteRequest request, int usuarioId)
    {
        var mesFinal = request.Mes ?? request.Registros[0].Fecha.Month;
        var anioFinal = request.Anio ?? request.Registros[0].Fecha.Year;

        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            const string insertCargaSql = """
                INSERT INTO cargas_mensuales (mes, anio, nombre_archivo, usuario_id)
                VALUES (@Mes, @Anio, @NombreArchivo, @UsuarioId)
                RETURNING id;
                """;

            var cargaId = await conn.ExecuteScalarAsync<int>(insertCargaSql, new
            {
                Mes = mesFinal,
                Anio = anioFinal,
                NombreArchivo = request.NombreArchivo.Trim(),
                UsuarioId = usuarioId
            }, tx);

            var cuadrillasMap = (await conn.QueryAsync<(int Id, short Numero)>(
                "SELECT id, numero FROM cuadrillas;", transaction: tx))
                .ToDictionary(x => (int)x.Numero, x => x.Id);

            var categoriasMap = (await conn.QueryAsync<(int Id, string Nombre)>(
                "SELECT id, nombre FROM categorias;", transaction: tx))
                .ToDictionary(x => x.Nombre.Trim(), x => x.Id, StringComparer.OrdinalIgnoreCase);

            var actividadesMap = (await conn.QueryAsync<(int Id, string Clave)>(
                "SELECT id, clave FROM catalogo_actividades;", transaction: tx))
                .ToDictionary(x => x.Clave.Trim(), x => x.Id, StringComparer.OrdinalIgnoreCase);

            // Asegurar que todas las categorías y actividades existan antes de la inserción masiva
            foreach (var cat in request.Registros
                         .Select(r => string.IsNullOrWhiteSpace(r.Categoria) ? "Otros" : r.Categoria.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!categoriasMap.ContainsKey(cat))
                {
                    var catId = await conn.ExecuteScalarAsync<int>(
                        "INSERT INTO categorias (nombre) VALUES (@Nombre) ON CONFLICT (nombre) DO UPDATE SET nombre = EXCLUDED.nombre RETURNING id;",
                        new { Nombre = cat },
                        tx);
                    categoriasMap[cat] = catId;
                }
            }

            foreach (var act in request.Registros
                         .SelectMany(r => r.Actividades)
                         .Where(a => !string.IsNullOrWhiteSpace(a))
                         .Select(a => a.Trim().ToUpperInvariant())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!actividadesMap.ContainsKey(act))
                {
                    var actId = await conn.ExecuteScalarAsync<int>(
                        "INSERT INTO catalogo_actividades (clave, nombre) VALUES (@Clave, @Nombre) ON CONFLICT (clave) DO UPDATE SET clave = EXCLUDED.clave RETURNING id;",
                        new { Clave = act, Nombre = act },
                        tx);
                    actividadesMap[act] = actId;
                }
            }

            var count = request.Registros.Count;
            var fechas = new DateTime[count];
            var cuadrillaIds = new int?[count];
            var ubicaciones = new string[count];
            var solicitados = new string[count];
            var numerosOficio = new string?[count];
            var categoriaIds = new int[count];
            var mls = new decimal[count];
            var m2s = new decimal[count];
            var m3s = new decimal[count];
            var kgs = new decimal[count];
            decimal totalKg = 0m;

            for (int i = 0; i < count; i++)
            {
                var item = request.Registros[i];
                var catNombre = string.IsNullOrWhiteSpace(item.Categoria) ? "Otros" : item.Categoria.Trim();
                var kgCalculado = item.M3 > 0 ? Math.Round(item.M3 * _factorKgPorM3, 2) : 0m;
                totalKg += kgCalculado;

                fechas[i] = item.Fecha.ToDateTime(TimeOnly.MinValue);
                cuadrillaIds[i] = item.NoCuadrilla.HasValue && cuadrillasMap.TryGetValue(item.NoCuadrilla.Value, out var cId)
                    ? cId
                    : null;
                ubicaciones[i] = item.Ubicacion.Trim();
                solicitados[i] = string.IsNullOrWhiteSpace(item.SolicitadoRaw) ? "Sin especificar" : item.SolicitadoRaw.Trim();
                numerosOficio[i] = string.IsNullOrWhiteSpace(item.NumeroOficio) ? null : item.NumeroOficio.Trim();
                categoriaIds[i] = categoriasMap[catNombre];
                mls[i] = item.Ml;
                m2s[i] = item.M2;
                m3s[i] = item.M3;
                kgs[i] = kgCalculado;
            }

            // Inserción masiva en 1 solo viaje a Supabase
            const string bulkInsertRegistrosSql = """
                INSERT INTO registros_operativos
                    (carga_id, fecha, cuadrilla_id, ubicacion, solicitado_raw, numero_oficio, categoria_id, ml, m2, m3, kg)
                SELECT
                    @CargaId,
                    CAST(u.fecha AS DATE),
                    u.cuadrilla_id,
                    u.ubicacion,
                    u.solicitado_raw,
                    u.numero_oficio,
                    u.categoria_id,
                    u.ml,
                    u.m2,
                    u.m3,
                    u.kg
                FROM UNNEST(
                    @Fechas,
                    @CuadrillaIds,
                    @Ubicaciones,
                    @Solicitados,
                    @NumerosOficio,
                    @CategoriaIds,
                    @Mls,
                    @M2s,
                    @M3s,
                    @Kgs
                ) AS u(fecha, cuadrilla_id, ubicacion, solicitado_raw, numero_oficio, categoria_id, ml, m2, m3, kg)
                RETURNING id;
                """;

            var registroIds = (await conn.QueryAsync<long>(bulkInsertRegistrosSql, new
            {
                CargaId = cargaId,
                Fechas = fechas,
                CuadrillaIds = cuadrillaIds,
                Ubicaciones = ubicaciones,
                Solicitados = solicitados,
                NumerosOficio = numerosOficio,
                CategoriaIds = categoriaIds,
                Mls = mls,
                M2s = m2s,
                M3s = m3s,
                Kgs = kgs
            }, tx)).ToList();

            var relRegIds = new List<long>();
            var relActIds = new List<int>();

            for (int i = 0; i < count; i++)
            {
                var regId = registroIds[i];
                foreach (var actRaw in request.Registros[i].Actividades
                             .Where(a => !string.IsNullOrWhiteSpace(a))
                             .Select(a => a.Trim().ToUpperInvariant())
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    relRegIds.Add(regId);
                    relActIds.Add(actividadesMap[actRaw]);
                }
            }

            if (relRegIds.Count > 0)
            {
                const string bulkInsertActSql = """
                    INSERT INTO registro_actividades (registro_id, actividad_id)
                    SELECT * FROM UNNEST(@RegIds, @ActIds)
                    ON CONFLICT DO NOTHING;
                    """;

                await conn.ExecuteAsync(bulkInsertActSql, new
                {
                    RegIds = relRegIds.ToArray(),
                    ActIds = relActIds.ToArray()
                }, tx);
            }

            tx.Commit();

            return new ConfirmarReporteResponse
            {
                CargaId = cargaId,
                Mes = mesFinal,
                Anio = anioFinal,
                NombreArchivo = request.NombreArchivo.Trim(),
                TotalRegistrosGuardados = count,
                TotalKgCalculado = totalKg
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<CargaMensualDto>> ListarCargasAsync()
    {
        const string sql = """
            SELECT
                cm.id,
                cm.mes,
                cm.anio,
                cm.nombre_archivo,
                cm.usuario_id,
                cm.fecha_carga,
                COUNT(ro.id)::BIGINT AS total_registros
            FROM cargas_mensuales cm
            LEFT JOIN registros_operativos ro ON ro.carga_id = cm.id
            GROUP BY cm.id, cm.mes, cm.anio, cm.nombre_archivo, cm.usuario_id, cm.fecha_carga
            ORDER BY cm.anio DESC, cm.mes DESC, cm.fecha_carga DESC;
            """;

        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<CargaMensualDto>(sql);
    }
}
