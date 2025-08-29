using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("nombre", "area_padre_id", Name = "uq_area", IsUnique = true)]
public partial class area
{
    [Key]
    public long id { get; set; }

    public string nombre { get; set; } = null!;

    public long? area_padre_id { get; set; }

    [InverseProperty("area_padre")]
    public virtual ICollection<area> Inversearea_padre { get; set; } = new List<area>();

    [InverseProperty("area")]
    public virtual ICollection<activo> activo { get; set; } = new List<activo>();

    [ForeignKey("area_padre_id")]
    [InverseProperty("Inversearea_padre")]
    public virtual area? area_padre { get; set; }

    [InverseProperty("area_id_destinoNavigation")]
    public virtual ICollection<compra> compra { get; set; } = new List<compra>();

    [InverseProperty("area")]
    public virtual ICollection<existencia_area_stock> existencia_area_stock { get; set; } = new List<existencia_area_stock>();

    [InverseProperty("area")]
    public virtual ICollection<salida> salida { get; set; } = new List<salida>();

    [InverseProperty("area_destino")]
    public virtual ICollection<traslado_activo> traslado_activoarea_destino { get; set; } = new List<traslado_activo>();

    [InverseProperty("area_origen")]
    public virtual ICollection<traslado_activo> traslado_activoarea_origen { get; set; } = new List<traslado_activo>();
}
