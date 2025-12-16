using System;

namespace Tiwintza.Infrastructure.Dtos.Auditoria;

public sealed class AuditoriaListItemDto
{
    public long Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public long? EntidadId { get; set; }
    public string? Resumen { get; set; }
    public string? DetalleJson { get; set; }
    public string? TransactionId { get; set; }
    public string? AccionUsuario { get; set; }
}
