using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("proveedor")]
[Index("Ruc", Name = "proveedor_ruc_key", IsUnique = true)]
public partial class Proveedor
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("ruc")]
    [StringLength(13)]
    public string Ruc { get; set; } = null!;

    [Column("razon_social")]
    public string RazonSocial { get; set; } = null!;

    [Column("contacto")]
    public string? Contacto { get; set; }

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [InverseProperty("Proveedor")]
    public virtual ICollection<Activo> Activo { get; set; } = new List<Activo>();

    [InverseProperty("Proveedor")]
    public virtual ICollection<Compra> Compra { get; set; } = new List<Compra>();

    [InverseProperty("ProveedorPref")]
    public virtual ICollection<Existencia> Existencia { get; set; } = new List<Existencia>();
}
