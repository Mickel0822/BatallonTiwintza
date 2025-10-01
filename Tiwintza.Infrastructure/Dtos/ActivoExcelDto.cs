using System;

namespace Tiwintza.Infrastructure.Dtos;

public sealed class ActivoExcelDto
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public string? Marca { get; init; }
    public string? Modelo { get; init; }
    public string? Serie { get; init; }
    public string? Material { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public bool EnBaja { get; init; }
    public string Area { get; init; } = string.Empty;
    public decimal ValorUnitario { get; init; }
    public DateOnly? FechaCompra { get; init; }
    public string? Proveedor { get; init; }
    public int? VidaUtilMeses { get; init; }
    public decimal? DepreciacionMensual { get; init; }
    public bool DocumentoAutorizacion { get; init; }
    public int? GarantiaMeses { get; init; }
    public string? Observaciones { get; init; }
}

