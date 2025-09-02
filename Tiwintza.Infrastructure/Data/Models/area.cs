using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("area")]
[Index("Nombre", "AreaPadreId", Name = "uq_area", IsUnique = true)]
public partial class Area
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("area_padre_id")]
    public long? AreaPadreId { get; set; }

    [InverseProperty("Area")]
    public virtual ICollection<Activo> Activo { get; set; } = new List<Activo>();

    [ForeignKey("AreaPadreId")]
    [InverseProperty("InverseAreaPadre")]
    public virtual Area? AreaPadre { get; set; }

    [InverseProperty("AreaIdDestinoNavigation")]
    public virtual ICollection<Compra> Compra { get; set; } = new List<Compra>();

    [InverseProperty("Area")]
    public virtual ICollection<ExistenciaAreaStock> ExistenciaAreaStock { get; set; } = new List<ExistenciaAreaStock>();

    [InverseProperty("AreaPadre")]
    public virtual ICollection<Area> InverseAreaPadre { get; set; } = new List<Area>();

    [InverseProperty("Area")]
    public virtual ICollection<Salida> Salida { get; set; } = new List<Salida>();

    [InverseProperty("AreaDestino")]
    public virtual ICollection<TrasladoActivo> TrasladoActivoAreaDestino { get; set; } = new List<TrasladoActivo>();

    [InverseProperty("AreaOrigen")]
    public virtual ICollection<TrasladoActivo> TrasladoActivoAreaOrigen { get; set; } = new List<TrasladoActivo>();

    [InverseProperty("Area")]
    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}
