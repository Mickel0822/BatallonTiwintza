using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Dtos.Dashboard;

namespace Tiwintza.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        // Trabajamos todo en base a UTC para los TIMESTAMPTZ
        var nowUtc = DateTime.UtcNow;

        // Para columnas DATE, usa DateOnly sin problema
        var hoy = DateOnly.FromDateTime(nowUtc);
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);
        var finMesPrev = inicioMes.AddDays(-1);
        var inicioMesPrev = inicioMes.AddMonths(-1);

        // .NET 8 tiene overload con DateTimeKind. Si no, usa SpecifyKind(...)
        var finMesPrevUtc = finMesPrev.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        // (hoyDateTime no se usa; puedes eliminar esa variable)

        // Activos operativos actuales (usa DATE para FechaBaja => OK)
        var operativosActual = await _db.Activo.AsNoTracking()
            .CountAsync(a => !a.BajaActivo.Any() || a.BajaActivo.All(b => b.FechaBaja > hoy), ct);

        // Para CreadoEn (TIMESTAMPTZ), compara con DateTime UTC
        var operativosPrevio = await _db.Activo.AsNoTracking()
            .CountAsync(a => a.CreadoEn <= finMesPrevUtc &&
                             (!a.BajaActivo.Any() || a.BajaActivo.All(b => b.FechaBaja > finMesPrev)), ct);

        var kpiActivos = new DashboardKpiDto { Title = "Activos Operativos", Actual = operativosActual, Previous = operativosPrevio };

        var itemsCriticosActual = await _db.Existencia.AsNoTracking()
            .CountAsync(e => e.StockActual <= e.NivelCritico, ct);

        var kpiCriticos = new DashboardKpiDto { Title = "Ítems en Crítico", Actual = itemsCriticosActual, Previous = null };

        // Bajas por mes (FechaBaja es DATE, aquí no hay problema)
        var bajasMesActual = await _db.BajaActivo.AsNoTracking()
            .CountAsync(b => b.FechaBaja >= inicioMes && b.FechaBaja <= finMes, ct);

        var bajasMesAnterior = await _db.BajaActivo.AsNoTracking()
            .CountAsync(b => b.FechaBaja >= inicioMesPrev && b.FechaBaja <= finMesPrev, ct);

        var kpiBajas = new DashboardKpiDto { Title = "Bajas del Mes", Actual = bajasMesActual, Previous = bajasMesAnterior };

        var movimientos = await ObtenerMovimientosAsync(limit: 8, desde: null, hasta: null, ct);

        return new DashboardSummaryDto
        {
            ActivosOperativos = kpiActivos,
            ItemsCriticos = kpiCriticos,
            BajasDelMes = kpiBajas,
            UltimosMovimientos = movimientos
        };
    }

    public async Task<IReadOnlyList<AreaOptionDto>> ObtenerAreasAsync(CancellationToken ct = default)
    {
        var items = await _db.Area.AsNoTracking()
            .OrderBy(a => a.Nombre)
            .Select(a => new AreaOptionDto
            {
                Id = a.Id,
                Nombre = a.Nombre
            })
            .ToListAsync(ct);

        return items;
    }

    public async Task<IReadOnlyList<ActivoPorAreaDto>> ObtenerActivosPorAreaAsync(long areaId, CancellationToken ct = default)
    {
        var datos = await _db.Activo.AsNoTracking()
            .Where(a => a.AreaId == areaId)
            .OrderBy(a => a.Nombre)
            .Select(a => new ActivoPorAreaDto
            {
                ActivoId = a.Id,
                Codigo = a.CodigoInventario,
                Nombre = a.Nombre,
                Tipo = a.Tipo.Nombre,
                Estado = a.Estado.Nombre,
                Area = a.Area.Nombre,
                ValorUnitario = a.ValorUnitario,
                FechaCompra = a.FechaCompra
            })
            .ToListAsync(ct);

        return datos;
    }

    public async Task<IReadOnlyList<DashboardMovimientoDto>> ObtenerMovimientosDelDiaAsync(DateOnly dia, CancellationToken ct = default)
    {
        // IMPORTANTE: al filtrar por CreadoEn (TIMESTAMPTZ) usa UTC
        var desde = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var hasta = dia.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await ObtenerMovimientosAsync(limit: null, desde, hasta, ct);
    }

    private async Task<IReadOnlyList<DashboardMovimientoDto>> ObtenerMovimientosAsync(int? limit, DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        var limiteFuente = limit.HasValue ? Math.Max(limit.Value * 2, 10) : (int?)null;

        var comprasQuery = _db.Compra.AsNoTracking();
        if (desde.HasValue) comprasQuery = comprasQuery.Where(c => c.CreadoEn >= desde.Value);
        if (hasta.HasValue) comprasQuery = comprasQuery.Where(c => c.CreadoEn <= hasta.Value);
        comprasQuery = comprasQuery.OrderByDescending(c => c.CreadoEn);
        if (limiteFuente.HasValue) comprasQuery = comprasQuery.Take(limiteFuente.Value);

        var compras = await comprasQuery
            .Select(c => new
            {
                c.Id,
                c.CreadoEn,
                c.NumFactura,
                Proveedor = c.Proveedor.RazonSocial,
                TotalItems = c.DetalleCompra.Sum(d => (int?)d.Cantidad) ?? 0
            })
            .ToListAsync(ct);

        var salidasQuery = _db.Salida.AsNoTracking();
        if (desde.HasValue) salidasQuery = salidasQuery.Where(s => s.CreadoEn >= desde.Value);
        if (hasta.HasValue) salidasQuery = salidasQuery.Where(s => s.CreadoEn <= hasta.Value);
        salidasQuery = salidasQuery.OrderByDescending(s => s.CreadoEn);
        if (limiteFuente.HasValue) salidasQuery = salidasQuery.Take(limiteFuente.Value);

        var salidas = await salidasQuery
            .Select(s => new
            {
                s.Id,
                s.CreadoEn,
                Existencia = s.Existencia.Nombre,
                Area = s.Area.Nombre,
                s.Cantidad,
                s.Responsable
            })
            .ToListAsync(ct);

        var trasladosQuery = _db.TrasladoActivo.AsNoTracking();
        if (desde.HasValue) trasladosQuery = trasladosQuery.Where(t => t.CreadoEn >= desde.Value);
        if (hasta.HasValue) trasladosQuery = trasladosQuery.Where(t => t.CreadoEn <= hasta.Value);
        trasladosQuery = trasladosQuery.OrderByDescending(t => t.CreadoEn);
        if (limiteFuente.HasValue) trasladosQuery = trasladosQuery.Take(limiteFuente.Value);

        var traslados = await trasladosQuery
            .Select(t => new
            {
                t.Id,
                t.CreadoEn,
                Activo = t.Activo.Nombre,
                Origen = t.AreaOrigen.Nombre,
                Destino = t.AreaDestino.Nombre,
                t.Usuario
            })
            .ToListAsync(ct);

        var bajasQuery = _db.BajaActivo.AsNoTracking();
        if (desde.HasValue) bajasQuery = bajasQuery.Where(b => b.CreadoEn >= desde.Value);
        if (hasta.HasValue) bajasQuery = bajasQuery.Where(b => b.CreadoEn <= hasta.Value);
        bajasQuery = bajasQuery.OrderByDescending(b => b.CreadoEn);
        if (limiteFuente.HasValue) bajasQuery = bajasQuery.Take(limiteFuente.Value);

        var bajas = await bajasQuery
            .Select(b => new
            {
                b.Id,
                b.CreadoEn,
                Activo = b.Activo.Nombre,
                b.Responsable
            })
            .ToListAsync(ct);

        var resultado = new List<DashboardMovimientoDto>(
            compras.Count + salidas.Count + traslados.Count + bajas.Count);

        resultado.AddRange(compras.Select(c => new DashboardMovimientoDto
        {
            Codigo = $"ING-{c.Id:D5}",
            Fecha = c.CreadoEn,
            Tipo = DashboardMovimientoTipo.Ingreso,
            Descripcion = string.IsNullOrWhiteSpace(c.NumFactura)
                ? $"Compra registrada a {c.Proveedor}"
                : $"Compra {c.NumFactura} - {c.Proveedor}",
            Usuario = c.Proveedor,
            Items = c.TotalItems == 0 ? null : c.TotalItems
        }));

        resultado.AddRange(salidas.Select(s => new DashboardMovimientoDto
        {
            Codigo = $"SAL-{s.Id:D5}",
            Fecha = s.CreadoEn,
            Tipo = DashboardMovimientoTipo.Salida,
            Descripcion = $"Salida de {s.Existencia} hacia {s.Area}",
            Usuario = string.IsNullOrWhiteSpace(s.Responsable) ? null : s.Responsable,
            Items = s.Cantidad
        }));

        resultado.AddRange(traslados.Select(t => new DashboardMovimientoDto
        {
            Codigo = $"TRA-{t.Id:D5}",
            Fecha = t.CreadoEn,
            Tipo = DashboardMovimientoTipo.Traslado,
            Descripcion = $"Traslado de {t.Activo}: {t.Origen} -> {t.Destino}",
            Usuario = string.IsNullOrWhiteSpace(t.Usuario) ? null : t.Usuario,
            Items = 1
        }));

        resultado.AddRange(bajas.Select(b => new DashboardMovimientoDto
        {
            Codigo = $"BAJ-{b.Id:D5}",
            Fecha = b.CreadoEn,
            Tipo = DashboardMovimientoTipo.Baja,
            Descripcion = $"Baja de activo {b.Activo}",
            Usuario = string.IsNullOrWhiteSpace(b.Responsable) ? null : b.Responsable,
            Items = 1
        }));

        var ordered = resultado
            .OrderByDescending(m => m.Fecha)
            .ToList();

        if (limit.HasValue)
        {
            ordered = ordered.Take(limit.Value).ToList();
        }

        return ordered;
    }
}



