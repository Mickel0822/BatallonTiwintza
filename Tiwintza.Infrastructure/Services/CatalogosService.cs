using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using Tiwintza.Infrastructure.Dtos.Catalogos;

namespace Tiwintza.Infrastructure.Services;

public sealed class CatalogosService : ICatalogosService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CatalogosService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<CatalogoItemDto>> ObtenerAreasAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Area.AsNoTracking()
            .OrderBy(a => a.Nombre)
            .Select(a => new CatalogoItemDto { Id = a.Id, Nombre = a.Nombre })
            .ToListAsync(ct);
    }

    public async Task<CatalogoItemDto> CrearAreaAsync(string nombre, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        nombre = NormalizeNombre(nombre);

        if (await db.Area.AnyAsync(a => a.Nombre.ToLower() == nombre.ToLower(), ct))
            throw new DuplicateCodeException("Ya existe un área con ese nombre.");

        var area = new Area { Nombre = nombre };
        db.Area.Add(area);
        await db.SaveChangesAsync(ct);

        return new CatalogoItemDto { Id = area.Id, Nombre = area.Nombre };
    }

    public async Task<CatalogoItemDto> ActualizarAreaAsync(long id, string nombre, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var area = await db.Area.FirstOrDefaultAsync(a => a.Id == id, ct)
                   ?? throw new KeyNotFoundException("Área no encontrada.");

        nombre = NormalizeNombre(nombre);

        if (await db.Area.AnyAsync(a => a.Id != id && a.Nombre.ToLower() == nombre.ToLower(), ct))
            throw new DuplicateCodeException("Ya existe un área con ese nombre.");

        area.Nombre = nombre;
        await db.SaveChangesAsync(ct);

        return new CatalogoItemDto { Id = area.Id, Nombre = area.Nombre };
    }

    public async Task EliminarAreaAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var area = await db.Area.FirstOrDefaultAsync(a => a.Id == id, ct)
                   ?? throw new KeyNotFoundException("Área no encontrada.");

        db.Area.Remove(area);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se puede eliminar el área porque está en uso.", ex);
        }
    }

    public async Task<IReadOnlyList<CatalogoItemDto>> ObtenerTiposAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TipoBien.AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new CatalogoItemDto { Id = t.Id, Nombre = t.Nombre })
            .ToListAsync(ct);
    }

    public async Task<CatalogoItemDto> CrearTipoAsync(string nombre, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        nombre = NormalizeNombre(nombre);

        if (await db.TipoBien.AnyAsync(t => t.Nombre.ToLower() == nombre.ToLower(), ct))
            throw new DuplicateCodeException("Ya existe un tipo con ese nombre.");

        var tipo = new TipoBien { Nombre = nombre };
        db.TipoBien.Add(tipo);
        await db.SaveChangesAsync(ct);

        return new CatalogoItemDto { Id = tipo.Id, Nombre = tipo.Nombre };
    }

    public async Task<CatalogoItemDto> ActualizarTipoAsync(long id, string nombre, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var tipo = await db.TipoBien.FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new KeyNotFoundException("Tipo no encontrado.");

        nombre = NormalizeNombre(nombre);

        if (await db.TipoBien.AnyAsync(t => t.Id != id && t.Nombre.ToLower() == nombre.ToLower(), ct))
            throw new DuplicateCodeException("Ya existe un tipo con ese nombre.");

        tipo.Nombre = nombre;
        await db.SaveChangesAsync(ct);

        return new CatalogoItemDto { Id = tipo.Id, Nombre = tipo.Nombre };
    }

    public async Task EliminarTipoAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var tipo = await db.TipoBien.FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new KeyNotFoundException("Tipo no encontrado.");

        db.TipoBien.Remove(tipo);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se puede eliminar el tipo porque está en uso.", ex);
        }
    }

    public async Task<IReadOnlyList<ProveedorDetalleDto>> ObtenerProveedoresAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Proveedor.AsNoTracking()
            .OrderBy(p => p.RazonSocial)
            .Select(p => new ProveedorDetalleDto
            {
                Id = p.Id,
                Ruc = p.Ruc,
                RazonSocial = p.RazonSocial,
                Contacto = p.Contacto,
                Telefono = p.Telefono,
                Email = p.Email
            })
            .ToListAsync(ct);
    }

    public async Task<ProveedorDetalleDto> CrearProveedorAsync(ProveedorUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var (ruc, razon) = NormalizeProveedor(dto);

        if (await db.Proveedor.AnyAsync(p => p.Ruc == ruc, ct))
            throw new DuplicateCodeException("Ya existe un proveedor con ese RUC.");

        var proveedor = new Proveedor
        {
            Ruc = ruc,
            RazonSocial = razon,
            Contacto = dto.Contacto?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Email = dto.Email?.Trim()
        };

        db.Proveedor.Add(proveedor);
        await db.SaveChangesAsync(ct);

        return MapProveedor(proveedor);
    }

    public async Task<ProveedorDetalleDto> ActualizarProveedorAsync(long id, ProveedorUpsertDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var proveedor = await db.Proveedor.FirstOrDefaultAsync(p => p.Id == id, ct)
                         ?? throw new KeyNotFoundException("Proveedor no encontrado.");

        var (ruc, razon) = NormalizeProveedor(dto);

        if (await db.Proveedor.AnyAsync(p => p.Id != id && p.Ruc == ruc, ct))
            throw new DuplicateCodeException("Ya existe un proveedor con ese RUC.");

        proveedor.Ruc = ruc;
        proveedor.RazonSocial = razon;
        proveedor.Contacto = dto.Contacto?.Trim();
        proveedor.Telefono = dto.Telefono?.Trim();
        proveedor.Email = dto.Email?.Trim();

        await db.SaveChangesAsync(ct);

        return MapProveedor(proveedor);
    }

    public async Task EliminarProveedorAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var proveedor = await db.Proveedor.FirstOrDefaultAsync(p => p.Id == id, ct)
                         ?? throw new KeyNotFoundException("Proveedor no encontrado.");

        db.Proveedor.Remove(proveedor);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se puede eliminar el proveedor porque está en uso.", ex);
        }
    }

    private static string NormalizeNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es requerido", nameof(nombre));
        return nombre.Trim();
    }

    private static (string Ruc, string RazonSocial) NormalizeProveedor(ProveedorUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ruc))
            throw new ArgumentException("El RUC es requerido", nameof(dto.Ruc));
        if (string.IsNullOrWhiteSpace(dto.RazonSocial))
            throw new ArgumentException("La razón social es requerida", nameof(dto.RazonSocial));

        var ruc = dto.Ruc.Trim();
        var razon = dto.RazonSocial.Trim();
        return (ruc, razon);
    }

    private static ProveedorDetalleDto MapProveedor(Proveedor proveedor) => new()
    {
        Id = proveedor.Id,
        Ruc = proveedor.Ruc,
        RazonSocial = proveedor.RazonSocial,
        Contacto = proveedor.Contacto,
        Telefono = proveedor.Telefono,
        Email = proveedor.Email
    };
}
