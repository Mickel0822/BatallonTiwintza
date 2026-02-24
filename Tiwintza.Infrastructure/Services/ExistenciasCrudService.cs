using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Dtos.Existencias;

namespace Tiwintza.Infrastructure.Services;

public sealed class ExistenciasCrudService : IExistenciasCrudService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ExistenciasCrudService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<ProveedorDto>> ObtenerProveedoresAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Proveedor
            .AsNoTracking()
            .OrderBy(p => p.RazonSocial)
            .Select(p => new ProveedorDto
            {
                Id = p.Id,
                RazonSocial = p.RazonSocial
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExistenciaComboItemDto>> ObtenerProductosAsync(string? texto = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        IQueryable<Existencia> query = db.Existencia.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var term = texto.Trim();
            query = query.Where(e =>
                EF.Functions.ILike(e.Codigo, $"%{term}%") ||
                EF.Functions.ILike(e.Nombre, $"%{term}%"));
        }

        return await query
            .OrderBy(e => e.Nombre)
            .Select(e => new ExistenciaComboItemDto
            {
                Id = e.Id,
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                Unidad = e.Unidad
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IdNombreDto>> ObtenerAreasAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Area
            .AsNoTracking()
            .OrderBy(a => a.Nombre)
            .Select(a => new IdNombreDto(a.Id, a.Nombre))
            .ToListAsync(ct);
    }

    public async Task<ProveedorDto> CrearProveedorRapidoAsync(ProveedorCreateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Ruc))
            throw new ArgumentException("El RUC es obligatorio", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.RazonSocial))
            throw new ArgumentException("La razón social es obligatoria", nameof(dto));

        var proveedor = new Proveedor
        {
            Ruc = dto.Ruc.Trim(),
            RazonSocial = dto.RazonSocial.Trim(),
            Contacto = string.IsNullOrWhiteSpace(dto.Contacto) ? null : dto.Contacto.Trim(),
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim()
        };

        db.Proveedor.Add(proveedor);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.ConstraintName == "proveedor_ruc_key")
        {
            throw new InvalidOperationException("El RUC del proveedor ya existe.", ex);
        }

        return new ProveedorDto
        {
            Id = proveedor.Id,
            RazonSocial = proveedor.RazonSocial
        };
    }

    public async Task<ExistenciaComboItemDto> CrearExistenciaRapidaAsync(ExistenciaQuickCreateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (dto is null) throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Codigo))
            throw new ArgumentException("El código es obligatorio", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ArgumentException("El nombre es obligatorio", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Unidad))
            throw new ArgumentException("La unidad es obligatoria", nameof(dto));

        var codigo = dto.Codigo.Trim();
        var nombre = dto.Nombre.Trim();
        var unidad = dto.Unidad.Trim();

        if (dto.NivelCritico < 0 || dto.NivelMinimo < 0 || dto.NivelSeguridad < 0 || dto.NivelMaximo < 0)
            throw new ArgumentException("Los niveles no pueden ser negativos", nameof(dto));

        if (dto.NivelCritico > dto.NivelMinimo || dto.NivelMinimo > dto.NivelSeguridad || dto.NivelSeguridad > dto.NivelMaximo)
            throw new ArgumentException("Debe respetar el orden crítico ≤ mínimo ≤ seguridad ≤ máximo", nameof(dto));

        var existencia = new Existencia
        {
            Codigo = codigo,
            Nombre = nombre,
            Unidad = unidad,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            NivelMaximo = dto.NivelMaximo,
            NivelSeguridad = dto.NivelSeguridad,
            NivelMinimo = dto.NivelMinimo,
            NivelCritico = dto.NivelCritico,
            StockActual = 0,
            ProveedorPrefId = dto.ProveedorPreferidoId
        };

        db.Existencia.Add(existencia);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.ConstraintName == "existencia_codigo_key")
        {
            throw new InvalidOperationException("El código de existencia ya está registrado.", ex);
        }

        return new ExistenciaComboItemDto
        {
            Id = existencia.Id,
            Codigo = existencia.Codigo,
            Nombre = existencia.Nombre,
            Unidad = existencia.Unidad
        };
    }

    /// <summary>
    /// Registra una COMPRA (ingreso) sin tocar StockActual ni saldos por área.
    /// Los triggers de BD actualizan stock y saldos.
    /// </summary>
    public async Task<long> RegistrarIngresoAsync(ExistenciaIngresoCreateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.ProveedorId <= 0) throw new ArgumentException("Proveedor inválido", nameof(dto));
        if (dto.Detalles is null || dto.Detalles.Count == 0)
            throw new ArgumentException("Debe agregar al menos un producto", nameof(dto));

        // Validaciones de ítems (no modificamos stock)
        foreach (var det in dto.Detalles)
        {
            if (det.ExistenciaId <= 0) throw new ArgumentException("Producto inválido", nameof(dto));
            if (det.Cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero", nameof(dto));
            if (det.CostoUnitario < 0) throw new ArgumentException("El costo unitario no puede ser negativo", nameof(dto));
        }

        // Verificar proveedor
        var proveedorExiste = await db.Proveedor.AnyAsync(p => p.Id == dto.ProveedorId, ct);
        if (!proveedorExiste)
            throw new InvalidOperationException("El proveedor seleccionado no existe.");

        // Verificar que todas las existencias existan
        var existenciaIds = dto.Detalles.Select(d => d.ExistenciaId).Distinct().ToList();
        var totalExistentes = await db.Existencia.CountAsync(e => existenciaIds.Contains(e.Id), ct);
        if (totalExistentes != existenciaIds.Count)
            throw new InvalidOperationException("No se encontró alguna de las existencias seleccionadas.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var compra = new Compra
        {
            Fecha = dto.Fecha,
            ProveedorId = dto.ProveedorId,
            NumFactura = string.IsNullOrWhiteSpace(dto.NumeroFactura) ? null : dto.NumeroFactura.Trim(),
            Total = Math.Round(dto.Detalles.Sum(d => d.Total), 2, MidpointRounding.AwayFromZero),
            CreadoEn = DateTime.UtcNow
        };

        // Solo insertamos detalle_compra; el TRIGGER suma al stock
        foreach (var det in dto.Detalles)
        {
            compra.DetalleCompra.Add(new DetalleCompra
            {
                ExistenciaId = det.ExistenciaId,
                Cantidad = det.Cantidad,
                CostoUnitario = det.CostoUnitario,
                CreadoEn = DateTime.UtcNow
            });
        }

        db.Compra.Add(compra);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return compra.Id;
    }

    /// <summary>
    /// Registra una SALIDA sin tocar StockActual ni ExistenciaAreaStock.
    /// Los triggers de BD descuentan stock y ajustan el saldo del área.
    /// </summary>
    public async Task<long> RegistrarSalidaAsync(ExistenciaSalidaCreateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.ExistenciaId <= 0) throw new ArgumentException("Existencia inválida", nameof(dto));
        if (dto.AreaId <= 0) throw new ArgumentException("Área inválida", nameof(dto));
        if (dto.Cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero", nameof(dto));

        // Validaciones de referencia
        var existencia = await db.Existencia
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == dto.ExistenciaId, ct)
            ?? throw new InvalidOperationException("La existencia seleccionada no existe.");

        var area = await db.Area
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == dto.AreaId, ct)
            ?? throw new InvalidOperationException("El área seleccionada no existe.");

        // Validación explícita de sede
        if (existencia.SedeId != area.SedeId)
        {
            throw new InvalidOperationException(
                "No se puede registrar la salida: la existencia pertenece a una sede diferente al área destino.");
        }

        // Validación de cortesía (mensaje amigable). No modifica stock.
        if (existencia.StockActual < dto.Cantidad)
            throw new InvalidOperationException("No hay stock suficiente para registrar la salida.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Solo insertamos la salida; el TRIGGER descuenta stock y actualiza existencia_area_stock
        var salida = new Salida
        {
            ExistenciaId = dto.ExistenciaId,
            Cantidad = dto.Cantidad,
            AreaId = dto.AreaId,
            Fecha = dto.Fecha,
            Responsable = string.IsNullOrWhiteSpace(dto.Responsable) ? null : dto.Responsable.Trim(),
            Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
            CreadoEn = DateTime.UtcNow
        };

        db.Salida.Add(salida);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return salida.Id;
    }
}
