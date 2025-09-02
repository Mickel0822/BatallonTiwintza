using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("tipo_bien")]
[Index("Nombre", Name = "tipo_bien_nombre_key", IsUnique = true)]
public partial class TipoBien
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [InverseProperty("Tipo")]
    public virtual ICollection<Activo> Activo { get; set; } = new List<Activo>();
}
