namespace Tiwintza.Infrastructure.Dtos;

public sealed class ActivoFiltro
{
    public string? Texto { get; set; }
    public long? AreaId { get; set; }
    public long? EstadoId { get; set; }
    public long? TipoId { get; set; }
    public bool IncluirBaja { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; } = "codigo";
    public bool SortDesc { get; set; } = false;
}
