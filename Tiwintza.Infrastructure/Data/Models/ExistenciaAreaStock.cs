using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("existencia_area_stock")]
[Index("ExistenciaId", "AreaId", Name = "uq_exi_area", IsUnique = true)]
public partial class ExistenciaAreaStock
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("existencia_id")]
    public long ExistenciaId { get; set; }

    [Column("area_id")]
    public long AreaId { get; set; }

    [Column("stock_area")]
    public int StockArea { get; set; }

    [ForeignKey("AreaId")]
    [InverseProperty("ExistenciaAreaStock")]
    public virtual Area Area { get; set; } = null!;

    [ForeignKey("ExistenciaId")]
    [InverseProperty("ExistenciaAreaStock")]
    public virtual Existencia Existencia { get; set; } = null!;
}
