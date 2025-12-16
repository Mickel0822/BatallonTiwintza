namespace Tiwintza.Infrastructure.Dtos.Dashboard;

public sealed class DashboardKpiDto
{
    public required string Title { get; init; }
    public int Actual { get; init; }
    public int? Previous { get; init; }
    public double? VariationPercent
    {
        get
        {
            if (Previous is null || Previous.Value == 0) return Actual == 0 ? 0 : (double?)null;
            var diff = Actual - Previous.Value;
            return diff / (double)Previous.Value * 100d;
        }
    }
}
