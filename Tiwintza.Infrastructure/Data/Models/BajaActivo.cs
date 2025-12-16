using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("baja_activo")]
public partial class BajaActivo : ISedeScoped
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("activo_id")]
    public long ActivoId { get; set; }

    [Column("codigo_informe_tecnico")]
    public string CodigoInformeTecnico { get; set; } = null!;

    [Column("fecha_baja")]
    public DateOnly FechaBaja { get; set; }

    [Column("responsable")]
    public string Responsable { get; set; } = null!;

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    public Guid SedeId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("ActivoId")]
    [InverseProperty("BajaActivo")]
    public virtual Activo Activo { get; set; } = null!;
}
