using LimpiezaUrbana.API.DTOs;
using LimpiezaUrbana.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimpiezaUrbana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportesController : ControllerBase
{
    private readonly IExcelPreviewService _excelPreviewService;

    public ReportesController(IExcelPreviewService excelPreviewService)
    {
        _excelPreviewService = excelPreviewService;
    }

    [HttpPost("previsualizar")]
    [Authorize(Roles = "Capturador,Desarrollador")]
    [Consumes("multipart/form-data")]
    public ActionResult<PrevisualizarReporteResponse> Previsualizar(IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { mensaje = "Debe adjuntar un archivo de Excel (.xlsx) válido." });
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
        {
            return BadRequest(new { mensaje = "Formato no soportado. Solo se aceptan archivos .xlsx o .xls." });
        }

        using var stream = archivo.OpenReadStream();
        var resultado = _excelPreviewService.ProcesarArchivo(stream, archivo.FileName);
        return Ok(resultado);
    }
}
