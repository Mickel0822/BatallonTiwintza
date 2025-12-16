using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Dtos.Auditoria;

namespace Tiwintza.Infrastructure.Services;

/// <summary>
/// Extension methods for AuditoriaService to support transaction-grouped queries
/// </summary>
public static class AuditoriaServiceExtensions
{
    /// <summary>
    /// Obtiene auditorías agrupadas por transaction_id
    /// </summary>
    public static async Task<PagedResult<AuditoriaGroupedDto>> BuscarAgrupadasAsync(
        this AppDbContext db,
        AuditoriaFiltroDto filtro,
        CancellationToken ct = default)
    {
        var query = db.Auditoria.AsNoTracking();

        // Aplicar filtros
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
            query = query.Where(a => a.Usuario.Contains(filtro.Usuario));

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
            query = query.Where(a => a.Entidad == filtro.Entidad);

        // Obtener todas las auditorías
        var auditorias = await query
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync(ct);

        // Agrupar por transaction_id (o por proximidad temporal si no tiene transaction_id)
        var grupos = auditorias
            .GroupBy(a => a.TransactionId ?? $"single_{a.Id}")
            .Select(g => new AuditoriaGroupedDto
            {
                TransactionId = g.Key,
                FechaHora = g.Min(a => a.FechaHora),
                Usuario = g.First().Usuario,
                AccionUsuario = g.First().AccionUsuario ?? ObtenerAccionPorEntidad(g.First()),
                TotalOperaciones = g.Count(),
                Detalles = g.Select(a => new AuditoriaListItemDto
                {
                    Id = a.Id,
                    FechaHora = a.FechaHora,
                    Usuario = a.Usuario,
                    Accion = a.Accion,
                    Entidad = a.Entidad,
                    EntidadId = a.IdEntidad,
                    DetalleJson = a.Detalle,
                    TransactionId = a.TransactionId,
                    AccionUsuario = a.AccionUsuario,
                    Resumen = GenerarResumen(a)
                }).OrderBy(a => a.FechaHora).ToList()
            })
            .OrderByDescending(g => g.FechaHora)
            .ToList();

        // Paginación
        var total = grupos.Count;
        var paginados = grupos
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToList();

        return new PagedResult<AuditoriaGroupedDto>(
            paginados,
            total,
            filtro.Page,
            filtro.PageSize
        );
    }

    private static string ObtenerAccionPorEntidad(Data.Models.Auditoria aud)
    {
        return aud.Entidad switch
        {
            "compra" => "Registrar Compra",
            "salida" => "Registrar Salida",
            "activo" when aud.Accion == "INSERT" => "Crear Activo",
            "activo" when aud.Accion == "UPDATE" => "Modificar Activo",
            "baja_activo" => "Dar de Baja Activo",
            "traslado_activo" => "Trasladar Activo",
            "existencia" when aud.Accion == "INSERT" => "Crear Existencia",
            "existencia" when aud.Accion == "UPDATE" => "Modificar Existencia",
            "area" when aud.Accion == "INSERT" => "Crear Área",
            "area" when aud.Accion == "UPDATE" => "Modificar Área",
            _ => $"{aud.Accion} {aud.Entidad}"
        };
    }

    private static string GenerarResumen(Data.Models.Auditoria aud)
    {
        var accion = aud.Accion switch
        {
            "INSERT" => "Creado",
            "UPDATE" => "Modificado",
            "DELETE" => "Eliminado",
            _ => aud.Accion
        };

        return $"{accion}: {aud.Entidad} #{aud.IdEntidad}";
    }
}
