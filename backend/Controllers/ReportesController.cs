using System.Security.Claims;
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
    private readonly IReportesPersistenceService _persistenceService;

    public ReportesController(
        IExcelPreviewService excelPreviewService,
        IReportesPersistenceService persistenceService)
    {
        _excelPreviewService = excelPreviewService;
        _persistenceService = persistenceService;
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

    [HttpPost("confirmar")]
    [Authorize(Roles = "Capturador,Desarrollador")]
    public async Task<ActionResult<ConfirmarReporteResponse>> Confirmar([FromBody] ConfirmarReporteRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var usuarioId))
        {
            return Unauthorized(new { mensaje = "Token inválido: no se encontró el identificador de usuario." });
        }

        var resultado = await _persistenceService.ConfirmarAsync(request, usuarioId);
        return Ok(resultado);
    }

    [HttpGet("cargas")]
    [Authorize(Roles = "Capturador,Coordinador,Desarrollador")]
    public async Task<ActionResult<IEnumerable<CargaMensualDto>>> ListarCargas()
    {
        var cargas = await _persistenceService.ListarCargasAsync();
        return Ok(cargas);
    }
}
