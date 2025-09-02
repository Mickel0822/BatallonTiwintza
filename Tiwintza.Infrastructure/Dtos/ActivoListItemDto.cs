namespace Tiwintza.Infrastructure.Dtos;

public sealed class ActivoListItemDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Tipo { get; init; } = "";
    public string Estado { get; init; } = "";
    public bool EnBaja { get; init; }          // true si tiene registro(s) en BajaActivo
    public string Area { get; init; } = "";
    public decimal Valor { get; init; }
    public DateOnly? FechaCompra { get; init; }

    // Para UI (badge / texto)
    public string EstadoUi => EnBaja ? "Baja" : Estado;
}
