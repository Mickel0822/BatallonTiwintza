using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos.Auditoria;

namespace Tiwintza.Infrastructure.Services;

public interface IAuditoriaService
{
    Task<AuditoriaCatalogosDto> ObtenerCatalogosAsync(CancellationToken ct = default);
    Task<PagedResult<AuditoriaListItemDto>> BuscarAsync(AuditoriaFiltroDto filtro, CancellationToken ct = default);
    Task<IReadOnlyList<AuditoriaListItemDto>> ExportarAsync(AuditoriaFiltroDto filtro, CancellationToken ct = default);
}
