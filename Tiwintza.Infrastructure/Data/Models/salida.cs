using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("area_id", Name = "idx_salida_area")]
[Index("existencia_id", Name = "idx_salida_exi")]
public partial class salida
{
    [Key]
    public long id { get; set; }

    public DateOnly fecha { get; set; }

    public long existencia_id { get; set; }

    public int cantidad { get; set; }

    public long area_id { get; set; }

    public string? responsable { get; set; }

    public string? observacion { get; set; }

    public DateTime creado_en { get; set; }

    [ForeignKey("area_id")]
    [InverseProperty("salida")]
    public virtual area area { get; set; } = null!;

    [ForeignKey("existencia_id")]
    [InverseProperty("salida")]
    public virtual existencia existencia { get; set; } = null!;
}
