using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("estado")]
[Index("Nombre", Name = "estado_nombre_key", IsUnique = true)]
public partial class Estado
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("es_baja")]
    public bool EsBaja { get; set; }

    [Column("es_operativo")]
    public bool EsOperativo { get; set; }

    [InverseProperty("Estado")]
    public virtual ICollection<Activo> Activo { get; set; } = new List<Activo>();
}
