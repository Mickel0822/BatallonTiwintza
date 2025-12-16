using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Dashboard;

namespace Tiwintza.Infrastructure.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> ObtenerResumenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AreaOptionDto>> ObtenerAreasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivoPorAreaDto>> ObtenerActivosPorAreaAsync(long areaId, CancellationToken ct = default);
    Task<IReadOnlyList<DashboardMovimientoDto>> ObtenerMovimientosDelDiaAsync(DateOnly dia, CancellationToken ct = default);
}
