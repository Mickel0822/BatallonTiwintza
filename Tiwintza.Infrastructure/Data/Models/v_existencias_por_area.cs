using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Keyless]
public partial class v_existencias_por_area
{
    public long? area_id { get; set; }

    public string? area { get; set; }

    public long? existencia_id { get; set; }

    [StringLength(50)]
    public string? codigo { get; set; }

    public string? producto { get; set; }

    public int? stock_area { get; set; }
}
