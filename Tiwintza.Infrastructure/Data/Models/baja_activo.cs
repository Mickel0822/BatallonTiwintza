using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

public partial class baja_activo
{
    [Key]
    public long id { get; set; }

    public long activo_id { get; set; }

    public string codigo_informe_tecnico { get; set; } = null!;

    public DateOnly fecha_baja { get; set; }

    public string responsable { get; set; } = null!;

    public string? observaciones { get; set; }

    public DateTime creado_en { get; set; }

    [ForeignKey("activo_id")]
    [InverseProperty("baja_activo")]
    public virtual activo activo { get; set; } = null!;
}
