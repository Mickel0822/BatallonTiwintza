using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Keyless]
public partial class VExistenciasNiveles
{
    [Column("id")]
    public long? Id { get; set; }

    [Column("codigo")]
    [StringLength(50)]
    public string? Codigo { get; set; }

    [Column("nombre")]
    public string? Nombre { get; set; }

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("unidad")]
    [StringLength(20)]
    public string? Unidad { get; set; }

    [Column("nivel_maximo")]
    public int? NivelMaximo { get; set; }

    [Column("nivel_seguridad")]
    public int? NivelSeguridad { get; set; }

    [Column("nivel_minimo")]
    public int? NivelMinimo { get; set; }

    [Column("nivel_critico")]
    public int? NivelCritico { get; set; }

    [Column("stock_actual")]
    public int? StockActual { get; set; }

    [Column("proveedor_pref_id")]
    public long? ProveedorPrefId { get; set; }

    [Column("nivel_estado")]
    public string? NivelEstado { get; set; }
}
