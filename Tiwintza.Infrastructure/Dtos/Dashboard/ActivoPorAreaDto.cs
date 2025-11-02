using System;

namespace Tiwintza.Infrastructure.Dtos.Dashboard;

public sealed class ActivoPorAreaDto
{
    public long ActivoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public decimal ValorUnitario { get; init; }
    public DateOnly? FechaCompra { get; init; }
}
