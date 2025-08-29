using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

public partial class auditoria
{
    [Key]
    public long id { get; set; }

    public DateTime fecha_hora { get; set; }

    public string usuario { get; set; } = null!;

    public string accion { get; set; } = null!;

    public string entidad { get; set; } = null!;

    public long? id_entidad { get; set; }

    [Column(TypeName = "jsonb")]
    public string? detalle { get; set; }
}
