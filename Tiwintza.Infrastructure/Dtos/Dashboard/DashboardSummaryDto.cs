using System.Collections.Generic;

namespace Tiwintza.Infrastructure.Dtos.Dashboard;

public sealed class DashboardSummaryDto
{
    public required DashboardKpiDto ActivosOperativos { get; init; }
    public required DashboardKpiDto ItemsCriticos { get; init; }
    public required DashboardKpiDto BajasDelMes { get; init; }
    public required IReadOnlyList<DashboardMovimientoDto> UltimosMovimientos { get; init; }
}
