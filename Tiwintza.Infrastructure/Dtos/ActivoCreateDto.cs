using System;

namespace Tiwintza.Infrastructure.Dtos;

public class ActivoCreateDto
{
    public string CodigoInventario { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public long TipoId { get; set; }
    public string? Descripcion { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Serie { get; set; }
    public string? Material { get; set; }
    public long EstadoId { get; set; }
    public long AreaId { get; set; }
    public decimal ValorUnitario { get; set; }
    public DateOnly? FechaCompra { get; set; }
    public long? ProveedorId { get; set; }
    public int? VidaUtilMeses { get; set; }
    public decimal? DepreciacionMensual { get; set; }
    public bool DocumentoAutorizacion { get; set; }
    public int? GarantiaMeses { get; set; }
    public string? Observaciones { get; set; }
}
