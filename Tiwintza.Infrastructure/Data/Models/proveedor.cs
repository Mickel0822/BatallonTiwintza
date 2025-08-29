using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("ruc", Name = "proveedor_ruc_key", IsUnique = true)]
public partial class proveedor
{
    [Key]
    public long id { get; set; }

    [StringLength(13)]
    public string ruc { get; set; } = null!;

    public string razon_social { get; set; } = null!;

    public string? contacto { get; set; }

    public string? telefono { get; set; }

    public string? email { get; set; }

    [InverseProperty("proveedor")]
    public virtual ICollection<activo> activo { get; set; } = new List<activo>();

    [InverseProperty("proveedor")]
    public virtual ICollection<compra> compra { get; set; } = new List<compra>();

    [InverseProperty("proveedor_pref")]
    public virtual ICollection<existencia> existencia { get; set; } = new List<existencia>();
}
