using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Index("nombre", Name = "tipo_bien_nombre_key", IsUnique = true)]
public partial class tipo_bien
{
    [Key]
    public long id { get; set; }

    public string nombre { get; set; } = null!;

    [InverseProperty("tipo")]
    public virtual ICollection<activo> activo { get; set; } = new List<activo>();
}
