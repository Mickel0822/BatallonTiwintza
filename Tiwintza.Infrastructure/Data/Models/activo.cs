using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("activo")]
[Index("CodigoInventario", Name = "activo_codigo_inventario_key", IsUnique = true)]
[Index("AreaId", Name = "idx_activo_area")]
[Index("EstadoId", Name = "idx_activo_estado")]
[Index("TipoId", Name = "idx_activo_tipo")]
public partial class Activo
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("codigo_inventario")]
    [StringLength(50)]
    public string CodigoInventario { get; set; } = null!;

    [Column("nombre")]
    public string Nombre { get; set; } = null!;

    [Column("tipo_id")]
    public long TipoId { get; set; }

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("marca")]
    public string? Marca { get; set; }

    [Column("modelo")]
    public string? Modelo { get; set; }

    [Column("serie")]
    public string? Serie { get; set; }

    [Column("material")]
    public string? Material { get; set; }

    [Column("estado_id")]
    public long EstadoId { get; set; }

    [Column("area_id")]
    public long AreaId { get; set; }

    [Column("valor_unitario")]
    [Precision(12, 2)]
    public decimal ValorUnitario { get; set; }

    [Column("fecha_compra")]
    public DateOnly? FechaCompra { get; set; }

    [Column("proveedor_id")]
    public long? ProveedorId { get; set; }

    [Column("compra_id")]
    public long? CompraId { get; set; }

    [Column("vida_util_meses")]
    public int? VidaUtilMeses { get; set; }

    [Column("depreciacion_mensual")]
    [Precision(12, 2)]
    public decimal? DepreciacionMensual { get; set; }
    [Column("documento_autorizacion")]
    public bool DocumentoAutorizacion { get; set; }

    [Column("garantia_meses")]
    public int? GarantiaMeses { get; set; }

    [Column("foto_url")]
    public string? FotoUrl { get; set; }

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("AreaId")]
    [InverseProperty("Activo")]
    public virtual Area Area { get; set; } = null!;

    [InverseProperty("Activo")]
    public virtual ICollection<BajaActivo> BajaActivo { get; set; } = new List<BajaActivo>();

    [ForeignKey("CompraId")]
    [InverseProperty("Activo")]
    public virtual Compra? Compra { get; set; }

    [ForeignKey("EstadoId")]
    [InverseProperty("Activo")]
    public virtual Estado Estado { get; set; } = null!;

    [ForeignKey("ProveedorId")]
    [InverseProperty("Activo")]
    public virtual Proveedor? Proveedor { get; set; }

    [ForeignKey("TipoId")]
    [InverseProperty("Activo")]
    public virtual TipoBien Tipo { get; set; } = null!;

    [InverseProperty("Activo")]
    public virtual ICollection<TrasladoActivo> TrasladoActivo { get; set; } = new List<TrasladoActivo>();
}
