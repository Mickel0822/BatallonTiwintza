using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

public partial class traslado_activo
{
    [Key]
    public long id { get; set; }

    public long activo_id { get; set; }

    public long area_origen_id { get; set; }

    public long area_destino_id { get; set; }

    public DateOnly fecha { get; set; }

    public string? observacion { get; set; }

    public string? usuario { get; set; }

    public DateTime creado_en { get; set; }

    [ForeignKey("activo_id")]
    [InverseProperty("traslado_activo")]
    public virtual activo activo { get; set; } = null!;

    [ForeignKey("area_destino_id")]
    [InverseProperty("traslado_activoarea_destino")]
    public virtual area area_destino { get; set; } = null!;

    [ForeignKey("area_origen_id")]
    [InverseProperty("traslado_activoarea_origen")]
    public virtual area area_origen { get; set; } = null!;
}
