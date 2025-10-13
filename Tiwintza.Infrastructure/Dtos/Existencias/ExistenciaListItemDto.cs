namespace Tiwintza.Infrastructure.Dtos.Existencias;

public sealed class ExistenciaListItemDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int NivelSeguridad { get; set; }
    public int NivelMaximo { get; set; }
    public int NivelMinimo { get; set; }
    public int NivelCritico { get; set; }
    public ExistenciaNivelEstado NivelEstado { get; set; } = ExistenciaNivelEstado.Desconocido;
    public string? Proveedor { get; set; }
    public long? ProveedorId { get; set; }
}
