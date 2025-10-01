using System;

namespace Tiwintza.Infrastructure.Dtos;

public sealed class ActivoMovimientoDto
{
    public DateOnly Fecha { get; init; }
    public string AreaOrigen { get; init; } = string.Empty;
    public string AreaDestino { get; init; } = string.Empty;
    public string? Observacion { get; init; }
    public string? Usuario { get; init; }
}

