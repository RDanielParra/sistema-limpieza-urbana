using LimpiezaUrbana.API.DTOs;
using LimpiezaUrbana.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LimpiezaUrbana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Coordinador,Desarrollador")]
public sealed class EstadisticasController : ControllerBase
{
    private readonly IEstadisticasService _estadisticasService;

    public EstadisticasController(IEstadisticasService estadisticasService)
    {
        _estadisticasService = estadisticasService;
    }

    [HttpGet]
    public async Task<ActionResult<EstadisticasResponse>> Obtener([FromQuery] EstadisticasFiltroQuery filtro)
    {
        if (filtro.Mes is < 1 or > 12)
        {
            return BadRequest(new { mensaje = "El parámetro 'mes' debe estar entre 1 y 12." });
        }

        if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaInicio > filtro.FechaFin)
        {
            return BadRequest(new { mensaje = "La 'fechaInicio' no puede ser mayor que 'fechaFin'." });
        }

        var resultado = await _estadisticasService.ObtenerEstadisticasAsync(filtro);
        return Ok(resultado);
    }

    [HttpGet("categorias")]
    public async Task<ActionResult<IEnumerable<CategoriaCatalogoDto>>> ObtenerCategorias()
    {
        var categorias = await _estadisticasService.ObtenerCategoriasAsync();
        return Ok(categorias);
    }

    [HttpGet("registros")]
    public async Task<ActionResult<IEnumerable<RegistroOperativoConsultaDto>>> ListarRegistros(
        [FromQuery] EstadisticasFiltroQuery filtro,
        [FromQuery] int limite = 50,
        [FromQuery] int offset = 0)
    {
        if (filtro.Mes is < 1 or > 12)
        {
            return BadRequest(new { mensaje = "El parámetro 'mes' debe estar entre 1 y 12." });
        }

        if (filtro.FechaInicio.HasValue && filtro.FechaFin.HasValue && filtro.FechaInicio > filtro.FechaFin)
        {
            return BadRequest(new { mensaje = "La 'fechaInicio' no puede ser mayor que 'fechaFin'." });
        }

        var registros = await _estadisticasService.ListarRegistrosAsync(filtro, limite, offset);
        return Ok(registros);
    }
}
