using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("proveedor_id", "num_factura", Name = "uq_compra_factura", IsUnique = true)]
public partial class compra
{
    [Key]
    public long id { get; set; }

    public DateOnly fecha { get; set; }

    public long proveedor_id { get; set; }

    [StringLength(40)]
    public string? num_factura { get; set; }

    [Precision(14, 2)]
    public decimal? total { get; set; }

    public long? area_id_destino { get; set; }

    public DateTime creado_en { get; set; }

    [InverseProperty("compra")]
    public virtual ICollection<activo> activo { get; set; } = new List<activo>();

    [ForeignKey("area_id_destino")]
    [InverseProperty("compra")]
    public virtual area? area_id_destinoNavigation { get; set; }

    [InverseProperty("compra")]
    public virtual ICollection<detalle_compra> detalle_compra { get; set; } = new List<detalle_compra>();

    [ForeignKey("proveedor_id")]
    [InverseProperty("compra")]
    public virtual proveedor proveedor { get; set; } = null!;
}
