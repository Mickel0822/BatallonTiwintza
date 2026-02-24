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
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AuditoriaService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AuditoriaCatalogosDto> ObtenerCatalogosAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var usuarios = await db.Auditoria.AsNoTracking()
            .Where(a => a.Usuario != null && a.Usuario != "")
            .Select(a => a.Usuario)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);

        var entidades = await db.Auditoria.AsNoTracking()
            .Where(a => a.Entidad != null && a.Entidad != "")
            .Select(a => a.Entidad)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);

        var acciones = await db.Auditoria.AsNoTracking()
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.Auditoria.AsNoTracking();

        if (filtro.FechaInicio.HasValue)
        {
            var fechaInicioUtc = DateTime.SpecifyKind(filtro.FechaInicio.Value, DateTimeKind.Utc);
            query = query.Where(a => a.FechaHora >= fechaInicioUtc);
        }

        if (filtro.FechaFin.HasValue)
        {
            var fechaFinUtc = DateTime.SpecifyKind(filtro.FechaFin.Value, DateTimeKind.Utc);
            query = query.Where(a => a.FechaHora <= fechaFinUtc);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            query = query.Where(a => a.Usuario == filtro.Usuario);

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
            query = query.Where(a => a.Entidad == filtro.Entidad);

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
            query = query.Where(a => a.Accion == filtro.Accion);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var term = filtro.Texto.Trim();
            if (long.TryParse(term, out var id))
            {
                query = query.Where(a => a.IdEntidad == id);
            }
            else
            {
                query = query.Where(a => a.Detalle.Contains(term));
            }
        }

        var total = await query.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(filtro.SortBy))
        {
            if (filtro.SortBy.ToLower() == "fecha")
                query = filtro.SortDesc ? query.OrderByDescending(a => a.FechaHora) : query.OrderBy(a => a.FechaHora);
            else if (filtro.SortBy.ToLower() == "usuario")
                query = filtro.SortDesc ? query.OrderByDescending(a => a.Usuario) : query.OrderBy(a => a.Usuario);
            else if (filtro.SortBy.ToLower() == "entidad")
                query = filtro.SortDesc ? query.OrderByDescending(a => a.Entidad) : query.OrderBy(a => a.Entidad);
            else if (filtro.SortBy.ToLower() == "accion")
                query = filtro.SortDesc ? query.OrderByDescending(a => a.Accion) : query.OrderBy(a => a.Accion);
            else
                query = query.OrderByDescending(a => a.FechaHora);
        }
        else
        {
            query = query.OrderByDescending(a => a.FechaHora);
        }

        var items = await query
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(a => new AuditoriaListItemDto
            {
                Id = a.Id,
                FechaHora = a.FechaHora,
                Usuario = a.Usuario,
                Accion = a.Accion,
                Entidad = a.Entidad,
                EntidadId = a.IdEntidad,
                DetalleJson = a.Detalle,
                TransactionId = a.TransactionId,
                AccionUsuario = a.AccionUsuario
            })
            .ToListAsync(ct);

        // Generar resumen en memoria
        foreach (var item in items)
        {
            item.Resumen = GenerarResumen(item.DetalleJson);
        }

        return new PagedResult<AuditoriaListItemDto>(
            items,
            total,
            filtro.Page,
            filtro.PageSize
        );
    }

    public async Task<IReadOnlyList<AuditoriaListItemDto>> ExportarAsync(AuditoriaFiltroDto filtro, CancellationToken ct = default)
    {
        // Reutilizar lógica de búsqueda pero sin paginación
        var filtroExport = new AuditoriaFiltroDto
        {
            FechaInicio = filtro.FechaInicio,
            FechaFin = filtro.FechaFin,
            Usuario = filtro.Usuario,
            Entidad = filtro.Entidad,
            Accion = filtro.Accion,
            Texto = filtro.Texto,
            SortBy = filtro.SortBy,
            SortDesc = filtro.SortDesc,
            Page = 1,
            PageSize = int.MaxValue
        };
        var result = await BuscarAsync(filtroExport, ct);
        return result.Items;
    }

    public async Task<PagedResult<AuditoriaGroupedDto>> BuscarAgrupadasAsync(AuditoriaFiltroDto filtro, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.BuscarAgrupadasAsync(filtro, ct);
    }

    private static string? GenerarResumen(string? detalleJson)
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

                // Intento de parseo genérico amigable
                var props = new List<string>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) continue;

                    // Ignorar propiedades de auditoria interna o ids complejos sin nombre claro si hay muchos
                    if (prop.NameEquals("id") && doc.RootElement.EnumerateObject().Count() > 1) continue; 

                    var val = prop.Value.ToString();
                    if (val.Length > 50) val = val[..47] + "...";
                    
                    props.Add($"{prop.Name}: {val}");
                }
                
                if (props.Count > 0) return string.Join(", ", props);
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
