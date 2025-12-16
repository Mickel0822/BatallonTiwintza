using System;
using System.Collections.Generic;

namespace Tiwintza.Infrastructure.Dtos.Auditoria;

/// <summary>
/// DTO para mostrar grupos de auditoría agrupadas por transacción
/// </summary>
public sealed class AuditoriaGroupedDto
{
    public string TransactionId { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string AccionUsuario { get; set; } = string.Empty;
    public int TotalOperaciones { get; set; }
    public List<AuditoriaListItemDto> Detalles { get; set; } = new();
    
    /// <summary>
    /// Indica si este grupo solo tiene 1 operación (no agrupado)
    /// </summary>
    public bool EsOperacionSimple => TotalOperaciones == 1;
}
