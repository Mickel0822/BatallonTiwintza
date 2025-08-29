using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("nombre", Name = "estado_nombre_key", IsUnique = true)]
public partial class estado
{
    [Key]
    public long id { get; set; }

    public string nombre { get; set; } = null!;

    public bool es_baja { get; set; }

    public bool es_operativo { get; set; }

    [InverseProperty("estado")]
    public virtual ICollection<activo> activo { get; set; } = new List<activo>();
}
