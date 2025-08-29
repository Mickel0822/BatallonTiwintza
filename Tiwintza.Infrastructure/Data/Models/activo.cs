using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("codigo_inventario", Name = "activo_codigo_inventario_key", IsUnique = true)]
[Index("area_id", Name = "idx_activo_area")]
[Index("estado_id", Name = "idx_activo_estado")]
[Index("tipo_id", Name = "idx_activo_tipo")]
public partial class activo
{
    [Key]
    public long id { get; set; }

    [StringLength(50)]
    public string codigo_inventario { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public long tipo_id { get; set; }

    public string? descripcion { get; set; }

    public string? marca { get; set; }

    public string? modelo { get; set; }

    public string? serie { get; set; }

    public string? material { get; set; }

    public long estado_id { get; set; }

    public long area_id { get; set; }

    [Precision(12, 2)]
    public decimal valor_unitario { get; set; }

    public DateOnly? fecha_compra { get; set; }

    public long? proveedor_id { get; set; }

    public long? compra_id { get; set; }

    public int? vida_util_meses { get; set; }

    [Precision(12, 2)]
    public decimal? depreciacion_mensual { get; set; }

    public int? garantia_meses { get; set; }

    public string? foto_url { get; set; }

    public string? observaciones { get; set; }

    public DateTime creado_en { get; set; }

    [ForeignKey("area_id")]
    [InverseProperty("activo")]
    public virtual area area { get; set; } = null!;

    [InverseProperty("activo")]
    public virtual ICollection<baja_activo> baja_activo { get; set; } = new List<baja_activo>();

    [ForeignKey("compra_id")]
    [InverseProperty("activo")]
    public virtual compra? compra { get; set; }

    [ForeignKey("estado_id")]
    [InverseProperty("activo")]
    public virtual estado estado { get; set; } = null!;

    [ForeignKey("proveedor_id")]
    [InverseProperty("activo")]
    public virtual proveedor? proveedor { get; set; }

    [ForeignKey("tipo_id")]
    [InverseProperty("activo")]
    public virtual tipo_bien tipo { get; set; } = null!;

    [InverseProperty("activo")]
    public virtual ICollection<traslado_activo> traslado_activo { get; set; } = new List<traslado_activo>();
}
