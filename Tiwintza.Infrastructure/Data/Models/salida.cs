using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("salida")]
[Index("AreaId", Name = "idx_salida_area")]
[Index("ExistenciaId", Name = "idx_salida_exi")]
public partial class Salida
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Column("existencia_id")]
    public long ExistenciaId { get; set; }

    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("area_id")]
    public long AreaId { get; set; }

    [Column("responsable")]
    public string? Responsable { get; set; }

    [Column("observacion")]
    public string? Observacion { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("AreaId")]
    [InverseProperty("Salida")]
    public virtual Area Area { get; set; } = null!;

    [ForeignKey("ExistenciaId")]
    [InverseProperty("Salida")]
    public virtual Existencia Existencia { get; set; } = null!;
}
