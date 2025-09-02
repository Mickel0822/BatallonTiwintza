using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("detalle_compra")]
public partial class DetalleCompra
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("compra_id")]
    public long CompraId { get; set; }

    [Column("existencia_id")]
    public long ExistenciaId { get; set; }

    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("costo_unitario")]
    [Precision(12, 2)]
    public decimal? CostoUnitario { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("CompraId")]
    [InverseProperty("DetalleCompra")]
    public virtual Compra Compra { get; set; } = null!;

    [ForeignKey("ExistenciaId")]
    [InverseProperty("DetalleCompra")]
    public virtual Existencia Existencia { get; set; } = null!;
}
