using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Dtos.Dashboard;

namespace Tiwintza.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DashboardService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardSummaryDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var hoy = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);

        // Activos operativos (no dados de baja)
        var activosOperativos = await db.Activo.AsNoTracking()
            .Where(a => !db.BajaActivo.Any(b => b.ActivoId == a.Id))
            .CountAsync(ct);

        // Items críticos en existencias
        var itemsCriticos = await db.Existencia.AsNoTracking()
            .Where(e => e.StockActual <= e.NivelCritico)
            .CountAsync(ct);

        // Bajas del mes actual
        var bajasDelMes = await db.BajaActivo.AsNoTracking()
            .Where(b => b.FechaBaja >= DateOnly.FromDateTime(inicioMes) && b.FechaBaja <= DateOnly.FromDateTime(finMes))
            .CountAsync(ct);

        // Últimos movimientos DEL DÍA ACTUAL (auditorías del día)
        var movimientos = await db.Auditoria.AsNoTracking()
            .Where(a => a.FechaHora >= hoy && a.FechaHora < hoy.AddDays(1))
            .OrderByDescending(a => a.FechaHora)
            .Take(10)
            .Select(a => new DashboardMovimientoDto
            {
                Codigo = a.IdEntidad.HasValue ? a.IdEntidad.Value.ToString() : "-",
                Fecha = a.FechaHora,
                Tipo = MapearTipoMovimiento(a.Entidad, a.Accion),
                Descripcion = a.AccionUsuario ?? $"{a.Accion} en {a.Entidad}",
                Usuario = a.Usuario,
                Items = null
            })
            .ToListAsync(ct);

        return new DashboardSummaryDto
        {
            ActivosOperativos = new DashboardKpiDto
            {
                Title = "Activos operativos",
                Actual = activosOperativos,
                Previous = null
            },
            ItemsCriticos = new DashboardKpiDto
            {
                Title = "Items en crítico",
                Actual = itemsCriticos,
                Previous = null
            },
            BajasDelMes = new DashboardKpiDto
            {
                Title = "Bajas del mes",
                Actual = bajasDelMes,
                Previous = null
            },
            UltimosMovimientos = movimientos
        };
    }

    public async Task<IReadOnlyList<DashboardMovimientoDto>> ObtenerMovimientosDelDiaAsync(DateOnly fecha, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var inicio = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fin = inicio.AddDays(1);

        return await db.Auditoria.AsNoTracking()
            .Where(a => a.FechaHora >= inicio && a.FechaHora < fin)
            .OrderByDescending(a => a.FechaHora)
            .Select(a => new DashboardMovimientoDto
            {
                Codigo = a.IdEntidad.HasValue ? a.IdEntidad.Value.ToString() : "-",
                Fecha = a.FechaHora,
                Tipo = MapearTipoMovimiento(a.Entidad, a.Accion),
                Descripcion = a.AccionUsuario ?? $"{a.Accion} en {a.Entidad}",
                Usuario = a.Usuario,
                Items = null
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AreaOptionDto>> ObtenerAreasAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Area.AsNoTracking()
            .OrderBy(a => a.Nombre)
            .Select(a => new AreaOptionDto
            {
                Id = a.Id,
                Nombre = a.Nombre
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivoPorAreaDto>> ObtenerActivosPorAreaAsync(long areaId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Activo.AsNoTracking()
            .Where(a => a.AreaId == areaId)
            .OrderBy(a => a.CodigoInventario)
            .Select(a => new ActivoPorAreaDto
            {
                Codigo = a.CodigoInventario,
                Nombre = a.Nombre,
                Tipo = a.Tipo.Nombre,
                Estado = a.Estado.Nombre,
                ValorUnitario = a.ValorUnitario,
                FechaCompra = a.FechaCompra,
                Area = a.Area.Nombre
            })
            .ToListAsync(ct);
    }

    private static DashboardMovimientoTipo MapearTipoMovimiento(string entidad, string accion)
    {
        if (entidad == "compra" || entidad == "detalle_compra")
            return DashboardMovimientoTipo.Ingreso;

        if (entidad == "salida" || entidad == "detalle_salida")
            return DashboardMovimientoTipo.Salida;

        if (entidad == "traslado")
            return DashboardMovimientoTipo.Traslado;

        if (entidad == "baja_activo")
            return DashboardMovimientoTipo.Baja;

        return DashboardMovimientoTipo.Ingreso;
    }
}
