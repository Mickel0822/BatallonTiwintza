using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("sede")]
public partial class Sede
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("clave")]
    [StringLength(30)]
    public string Clave { get; set; } = null!;

    [Column("nombre")]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [InverseProperty("Sede")]
    public virtual ICollection<UsuarioSede> UsuarioSede { get; set; } = new List<UsuarioSede>();
}
