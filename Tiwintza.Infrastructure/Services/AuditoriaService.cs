using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Dtos.Auditoria;

namespace Tiwintza.Infrastructure.Services;

public sealed class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _db;

    public AuditoriaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuditoriaCatalogosDto> ObtenerCatalogosAsync(CancellationToken ct = default)
    {
        var usuarios = await _db.Auditoria.AsNoTracking()
            .Where(a => a.Usuario != null && a.Usuario != "")
            .Select(a => a.Usuario)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);

        var entidades = await _db.Auditoria.AsNoTracking()
            .Where(a => a.Entidad != null && a.Entidad != "")
            .Select(a => a.Entidad)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);

        var acciones = await _db.Auditoria.AsNoTracking()
            .Where(a => a.Accion != null && a.Accion != "")
            .Select(a => a.Accion)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);

        return new AuditoriaCatalogosDto
        {
            Usuarios = usuarios,
            Entidades = entidades,
            Acciones = acciones
        };
    }

    public async Task<PagedResult<AuditoriaListItemDto>> BuscarAsync(AuditoriaFiltroDto filtro, CancellationToken ct = default)
    {
        filtro ??= new AuditoriaFiltroDto();

        var page = filtro.Page <= 0 ? 1 : filtro.Page;
        var pageSize = filtro.PageSize <= 0 ? 20 : Math.Clamp(filtro.PageSize, 10, 200);

        var query = AplicarFiltros(filtro);
        var total = await query.CountAsync(ct);

        var ordenado = AplicarOrdenamiento(query, filtro);

        var registros = await ordenado
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditoriaListItemDto
            {
                Id = a.Id,
                FechaHora = a.FechaHora,
                Usuario = a.Usuario,
                Accion = a.Accion,
                Entidad = a.Entidad,
                EntidadId = a.IdEntidad,
                DetalleJson = a.Detalle
            })
            .ToListAsync(ct);

        foreach (var item in registros)
        {
            item.Resumen = ExtraerResumen(item.DetalleJson);
        }

        return new PagedResult<AuditoriaListItemDto>(registros, total, page, pageSize);
    }

    public async Task<IReadOnlyList<AuditoriaListItemDto>> ExportarAsync(AuditoriaFiltroDto filtro, CancellationToken ct = default)
    {
        filtro ??= new AuditoriaFiltroDto();

        var query = AplicarFiltros(filtro);
        var ordenado = AplicarOrdenamiento(query, filtro);

        var registros = await ordenado
            .Select(a => new AuditoriaListItemDto
            {
                Id = a.Id,
                FechaHora = a.FechaHora,
                Usuario = a.Usuario,
                Accion = a.Accion,
                Entidad = a.Entidad,
                EntidadId = a.IdEntidad,
                DetalleJson = a.Detalle
            })
            .ToListAsync(ct);

        foreach (var item in registros)
        {
            item.Resumen = ExtraerResumen(item.DetalleJson);
        }

        return registros;
    }

    private IQueryable<Data.Models.Auditoria> AplicarFiltros(AuditoriaFiltroDto filtro)
    {
        var query = _db.Auditoria.AsNoTracking();

        if (filtro.FechaInicio is { } fi)
        {
            query = query.Where(a => a.FechaHora >= fi);
        }
        if (filtro.FechaFin is { } ff)
        {
            query = query.Where(a => a.FechaHora <= ff);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
        {
            var usuario = filtro.Usuario.Trim();
            query = query.Where(a => a.Usuario == usuario);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
        {
            var entidad = filtro.Entidad.Trim();
            query = query.Where(a => a.Entidad == entidad);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            var accion = filtro.Accion.Trim();
            query = query.Where(a => a.Accion == accion);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            var like = $"%{texto}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.Usuario, like) ||
                EF.Functions.ILike(a.Entidad, like) ||
                EF.Functions.ILike(a.Accion, like) ||
                (a.Detalle != null && EF.Functions.ILike(a.Detalle, like)));
        }

        return query;
    }

    private static IOrderedQueryable<Data.Models.Auditoria> AplicarOrdenamiento(IQueryable<Data.Models.Auditoria> query, AuditoriaFiltroDto filtro)
    {
        var sortBy = (filtro.SortBy ?? "fecha").Trim().ToLowerInvariant();
        var desc = filtro.SortDesc;

        return sortBy switch
        {
            "usuario" => desc
                ? query.OrderByDescending(a => a.Usuario).ThenByDescending(a => a.FechaHora)
                : query.OrderBy(a => a.Usuario).ThenByDescending(a => a.FechaHora),
            "accion" => desc
                ? query.OrderByDescending(a => a.Accion).ThenByDescending(a => a.FechaHora)
                : query.OrderBy(a => a.Accion).ThenByDescending(a => a.FechaHora),
            "entidad" => desc
                ? query.OrderByDescending(a => a.Entidad).ThenByDescending(a => a.FechaHora)
                : query.OrderBy(a => a.Entidad).ThenByDescending(a => a.FechaHora),
            "id" => desc
                ? query.OrderByDescending(a => a.Id)
                : query.OrderBy(a => a.Id),
            _ => desc
                ? query.OrderByDescending(a => a.FechaHora)
                : query.OrderBy(a => a.FechaHora)
        };
    }

    private static string? ExtraerResumen(string? detalleJson)
    {
        if (string.IsNullOrWhiteSpace(detalleJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(detalleJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("resumen", out var resumen) && resumen.ValueKind == JsonValueKind.String)
                    return resumen.GetString();
                if (doc.RootElement.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String)
                    return summary.GetString();
                if (doc.RootElement.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                    return mensaje.GetString();
                if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString();
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        var clean = detalleJson.Replace('\n', ' ').Replace("\r", string.Empty).Trim();
        return clean.Length > 140 ? clean[..140] + "…" : clean;
    }
}
