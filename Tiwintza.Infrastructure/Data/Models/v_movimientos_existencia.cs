using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Keyless]
public partial class v_movimientos_existencia
{
    public DateOnly? fecha { get; set; }

    public long? existencia_id { get; set; }

    public string? tipo { get; set; }

    public int? cantidad { get; set; }

    public long? ref_id { get; set; }

    public long? area_id { get; set; }

    public DateTime? creado_en { get; set; }
}
