using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("traslado_activo")]
public partial class TrasladoActivo : ISedeScoped
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("activo_id")]
    public long ActivoId { get; set; }

    [Column("area_origen_id")]
    public long AreaOrigenId { get; set; }

    [Column("area_destino_id")]
    public long AreaDestinoId { get; set; }

    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Column("observacion")]
    public string? Observacion { get; set; }

    [Column("usuario")]
    public string? Usuario { get; set; }

    public Guid SedeId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("ActivoId")]
    [InverseProperty("TrasladoActivo")]
    public virtual Activo Activo { get; set; } = null!;

    [ForeignKey("AreaDestinoId")]
    [InverseProperty("TrasladoActivoAreaDestino")]
    public virtual Area AreaDestino { get; set; } = null!;

    [ForeignKey("AreaOrigenId")]
    [InverseProperty("TrasladoActivoAreaOrigen")]
    public virtual Area AreaOrigen { get; set; } = null!;
}
