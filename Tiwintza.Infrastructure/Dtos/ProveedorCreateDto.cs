namespace Tiwintza.Infrastructure.Dtos;

public sealed class ProveedorCreateDto
{
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
}

