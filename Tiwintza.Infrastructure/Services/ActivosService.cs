// ActivosService.cs

using Microsoft.EntityFrameworkCore;

using Tiwintza.Infrastructure.Common;

using Tiwintza.Infrastructure.Data;

using Tiwintza.Infrastructure.Data.Models;

using Tiwintza.Infrastructure.Dtos;



namespace Tiwintza.Infrastructure.Services;



public sealed class ActivosService : IActivosService

{

    private readonly IDbContextFactory<AppDbContext> _dbFactory;



    public ActivosService(IDbContextFactory<AppDbContext> dbFactory)

    {

        _dbFactory = dbFactory;

    }



    public async Task<(IReadOnlyList<IdNombreDto> areas,

                       IReadOnlyList<IdNombreDto> estados,

                       IReadOnlyList<IdNombreDto> tipos)> CatalogosAsync(CancellationToken ct = default)

    {

        await using var db = await _dbFactory.CreateDbContextAsync(ct);



        var areas = await db.Area

            .OrderBy(a => a.Nombre)

            .Select(a => new IdNombreDto(a.Id, a.Nombre))

            .ToListAsync(ct);



        var estados = await db.Estado

            .OrderBy(e => e.Nombre)

            .Select(e => new IdNombreDto(e.Id, e.Nombre))

            .ToListAsync(ct);



        var tipos = await db.TipoBien

            .OrderBy(t => t.Nombre)

            .Select(t => new IdNombreDto(t.Id, t.Nombre))

            .ToListAsync(ct);



        return (areas, estados, tipos);

    }



    public async Task<PagedResult<ActivoListItemDto>> BuscarAsync(ActivoFiltro f, CancellationToken ct = default)

    {

        await using var db = await _dbFactory.CreateDbContextAsync(ct);



        IQueryable<Activo> baseQ = db.Activo.AsNoTracking();



        // Texto: Código / Nombre

        if (!string.IsNullOrWhiteSpace(f.Texto))

        {

            var t = f.Texto.Trim();

            baseQ = baseQ.Where(a =>

                EF.Functions.ILike(a.CodigoInventario, $"%{t}%") ||

                EF.Functions.ILike(a.Nombre, $"%{t}%"));

        }



        if (f.AreaId is not null) baseQ = baseQ.Where(a => a.AreaId == f.AreaId);

        if (f.EstadoId is not null) baseQ = baseQ.Where(a => a.EstadoId == f.EstadoId);

        if (f.TipoId is not null) baseQ = baseQ.Where(a => a.TipoId == f.TipoId);



        var q = baseQ;



        q = (f.SortBy ?? string.Empty).ToLower() switch

        {

            "nombre" => f.SortDesc ? q.OrderByDescending(a => a.Nombre) : q.OrderBy(a => a.Nombre),

            "tipo" => f.SortDesc ? q.OrderByDescending(a => a.Tipo.Nombre) : q.OrderBy(a => a.Tipo.Nombre),

            "estado" => f.SortDesc ? q.OrderByDescending(a => a.Estado.Nombre) : q.OrderBy(a => a.Estado.Nombre),

            "area" => f.SortDesc ? q.OrderByDescending(a => a.Area.Nombre) : q.OrderBy(a => a.Area.Nombre),

            "valor" => f.SortDesc ? q.OrderByDescending(a => a.ValorUnitario) : q.OrderBy(a => a.ValorUnitario),

            "compra" => f.SortDesc ? q.OrderByDescending(a => a.FechaCompra) : q.OrderBy(a => a.FechaCompra),

            _ => f.SortDesc ? q.OrderByDescending(a => a.CodigoInventario) : q.OrderBy(a => a.CodigoInventario)

        };



        var total = await q.CountAsync(ct);



        var items = await q.Skip((f.Page - 1) * f.PageSize)

                           .Take(f.PageSize)

                           .Select(a => new ActivoListItemDto

                           {

                               Id = a.Id,

                               Codigo = a.CodigoInventario,

                               Nombre = a.Nombre,

                               Tipo = a.Tipo.Nombre,

                               Estado = a.Estado.Nombre,

                               EnBaja = a.BajaActivo.Any(),

                               Area = a.Area.Nombre,

                               Valor = a.ValorUnitario,

                               FechaCompra = a.FechaCompra

                           })

                           .ToListAsync(ct);



        return new PagedResult<ActivoListItemDto>(items, total, f.Page, f.PageSize);

    }



