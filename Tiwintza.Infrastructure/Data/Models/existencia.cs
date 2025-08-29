using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("codigo", Name = "existencia_codigo_key", IsUnique = true)]
[Index("nivel_critico", "nivel_minimo", "nivel_seguridad", "nivel_maximo", "stock_actual", Name = "idx_existencia_alertas")]
[Index("nombre", Name = "idx_existencia_nombre")]
public partial class existencia
{
    [Key]
    public long id { get; set; }

    [StringLength(50)]
    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    [StringLength(20)]
    public string unidad { get; set; } = null!;

    public int nivel_maximo { get; set; }

    public int nivel_seguridad { get; set; }

    public int nivel_minimo { get; set; }

    public int nivel_critico { get; set; }

    public int stock_actual { get; set; }

    public long? proveedor_pref_id { get; set; }

    [InverseProperty("existencia")]
    public virtual ICollection<detalle_compra> detalle_compra { get; set; } = new List<detalle_compra>();

    [InverseProperty("existencia")]
    public virtual ICollection<existencia_area_stock> existencia_area_stock { get; set; } = new List<existencia_area_stock>();

    [ForeignKey("proveedor_pref_id")]
    [InverseProperty("existencia")]
    public virtual proveedor? proveedor_pref { get; set; }

    [InverseProperty("existencia")]
    public virtual ICollection<salida> salida { get; set; } = new List<salida>();
}
