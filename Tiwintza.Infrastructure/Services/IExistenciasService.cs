using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos.Existencias;

namespace Tiwintza.Infrastructure.Services;

public interface IExistenciasService
{
    Task<PagedResult<ExistenciaListItemDto>> BuscarAsync(ExistenciaFiltro filtro, CancellationToken ct = default);
    Task<ExistenciaListItemDto?> ObtenerAsync(long id, CancellationToken ct = default);
}
