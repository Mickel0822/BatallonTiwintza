using System;

namespace Tiwintza.Infrastructure.Dtos.Auditoria;

public sealed class AuditoriaFiltroDto
{
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public string? Usuario { get; init; }
    public string? Entidad { get; init; }
    public string? Accion { get; init; }
    public string? Texto { get; init; }
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