    public async Task<IReadOnlyList<ActivoExcelDto>> ExportarMaestroAsync(ActivoFiltro f, CancellationToken ct = default)

    {

        await using var db = await _dbFactory.CreateDbContextAsync(ct);



        IQueryable<Activo> q = db.Activo

            .AsNoTracking()

            .Include(a => a.Area)

            .Include(a => a.Estado)

            .Include(a => a.Tipo)

            .Include(a => a.Proveedor);



        if (!string.IsNullOrWhiteSpace(f.Texto))

        {

            var t = f.Texto.Trim();

            q = q.Where(a =>

                EF.Functions.ILike(a.CodigoInventario, $"%{t}%") ||

                EF.Functions.ILike(a.Nombre, $"%{t}%"));

        }

        if (f.AreaId is not null) q = q.Where(a => a.AreaId == f.AreaId);

        if (f.EstadoId is not null) q = q.Where(a => a.EstadoId == f.EstadoId);

        if (f.TipoId is not null) q = q.Where(a => a.TipoId == f.TipoId);



        q = (f.SortBy ?? string.Empty).ToLower() switch

        {

            "nombre" => f.SortDesc ? q.OrderByDescending(a => a.Nombre) : q.OrderBy(a => a.Nombre),

            "tipo" => f.SortDesc ? q.OrderByDescending(a => a.Tipo.Nombre) : q.OrderBy(a => a.Tipo.Nombre),

            "estado" => f.SortDesc ? q.OrderByDescending(a => a.Estado.Nombre) : q.OrderBy(a => a.Estado.Nombre),

            "area" => f.SortDesc ? q.OrderByDescending(a => a.Area.Nombre) : q.OrderBy(a => a.Area.Nombre),

            "valor" => f.SortDesc ? q.OrderByDescending(a => a.ValorUnitario) : q.OrderBy(a => a.ValorUnitario),

            "compra" => f.SortDesc ? q.OrderByDescending(a => a.FechaCompra) : q.OrderBy(a => a.FechaCompra),

            _ => f.SortDesc ? q.OrderByDescending(a => a.CodigoInventario) : q.OrderBy(a => a.CodigoInventario)

        };



        var items = await q

            .Select(a => new ActivoExcelDto

            {

                Codigo = a.CodigoInventario,

                Nombre = a.Nombre,

                Descripcion = a.Descripcion,

                Marca = a.Marca,

                Modelo = a.Modelo,

                Serie = a.Serie,

                Material = a.Material,

                Tipo = a.Tipo.Nombre,

                Estado = a.Estado.Nombre,

                EnBaja = a.BajaActivo.Any(),

                Area = a.Area.Nombre,

                ValorUnitario = a.ValorUnitario,

                FechaCompra = a.FechaCompra,

                Proveedor = a.Proveedor != null ? a.Proveedor.RazonSocial : null,

                VidaUtilMeses = a.VidaUtilMeses,

                DepreciacionMensual = a.DepreciacionMensual,

                DocumentoAutorizacion = a.DocumentoAutorizacion,

                GarantiaMeses = a.GarantiaMeses,

                Observaciones = a.Observaciones

            })

            .ToListAsync(ct);



        return items;

    }



    public async Task<(int total, int operativos, int enBaja)> ResumenAsync(ActivoFiltro f, CancellationToken ct = default)

    {

        await using var db = await _dbFactory.CreateDbContextAsync(ct);



        IQueryable<Activo> baseQ = db.Activo.AsNoTracking();



        if (!string.IsNullOrWhiteSpace(f.Texto))

        {

            var t = f.Texto.Trim();

            baseQ = baseQ.Where(a =>

                EF.Functions.ILike(a.CodigoInventario, $"%{t}%") ||

                EF.Functions.ILike(a.Nombre, $"%{t}%"));

        }

        if (f.AreaId is not null) baseQ = baseQ.Where(a => a.AreaId == f.AreaId);

        if (f.EstadoId is not null) baseQ = baseQ.Where(a => a.EstadoId == f.EstadoId);

        if (f.TipoId is not null) baseQ = baseQ.Where(a => a.TipoId == f.TipoId);



        var total = await baseQ.CountAsync(ct);

        var enBaja = await baseQ.Where(a => a.BajaActivo.Any()).CountAsync(ct);

        var operativos = total - enBaja;

        return (total, operativos, enBaja);

    }

}





