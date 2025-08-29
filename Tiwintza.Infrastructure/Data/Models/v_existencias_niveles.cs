using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Keyless]
public partial class v_existencias_niveles
{
    public long? id { get; set; }

    [StringLength(50)]
    public string? codigo { get; set; }

    public string? nombre { get; set; }

    public string? descripcion { get; set; }

    [StringLength(20)]
    public string? unidad { get; set; }

    public int? nivel_maximo { get; set; }

    public int? nivel_seguridad { get; set; }

    public int? nivel_minimo { get; set; }

    public int? nivel_critico { get; set; }

    public int? stock_actual { get; set; }

    public long? proveedor_pref_id { get; set; }

    public string? nivel_estado { get; set; }
}
