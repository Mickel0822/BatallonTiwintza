using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("existencia_id", "area_id", Name = "uq_exi_area", IsUnique = true)]
public partial class existencia_area_stock
{
    [Key]
    public long id { get; set; }

    public long existencia_id { get; set; }

    public long area_id { get; set; }

    public int stock_area { get; set; }

    [ForeignKey("area_id")]
    [InverseProperty("existencia_area_stock")]
    public virtual area area { get; set; } = null!;

    [ForeignKey("existencia_id")]
    [InverseProperty("existencia_area_stock")]
    public virtual existencia existencia { get; set; } = null!;
}
