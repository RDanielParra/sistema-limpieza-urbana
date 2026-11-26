using System.Data;
using System.Globalization;
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
    private readonly decimal _factorKgPorM3;

    public ExcelPreviewService(IConfiguration configuration)
    {
        _factorKgPorM3 = configuration.GetValue<decimal?>("Excel:FactorKgPorM3") ?? 1000m;
    }

    public PrevisualizarReporteResponse ProcesarArchivo(Stream archivoStream, string nombreArchivo)
    {
        var response = new PrevisualizarReporteResponse
        {
            NombreArchivo = nombreArchivo,
            FactorKgPorM3 = _factorKgPorM3
        };

        using var reader = ExcelReaderFactory.CreateReader(archivoStream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });

        foreach (DataTable table in dataSet.Tables)
        {
            var headerRowIndex = EncontrarFilaEncabezados(table);
            if (headerRowIndex < 0)
            {
                response.Advertencias.Add($"Hoja '{table.TableName}' omitida: no se detectaron encabezados válidos.");
                continue;
            }

            response.TotalHojasLeidas++;
            var columnas = MapearColumnas(table.Rows[headerRowIndex]);
            var fechaHoja = IntentarObtenerFechaDeHoja(table.TableName);

            for (int r = headerRowIndex + 1; r < table.Rows.Count; r++)
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
                foreach (var (colIndex, nombreActividad) in columnas.Actividades)
                {
                    var valor = ObtenerTexto(row, colIndex);
                    if (EstaMarcadaActividad(valor))
                    {
                        actividades.Add(nombreActividad);
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
                (textos.Any(t => t.Contains("solicitado")) || textos.Any(t => t.Contains("cuadrilla"))))
            {
                return i;
            }
        }

        return -1;
    }

    private static ColumnMap MapearColumnas(DataRow headerRow)
    {
        var map = new ColumnMap();
        var colCount = headerRow.Table.Columns.Count;

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
            else if (map.SolicitadoIndex < 0 && norm.Contains("solicitado"))
            {
                map.SolicitadoIndex = c;
            }
            else if (map.MlIndex < 0 && (norm == "ml" || norm.Contains("metros lineales")))
            {
                map.MlIndex = c;
            }
            else if (map.M2Index < 0 && (norm == "m2" || norm.Contains("metros cuadrados")))
            {
                map.M2Index = c;
            }
            else if (map.M3Index < 0 && (norm == "m3" || norm.Contains("metros cubicos")))
            {
                map.M3Index = c;
            }
            else if (norm == "kg" || norm.Contains("kilogramo"))
            {
                // Ignorar columna kg de fórmula del Excel por regla de negocio
                continue;
            }
            else
            {
                // Columnas restantes entre ubicación/solicitado/cantidades corresponden a actividades
                map.Actividades.Add((c, rawHeader));
            }
        }

        return map;
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

    private static DateOnly? IntentarObtenerFechaDeHoja(string nombreHoja)
    {
        if (DateOnly.TryParse(nombreHoja.Trim(), new CultureInfo("es-MX"), DateTimeStyles.None, out var fecha))
        {
            return fecha;
        }

        return null;
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

        var formD = texto.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private sealed class ColumnMap
    {
        public int CuadrillaIndex { get; set; } = -1;
        public int UbicacionIndex { get; set; } = -1;
        public int SolicitadoIndex { get; set; } = -1;
        public int MlIndex { get; set; } = -1;
        public int M2Index { get; set; } = -1;
        public int M3Index { get; set; } = -1;
        public List<(int Index, string Nombre)> Actividades { get; } = new();
    }
}
