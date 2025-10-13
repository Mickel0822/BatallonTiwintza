using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using Tiwintza.Infrastructure.Dtos.Existencias;

namespace Tiwintza.Infrastructure.Services;

public sealed class ExistenciasService : IExistenciasService
{
    private readonly AppDbContext _db;

    public ExistenciasService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ExistenciaListItemDto>> BuscarAsync(ExistenciaFiltro filtro, CancellationToken ct = default)
    {
        filtro ??= new ExistenciaFiltro();

        var page = filtro.Page <= 0 ? 1 : filtro.Page;
        var pageSize = filtro.PageSize <= 0 ? 30 : Math.Clamp(filtro.PageSize, 5, 200);

        IQueryable<Existencia> query = _db.Existencia
            .AsNoTracking()
            .Include(e => e.ProveedorPref);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            query = query.Where(e =>
                EF.Functions.ILike(e.Codigo, $"%{texto}%") ||
                EF.Functions.ILike(e.Nombre, $"%{texto}%"));
        }

        if (filtro.Nivel is { } nivel && nivel != ExistenciaNivelEstado.Desconocido)
        {
            query = nivel switch
            {
                ExistenciaNivelEstado.Critico => query.Where(e => e.StockActual <= e.NivelCritico),
                ExistenciaNivelEstado.Bajo => query.Where(e => e.StockActual > e.NivelCritico && e.StockActual <= e.NivelMinimo),
                ExistenciaNivelEstado.Seguro => query.Where(e => e.StockActual > e.NivelMinimo && e.StockActual <= e.NivelSeguridad),
                ExistenciaNivelEstado.Normal => query.Where(e => e.StockActual > e.NivelSeguridad && e.StockActual <= e.NivelMaximo),
                ExistenciaNivelEstado.Excedido => query.Where(e => e.StockActual > e.NivelMaximo),
                _ => query
            };
        }

        query = ApplySort(query, filtro.SortBy, filtro.SortDesc);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.Codigo,
                e.Nombre,
                e.Unidad,
                e.StockActual,
                e.NivelSeguridad,
                e.NivelMaximo,
                e.NivelMinimo,
                e.NivelCritico,
                Proveedor = e.ProveedorPref != null ? e.ProveedorPref.RazonSocial : null,
                ProveedorId = e.ProveedorPrefId
            })
            .ToListAsync(ct);

        var dtos = new List<ExistenciaListItemDto>(items.Count);
        foreach (var item in items)
        {
            dtos.Add(new ExistenciaListItemDto
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Nombre = item.Nombre,
                Unidad = item.Unidad,
                StockActual = item.StockActual,
                NivelSeguridad = item.NivelSeguridad,
                NivelMaximo = item.NivelMaximo,
                NivelMinimo = item.NivelMinimo,
                NivelCritico = item.NivelCritico,
                NivelEstado = CalcularEstado(item.StockActual, item.NivelCritico, item.NivelMinimo, item.NivelSeguridad, item.NivelMaximo),
                Proveedor = item.Proveedor,
                ProveedorId = item.ProveedorId
            });
        }

        return new PagedResult<ExistenciaListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<ExistenciaListItemDto?> ObtenerAsync(long id, CancellationToken ct = default)
    {
        var item = await _db.Existencia
            .AsNoTracking()
            .Include(e => e.ProveedorPref)
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Codigo,
                e.Nombre,
                e.Unidad,
                e.StockActual,
                e.NivelSeguridad,
                e.NivelMaximo,
                e.NivelMinimo,
                e.NivelCritico,
                Proveedor = e.ProveedorPref != null ? e.ProveedorPref.RazonSocial : null,
                ProveedorId = e.ProveedorPrefId
            })
            .FirstOrDefaultAsync(ct);

        if (item is null) return null;

        return new ExistenciaListItemDto
        {
            Id = item.Id,
            Codigo = item.Codigo,
            Nombre = item.Nombre,
            Unidad = item.Unidad,
            StockActual = item.StockActual,
            NivelSeguridad = item.NivelSeguridad,
            NivelMaximo = item.NivelMaximo,
            NivelMinimo = item.NivelMinimo,
            NivelCritico = item.NivelCritico,
            NivelEstado = CalcularEstado(item.StockActual, item.NivelCritico, item.NivelMinimo, item.NivelSeguridad, item.NivelMaximo),
            Proveedor = item.Proveedor,
            ProveedorId = item.ProveedorId
        };
    }

    private static IQueryable<Existencia> ApplySort(IQueryable<Existencia> query, string? sortBy, bool sortDesc)
    {
        sortBy = sortBy?.Trim().ToLowerInvariant();

        return sortBy switch
        {
            "codigo" => sortDesc ? query.OrderByDescending(e => e.Codigo) : query.OrderBy(e => e.Codigo),
            "unidad" => sortDesc ? query.OrderByDescending(e => e.Unidad) : query.OrderBy(e => e.Unidad),
            "stock" => sortDesc ? query.OrderByDescending(e => e.StockActual) : query.OrderBy(e => e.StockActual),
            "nivel" => sortDesc ? query.OrderByDescending(e => e.NivelSeguridad) : query.OrderBy(e => e.NivelSeguridad),
            "maximo" => sortDesc ? query.OrderByDescending(e => e.NivelMaximo) : query.OrderBy(e => e.NivelMaximo),
            "proveedor" => sortDesc ? query.OrderByDescending(e => e.ProveedorPref != null ? e.ProveedorPref.RazonSocial : "") : query.OrderBy(e => e.ProveedorPref != null ? e.ProveedorPref.RazonSocial : ""),
            _ => sortDesc ? query.OrderByDescending(e => e.Nombre) : query.OrderBy(e => e.Nombre)
        };
    }

    private static ExistenciaNivelEstado CalcularEstado(int stock, int nivelCritico, int nivelMinimo, int nivelSeguridad, int nivelMaximo)
    {
        var tieneParametros = nivelCritico > 0 || nivelMinimo > 0 || nivelSeguridad > 0 || nivelMaximo > 0;
        if (!tieneParametros)
        {
            return stock <= 0 ? ExistenciaNivelEstado.Critico : ExistenciaNivelEstado.Normal;
        }

        if (nivelCritico > 0 && stock <= nivelCritico)
        {
            return ExistenciaNivelEstado.Critico;
        }

        if (nivelMinimo > 0 && stock <= nivelMinimo)
        {
            return ExistenciaNivelEstado.Bajo;
        }

        if (nivelSeguridad > 0 && stock <= nivelSeguridad)
        {
            return ExistenciaNivelEstado.Seguro;
        }

        if (nivelMaximo > 0 && stock <= nivelMaximo)
        {
            return ExistenciaNivelEstado.Normal;
        }

        return ExistenciaNivelEstado.Excedido;
    }
}
