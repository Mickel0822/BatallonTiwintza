using System;
﻿using System.Collections.Generic;
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
    private const string AreaProcesoBajaNombre = "Bodega en proceso de baja";
    private const string AreaBajaNombre = "Bodega de Baja";
    private const string EstadoMaloNombre = "Malo";
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

    public async Task<IReadOnlyList<ActivoMovimientoDto>> MovimientosAsync(long activoId)
    {
        return await _db.TrasladoActivo.AsNoTracking()
            .Where(t => t.ActivoId == activoId)
            .OrderByDescending(t => t.Fecha)
            .Select(t => new ActivoMovimientoDto
            {
                Fecha = t.Fecha,
                AreaOrigen = t.AreaOrigen.Nombre,
                AreaDestino = t.AreaDestino.Nombre,
                Observacion = t.Observacion,
                Usuario = t.Usuario
            })
            .ToListAsync();
    }

    public async Task<ProveedorDto> CrearProveedorRapidoAsync(ProveedorCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ruc))
        {
            throw new ArgumentException("El RUC es obligatorio", nameof(dto));
        }
        if (string.IsNullOrWhiteSpace(dto.RazonSocial))
        {
            throw new ArgumentException("La razon social es obligatoria", nameof(dto));
        }
        var proveedor = new Proveedor
        {
            Ruc = dto.Ruc.Trim(),
            RazonSocial = dto.RazonSocial.Trim(),
            Contacto = string.IsNullOrWhiteSpace(dto.Contacto) ? null : dto.Contacto.Trim(),
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim()
        };
        _db.Proveedor.Add(proveedor);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.ConstraintName == "proveedor_ruc_key")
        {
            throw new DuplicateCodeException("El RUC del proveedor ya existe.");
        }
        return new ProveedorDto
        {
            Id = proveedor.Id,
            RazonSocial = proveedor.RazonSocial
        };
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
            DocumentoAutorizacion = e.DocumentoAutorizacion,
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
            DocumentoAutorizacion = d.DocumentoAutorizacion,
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
        e.DocumentoAutorizacion = d.DocumentoAutorizacion;
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

    public async Task TrasladarAsync(long activoId, long areaDestinoId, DateOnly fecha, string? observacion = null, string? usuario = null)
    {
        var activo = await _db.Activo.FirstAsync(x => x.Id == activoId);
        var areaOrigenId = activo.AreaId;
        if (areaOrigenId == areaDestinoId)
        {
            return;
        }

        var areaDestino = await _db.Area.FindAsync(areaDestinoId);
        if (areaDestino is null)
        {
            throw new InvalidOperationException($"El area destino con id {areaDestinoId} no existe.");
        }

        if (NombreCoincide(areaDestino.Nombre, AreaProcesoBajaNombre))
        {
            await AsignarEstadoAsync(activo, EstadoMaloNombre);
        }

        var movimiento = new TrasladoActivo
        {
            ActivoId = activoId,
            AreaOrigenId = areaOrigenId,
            AreaDestinoId = areaDestinoId,
            Fecha = fecha,
            Observacion = observacion,
            Usuario = usuario
        };
        _db.TrasladoActivo.Add(movimiento);
        activo.AreaId = areaDestinoId;
        await _db.SaveChangesAsync();
    }

    public async Task DarBajaAsync(long activoId, string codigoInformeTecnico, DateOnly fechaBaja, string responsable, string? observaciones = null)
    {
        var yaBaja = await _db.BajaActivo.AnyAsync(b => b.ActivoId == activoId);
        if (yaBaja) return;

        var activo = await _db.Activo.FirstAsync(x => x.Id == activoId);
        var baja = new BajaActivo
        {
            ActivoId = activoId,
            CodigoInformeTecnico = codigoInformeTecnico,
            FechaBaja = fechaBaja,
            Responsable = responsable,
            Observaciones = observaciones
        };
        _db.BajaActivo.Add(baja);

        var areaDestinoId = await BuscarAreaIdPorNombreAsync(AreaBajaNombre);
        if (areaDestinoId is long areaDestino && areaDestino != activo.AreaId)
        {
            var areaOrigenId = activo.AreaId;
            var traslado = new TrasladoActivo
            {
                ActivoId = activoId,
                AreaOrigenId = areaOrigenId,
                AreaDestinoId = areaDestino,
                Fecha = fechaBaja,
                Observacion = observaciones,
                Usuario = responsable
            };
            _db.TrasladoActivo.Add(traslado);
            activo.AreaId = areaDestino;
        }

        await AsignarEstadoAsync(activo, EstadoMaloNombre);
        await _db.SaveChangesAsync();
    }

    private static bool NombreCoincide(string? actual, string esperado) =>
        !string.IsNullOrWhiteSpace(actual) &&
        string.Equals(actual.Trim(), esperado, StringComparison.OrdinalIgnoreCase);

    private Task<long?> BuscarAreaIdPorNombreAsync(string nombre)
    {
        var target = nombre.Trim().ToLowerInvariant();
        return _db.Area
            .Where(a => a.Nombre.Trim().ToLower() == target)
            .Select(a => (long?)a.Id)
            .FirstOrDefaultAsync();
    }

    private Task<long?> BuscarEstadoIdPorNombreAsync(string nombre)
    {
        var target = nombre.Trim().ToLowerInvariant();
        return _db.Estado
            .Where(e => e.Nombre.Trim().ToLower() == target)
            .Select(e => (long?)e.Id)
            .FirstOrDefaultAsync();
    }

    private async Task AsignarEstadoAsync(Activo activo, string estadoNombre)
    {
        var estadoId = await BuscarEstadoIdPorNombreAsync(estadoNombre);
        if (estadoId is long id && activo.EstadoId != id)
        {
            activo.EstadoId = id;
        }
    }

}
