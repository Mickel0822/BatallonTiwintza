namespace Tiwintza.Infrastructure.Dtos.Catalogos;

public sealed class ProveedorDetalleDto
{
    public long Id { get; init; }
    public string Ruc { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? Contacto { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
}
