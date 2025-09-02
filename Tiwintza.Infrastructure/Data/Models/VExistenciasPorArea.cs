using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Keyless]
public partial class VExistenciasPorArea
{
    [Column("area_id")]
    public long? AreaId { get; set; }

    [Column("area")]
    public string? Area { get; set; }

    [Column("existencia_id")]
    public long? ExistenciaId { get; set; }

    [Column("codigo")]
    [StringLength(50)]
    public string? Codigo { get; set; }

    [Column("producto")]
    public string? Producto { get; set; }

    [Column("stock_area")]
    public int? StockArea { get; set; }
}
