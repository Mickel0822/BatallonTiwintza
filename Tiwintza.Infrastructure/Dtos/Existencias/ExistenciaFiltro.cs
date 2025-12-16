namespace Tiwintza.Infrastructure.Dtos.Existencias;

public sealed class ExistenciaFiltro
{
    public string? Texto { get; set; }
    public ExistenciaNivelEstado? Nivel { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}
