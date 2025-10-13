using System.Collections.Generic;
using System.Linq;

namespace Tiwintza.Infrastructure.Dtos.Existencias;

public sealed class ExistenciaIngresoCreateDto
{
    public long ProveedorId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? NumeroFactura { get; set; }
    public IReadOnlyList<ExistenciaIngresoDetalleDto> Detalles { get; set; } = new List<ExistenciaIngresoDetalleDto>();

    public decimal Total => Detalles.Sum(d => d.Total);
}

public sealed class ExistenciaIngresoDetalleDto
{
    public long ExistenciaId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }

    public decimal Total => Cantidad * CostoUnitario;
}
