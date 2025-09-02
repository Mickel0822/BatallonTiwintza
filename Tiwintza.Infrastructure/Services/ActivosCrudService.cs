using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using Tiwintza.Infrastructure.Dtos;

namespace Tiwintza.Infrastructure.Services;

public sealed class ActivosCrudService : IActivosCrudService
{
    private readonly AppDbContext _db;
    public ActivosCrudService(AppDbContext db) => _db = db;

    public async Task<(IEnumerable<IdNombreDto> Areas,
                       IEnumerable<IdNombreDto> Estados,
                       IEnumerable<IdNombreDto> Tipos,
                       IEnumerable<ProveedorDto> Proveedores)> CatalogosFormAsync()
    {
        var areas = await _db.Area.AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new IdNombreDto(x.Id, x.Nombre))          // <-- constructor posicional
            .ToListAsync();

        var estados = await _db.Estado.AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new IdNombreDto(x.Id, x.Nombre))          // <-- constructor posicional
            .ToListAsync();

        var tipos = await _db.TipoBien.AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new IdNombreDto(x.Id, x.Nombre))          // <-- constructor posicional
            .ToListAsync();

        var proveedores = await _db.Proveedor.AsNoTracking()
        .OrderBy(x => x.RazonSocial)
        .Select(x => new ProveedorDto
        {                  // clase con ctor por defecto
            Id = x.Id,
            RazonSocial = x.RazonSocial
        })
        .ToListAsync();

        return (areas, estados, tipos, proveedores);
    }

    public async Task<ActivoFormDto> ObtenerAsync(long id)
    {
        var e = await _db.Activo.AsNoTracking().FirstAsync(x => x.Id == id);
        return new ActivoFormDto
        {
            Id = e.Id,
            CodigoInventario = e.CodigoInventario,
            Nombre = e.Nombre,
            TipoId = e.TipoId,
            Descripcion = e.Descripcion,
            Marca = e.Marca,
            Modelo = e.Modelo,
            Serie = e.Serie,
            Material = e.Material,
            EstadoId = e.EstadoId,
            AreaId = e.AreaId,
            ValorUnitario = e.ValorUnitario,
            FechaCompra = e.FechaCompra,
            ProveedorId = e.ProveedorId,
            VidaUtilMeses = e.VidaUtilMeses,
            DepreciacionMensual = e.DepreciacionMensual,
            GarantiaMeses = e.GarantiaMeses,
            Observaciones = e.Observaciones
        };
    }

    public async Task<long> CrearAsync(ActivoCreateDto d)
    {
        var e = new Activo
        {
            CodigoInventario = d.CodigoInventario,
            Nombre = d.Nombre,
            TipoId = d.TipoId,
            Descripcion = d.Descripcion,
            Marca = d.Marca,
            Modelo = d.Modelo,
            Serie = d.Serie,
            Material = d.Material,
            EstadoId = d.EstadoId,
            AreaId = d.AreaId,
            ValorUnitario = d.ValorUnitario,
            FechaCompra = d.FechaCompra,
            ProveedorId = d.ProveedorId,
            VidaUtilMeses = d.VidaUtilMeses,
            DepreciacionMensual = d.DepreciacionMensual,
            GarantiaMeses = d.GarantiaMeses,
            Observaciones = d.Observaciones
        };

        _db.Activo.Add(e);
        try
        {
            await _db.SaveChangesAsync();
            return e.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.ConstraintName == "activo_codigo_inventario_key")
        {
            throw new DuplicateCodeException("El código ya existe.");
        }
    }

    public async Task ActualizarAsync(long id, ActivoUpdateDto d)
    {
        var e = await _db.Activo.FirstAsync(x => x.Id == id);

        e.CodigoInventario = d.CodigoInventario;
        e.Nombre = d.Nombre;
        e.TipoId = d.TipoId;
        e.Descripcion = d.Descripcion;
        e.Marca = d.Marca;
        e.Modelo = d.Modelo;
        e.Serie = d.Serie;
        e.Material = d.Material;
        e.EstadoId = d.EstadoId;
        e.AreaId = d.AreaId;
        e.ValorUnitario = d.ValorUnitario;
        e.FechaCompra = d.FechaCompra;
        e.ProveedorId = d.ProveedorId;
        e.VidaUtilMeses = d.VidaUtilMeses;
        e.DepreciacionMensual = d.DepreciacionMensual;
        e.GarantiaMeses = d.GarantiaMeses;
        e.Observaciones = d.Observaciones;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.ConstraintName == "activo_codigo_inventario_key")
        {
            throw new DuplicateCodeException("El código ya existe.");
        }
    }
}
