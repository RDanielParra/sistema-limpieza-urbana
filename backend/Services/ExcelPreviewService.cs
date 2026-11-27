using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using LimpiezaUrbana.API.DTOs;

namespace LimpiezaUrbana.API.Services;

public interface IExcelPreviewService
{
    PrevisualizarReporteResponse ProcesarArchivo(Stream archivoStream, string nombreArchivo);
}

public sealed class ExcelPreviewService : IExcelPreviewService
{
    private const decimal FactorDefault = 1650m / 14m; // 117.85714285714286 kg/m3 exacto del Excel
    private readonly decimal _factorKgPorM3;

    private static readonly Dictionary<string, int> MesesEs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enero"] = 1, ["febrero"] = 2, ["marzo"] = 3, ["abril"] = 4,
        ["mayo"] = 5, ["junio"] = 6, ["julio"] = 7, ["agosto"] = 8,
        ["septiembre"] = 9, ["setiembre"] = 9, ["octubre"] = 10,
        ["noviembre"] = 11, ["diciembre"] = 12
    };

    public ExcelPreviewService(IConfiguration configuration)
    {
        _factorKgPorM3 = configuration.GetValue<decimal?>("Excel:FactorKgPorM3") ?? FactorDefault;
    }

    public PrevisualizarReporteResponse ProcesarArchivo(Stream archivoStream, string nombreArchivo)
    {
        var response = new PrevisualizarReporteResponse
        {
            NombreArchivo = nombreArchivo,
            FactorKgPorM3 = Math.Round(_factorKgPorM3, 4)
        };

        using var reader = ExcelReaderFactory.CreateReader(archivoStream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });

        var (anioLibro, mesLibro) = DetectarAnioMesLibro(dataSet);
        response.Mes = mesLibro;
        response.Anio = anioLibro;

        foreach (DataTable table in dataSet.Tables)
        {
            // Ignorar hojas irrelevantes al final (ej. "Mensual", "Tablas") o cualquier hoja que no sea día del mes (1..31)
            if (!int.TryParse(table.TableName.Trim(), out var diaHoja) || diaHoja is < 1 or > 31)
            {
                continue;
            }

            var headerRowIndex = EncontrarFilaEncabezados(table);
            if (headerRowIndex < 0)
            {
                response.Advertencias.Add($"Hoja '{table.TableName}' omitida: no se detectaron encabezados válidos.");
                continue;
            }

            response.TotalHojasLeidas++;
            var columnas = MapearColumnas(table, headerRowIndex);
            var fechaHoja = ExtraerFechaDeHoja(table, headerRowIndex)
                ?? ConstruirFechaDesdeLibro(anioLibro, mesLibro, diaHoja);

            var dataStartRow = columnas.SubHeaderRows > 0
                ? headerRowIndex + columnas.SubHeaderRows + 1
                : headerRowIndex + 1;

            for (int r = dataStartRow; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                var ubicacion = ObtenerTexto(row, columnas.UbicacionIndex);

                if (string.IsNullOrWhiteSpace(ubicacion) || EsFilaTotal(ubicacion))
                {
                    continue;
                }

                var solicitado = ObtenerTexto(row, columnas.SolicitadoIndex);
                var cuadrilla = ParsearCuadrilla(ObtenerTexto(row, columnas.CuadrillaIndex));

                var actividades = new List<string>();
                foreach (var (colIndex, codigoActividad) in columnas.Actividades)
                {
                    var valor = ObtenerTexto(row, colIndex);
                    if (string.IsNullOrWhiteSpace(valor))
                        continue;

                    if (EstaMarcadaActividad(valor))
                    {
                        actividades.Add(codigoActividad);
                    }
                    else if (!actividades.Contains(valor, StringComparer.OrdinalIgnoreCase))
                    {
                        // Texto personalizado en celdas combinadas de actividades (ej. "Animal Muerto", "Cacharros")
                        actividades.Add(valor);
                    }
                }

                var ml = ParsearDecimal(ObtenerTexto(row, columnas.MlIndex));
                var m2 = ParsearDecimal(ObtenerTexto(row, columnas.M2Index));
                var m3 = ParsearDecimal(ObtenerTexto(row, columnas.M3Index));

                // Regla crítica: ignorar columna kg del Excel y calcular siempre desde m3
                var kg = m3 > 0 ? Math.Round(m3 * _factorKgPorM3, 2) : 0m;

                response.Registros.Add(new RegistroPrevisualizacionDto
                {
                    IdTemporal = Guid.NewGuid().ToString("N"),
                    Hoja = table.TableName,
                    Fecha = fechaHoja,
                    NoCuadrilla = cuadrilla,
                    Ubicacion = ubicacion,
                    Actividades = actividades,
                    SolicitadoRaw = solicitado,
                    NumeroOficio = ExtraerNumeroOficio(solicitado),
                    Categoria = ClasificarCategoria(solicitado, ubicacion),
                    Ml = ml,
                    M2 = m2,
                    M3 = m3,
                    Kg = kg
                });
            }
        }

        response.TotalRegistros = response.Registros.Count;
        return response;
    }

    private static int EncontrarFilaEncabezados(DataTable table)
    {
        var maxScan = Math.Min(table.Rows.Count, 15);
        for (int i = 0; i < maxScan; i++)
        {
            var row = table.Rows[i];
            var textos = new List<string>();
            for (int c = 0; c < table.Columns.Count; c++)
            {
                textos.Add(Normalizar(row[c]?.ToString()));
            }

            if (textos.Any(t => t.Contains("ubicacion")) &&
                (textos.Any(t => t.Contains("solicit")) || textos.Any(t => t.Contains("cuadrilla"))))
            {
                return i;
            }
        }

        return -1;
    }

    private static ColumnMap MapearColumnas(DataTable table, int headerRowIndex)
    {
        var map = new ColumnMap();
        var headerRow = table.Rows[headerRowIndex];
        var colCount = table.Columns.Count;
        var kgIndex = -1;

        for (int c = 0; c < colCount; c++)
        {
            var rawHeader = headerRow[c]?.ToString()?.Trim() ?? string.Empty;
            var norm = Normalizar(rawHeader);
            if (string.IsNullOrWhiteSpace(norm))
                continue;

            if (map.CuadrillaIndex < 0 && norm.Contains("cuadrilla"))
            {
                map.CuadrillaIndex = c;
            }
            else if (map.UbicacionIndex < 0 && norm.Contains("ubicacion"))
            {
                map.UbicacionIndex = c;
            }
            else if (map.SolicitadoIndex < 0 && norm.Contains("solicit"))
            {
                map.SolicitadoIndex = c;
            }
            else if (map.MlIndex < 0 && (norm == "ml" || norm.Contains("(ml)") || norm.Contains("barrido manual") || norm.Contains("metros lineales")))
            {
                map.MlIndex = c;
            }
            else if (map.M2Index < 0 && (norm == "m2" || norm.Contains("area") || norm.Contains("m²") || norm.Contains("metros cuadrados")))
            {
                map.M2Index = c;
            }
            else if (map.M3Index < 0 && (norm == "m3" || norm.Contains("m³") || norm.Contains("metros cubicos")))
            {
                map.M3Index = c;
            }
            else if (kgIndex < 0 && (norm == "kg" || norm.Contains("peso") || norm.Contains("kilogramo")))
            {
                // Ignorar columna kg de fórmula del Excel por regla de negocio; además marca el fin de la tabla principal
                kgIndex = c;
                break;
            }
        }

        // Detectar columnas de actividades entre Ubicaciones y Solicito (formato con subencabezados en 2 filas: B+M=BM, C+Z=CZ, etc.)
        if (map.UbicacionIndex >= 0 && map.SolicitadoIndex > map.UbicacionIndex + 1)
        {
            var hasRow1 = headerRowIndex + 1 < table.Rows.Count;
            var hasRow2 = headerRowIndex + 2 < table.Rows.Count;
            var sub1 = hasRow1 ? table.Rows[headerRowIndex + 1] : null;
            var sub2 = hasRow2 ? table.Rows[headerRowIndex + 2] : null;

            for (int c = map.UbicacionIndex + 1; c < map.SolicitadoIndex; c++)
            {
                var p1 = sub1?[c]?.ToString()?.Trim() ?? string.Empty;
                var p2 = sub2?[c]?.ToString()?.Trim() ?? string.Empty;
                var codigo = $"{p1}{p2}".Trim();

                if (!string.IsNullOrWhiteSpace(codigo) && codigo.Length <= 4 && codigo.All(char.IsLetter))
                {
                    map.Actividades.Add((c, codigo.ToUpperInvariant()));
                    map.SubHeaderRows = 2;
                }
                else
                {
                    var fallback = headerRow[c]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(fallback))
                    {
                        map.Actividades.Add((c, fallback));
                    }
                }
            }
        }

        return map;
    }

    private static (int? Anio, int? Mes) DetectarAnioMesLibro(DataSet dataSet)
    {
        foreach (DataTable table in dataSet.Tables)
        {
            if (!int.TryParse(table.TableName.Trim(), out _))
                continue;

            var headerIdx = EncontrarFilaEncabezados(table);
            if (headerIdx < 0) continue;

            var fecha = ExtraerFechaDeHoja(table, headerIdx);
            if (fecha.HasValue)
            {
                return (fecha.Value.Year, fecha.Value.Month);
            }
        }

        return (null, null);
    }

    private static DateOnly? ExtraerFechaDeHoja(DataTable table, int headerRowIndex)
    {
        var maxRow = Math.Min(headerRowIndex, table.Rows.Count);
        for (int r = 0; r < maxRow; r++)
        {
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var raw = table.Rows[r][c]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(raw) || !raw.Contains("Fecha", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Ejemplo: "Fecha: 01 Agosto 2026"
                var match = Regex.Match(raw, @"(\d{1,2})\s+([A-Za-záéíóúÁÉÍÓÚñÑ]+)\s+(\d{4})");
                if (match.Success &&
                    int.TryParse(match.Groups[1].Value, out var dia) &&
                    MesesEs.TryGetValue(Normalizar(match.Groups[2].Value), out var mes) &&
                    int.TryParse(match.Groups[3].Value, out var anio))
                {
                    if (dia >= 1 && dia <= DateTime.DaysInMonth(anio, mes))
                    {
                        return new DateOnly(anio, mes, dia);
                    }
                }
            }
        }

        return null;
    }

    private static DateOnly? ConstruirFechaDesdeLibro(int? anio, int? mes, int dia)
    {
        if (!anio.HasValue || !mes.HasValue)
            return null;

        if (dia < 1 || dia > DateTime.DaysInMonth(anio.Value, mes.Value))
            return null;

        return new DateOnly(anio.Value, mes.Value, dia);
    }

    private static string? ExtraerNumeroOficio(string solicitado)
    {
        if (string.IsNullOrWhiteSpace(solicitado))
            return null;

        // Ejemplos: "CIGA: 519604", "CIGA: STTGI2", "Oficio: OP-16308/2026"
        var match = Regex.Match(solicitado, @"(?:CIGA|OFICIO)\s*[:#\-]?\s*([A-Za-z0-9\-\/]+)", RegexOptions.IgnoreCase);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private static string ClasificarCategoria(string solicitado, string ubicacion)
    {
        var solNorm = Normalizar(solicitado);
        var ubiNorm = Normalizar(ubicacion);

        if (solNorm.Contains("ciga"))
            return "CIGA";

        if (solNorm.Contains("oficio"))
            return "OFICIO";

        if (solNorm.Contains("lic") || solNorm.Contains("recorrido") || !string.IsNullOrWhiteSpace(solNorm))
            return "Recorrido Diario";

        if (ubiNorm.Contains("ciga"))
            return "CIGA";

        return "Otros";
    }

    private static bool EstaMarcadaActividad(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        var trimmed = valor.Trim();
        return trimmed.Contains('✓')
            || trimmed.Contains('✔')
            || trimmed.Equals("x", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("si", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("sí", StringComparison.OrdinalIgnoreCase)
            || trimmed == "1";
    }

    private static int? ParsearCuadrilla(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var match = Regex.Match(valor, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var num) && num is >= 1 and <= 15)
        {
            return num;
        }

        return null;
    }

    private static decimal ParsearDecimal(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return 0m;

        var limpio = valor.Trim().Replace(",", ".");
        return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) && num >= 0
            ? num
            : 0m;
    }

    private static bool EsFilaTotal(string ubicacion)
    {
        var norm = Normalizar(ubicacion);
        return norm.StartsWith("total") || norm == "totales" || norm == "subtotal";
    }

    private static string ObtenerTexto(DataRow row, int index)
    {
        if (index < 0 || index >= row.Table.Columns.Count)
            return string.Empty;

        return row[index]?.ToString()?.Trim() ?? string.Empty;
    }

    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var formD = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed class ColumnMap
    {
        public int CuadrillaIndex { get; set; } = -1;
        public int UbicacionIndex { get; set; } = -1;
        public int SolicitadoIndex { get; set; } = -1;
        public int MlIndex { get; set; } = -1;
        public int M2Index { get; set; } = -1;
        public int M3Index { get; set; } = -1;
        public int SubHeaderRows { get; set; }
        public List<(int Index, string Codigo)> Actividades { get; } = new();
    }
}
