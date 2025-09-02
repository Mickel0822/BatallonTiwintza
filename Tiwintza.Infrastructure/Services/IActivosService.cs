// IActivosService.cs
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos;

namespace Tiwintza.Infrastructure.Services;

public interface IActivosService
{
    Task<PagedResult<ActivoListItemDto>> BuscarAsync(ActivoFiltro filtro, CancellationToken ct = default);

    // KPIs con filtros (ignora IncluirBaja; separa Operativos vs EnBaja)
    Task<(int total, int operativos, int enBaja)> ResumenAsync(ActivoFiltro filtro, CancellationToken ct = default);

    // Catálogos para los combos
    Task<(IReadOnlyList<IdNombreDto> areas,
          IReadOnlyList<IdNombreDto> estados,
          IReadOnlyList<IdNombreDto> tipos)> CatalogosAsync(CancellationToken ct = default);
}
