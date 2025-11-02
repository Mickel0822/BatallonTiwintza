using System;

namespace Tiwintza.Infrastructure.Dtos.Dashboard;

public sealed class DashboardMovimientoDto
{
    public required string Codigo { get; init; }
    public required DateTime Fecha { get; init; }
    public required DashboardMovimientoTipo Tipo { get; init; }
    public required string Descripcion { get; init; }
    public string? Usuario { get; init; }
    public int? Items { get; init; }
}

public enum DashboardMovimientoTipo
{
    Ingreso,
    Salida,
    Traslado,
    Baja
}
