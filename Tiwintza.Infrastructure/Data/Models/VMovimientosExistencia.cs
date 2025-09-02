using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Keyless]
public partial class VMovimientosExistencia
{
    [Column("fecha")]
    public DateOnly? Fecha { get; set; }

    [Column("existencia_id")]
    public long? ExistenciaId { get; set; }

    [Column("tipo")]
    public string? Tipo { get; set; }

    [Column("cantidad")]
    public int? Cantidad { get; set; }

    [Column("ref_id")]
    public long? RefId { get; set; }

    [Column("area_id")]
    public long? AreaId { get; set; }

    [Column("creado_en")]
    public DateTime? CreadoEn { get; set; }
}
