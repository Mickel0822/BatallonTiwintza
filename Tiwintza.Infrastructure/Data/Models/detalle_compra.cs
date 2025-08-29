using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

public partial class detalle_compra
{
    [Key]
    public long id { get; set; }

    public long compra_id { get; set; }

    public long existencia_id { get; set; }

    public int cantidad { get; set; }

    [Precision(12, 2)]
    public decimal? costo_unitario { get; set; }

    public DateTime creado_en { get; set; }

    [ForeignKey("compra_id")]
    [InverseProperty("detalle_compra")]
    public virtual compra compra { get; set; } = null!;

    [ForeignKey("existencia_id")]
    [InverseProperty("detalle_compra")]
    public virtual existencia existencia { get; set; } = null!;
}
