using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("rol")]
[Index("Nombre", Name = "rol_nombre_key", IsUnique = true)]
public partial class Rol
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("nombre")]
    [StringLength(40)]
    public string Nombre { get; set; } = null!;

    [Column("descripcion")]
    [StringLength(200)]
    public string? Descripcion { get; set; }

    [ForeignKey("RolId")]
    [InverseProperty("Rol")]
    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}
