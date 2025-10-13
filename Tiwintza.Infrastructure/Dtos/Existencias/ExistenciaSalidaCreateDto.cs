namespace Tiwintza.Infrastructure.Dtos.Existencias;

public sealed class ExistenciaSalidaCreateDto
{
    public long ExistenciaId { get; set; }
    public DateOnly Fecha { get; set; }
    public int Cantidad { get; set; }
    public long AreaId { get; set; }
    public string? Responsable { get; set; }
    public string? Observacion { get; set; }
}
