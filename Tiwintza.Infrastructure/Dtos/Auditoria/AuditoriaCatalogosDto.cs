using System.Collections.Generic;

namespace Tiwintza.Infrastructure.Dtos.Auditoria;

public sealed class AuditoriaCatalogosDto
{
    public required IReadOnlyList<string> Usuarios { get; init; }
    public required IReadOnlyList<string> Entidades { get; init; }
    public required IReadOnlyList<string> Acciones { get; init; }
}
