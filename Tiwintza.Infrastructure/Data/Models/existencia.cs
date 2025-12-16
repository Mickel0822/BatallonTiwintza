using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("existencia")]
[Index("NivelCritico", "NivelMinimo", "NivelSeguridad", "NivelMaximo", "StockActual", Name = "idx_existencia_alertas")]
[Index("Nombre", Name = "idx_existencia_nombre")]
public partial class Existencia : ISedeScoped
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("codigo")]
    [StringLength(50)]
    public string Codigo { get; set; } = null!;

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("unidad")]
    [StringLength(20)]
    public string Unidad { get; set; } = null!;

    [Column("nivel_maximo")]
    public int NivelMaximo { get; set; }

    [Column("nivel_seguridad")]
    public int NivelSeguridad { get; set; }

    [Column("nivel_minimo")]
    public int NivelMinimo { get; set; }

    [Column("nivel_critico")]
    public int NivelCritico { get; set; }

    [Column("stock_actual")]
    public int StockActual { get; set; }

    [Column("proveedor_pref_id")]
    public long? ProveedorPrefId { get; set; }

    public Guid SedeId { get; set; }



    [InverseProperty("Existencia")]
    public virtual ICollection<DetalleCompra> DetalleCompra { get; set; } = new List<DetalleCompra>();

    [InverseProperty("Existencia")]
    public virtual ICollection<ExistenciaAreaStock> ExistenciaAreaStock { get; set; } = new List<ExistenciaAreaStock>();

    [ForeignKey("ProveedorPrefId")]
    [InverseProperty("Existencia")]
    public virtual Proveedor? ProveedorPref { get; set; }

    [InverseProperty("Existencia")]
    public virtual ICollection<Salida> Salida { get; set; } = new List<Salida>();
}
