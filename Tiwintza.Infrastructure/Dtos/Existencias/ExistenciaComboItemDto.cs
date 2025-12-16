namespace Tiwintza.Infrastructure.Dtos.Existencias;

public sealed class ExistenciaComboItemDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;

    public string Display => string.IsNullOrWhiteSpace(Codigo)
        ? Nombre
        : $"{Codigo} - {Nombre}";
}
