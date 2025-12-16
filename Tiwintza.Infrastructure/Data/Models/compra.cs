using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("compra")]
public partial class Compra : ISedeScoped
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Column("proveedor_id")]
    public long ProveedorId { get; set; }

    [Column("num_factura")]
    [StringLength(40)]
    public string? NumFactura { get; set; }

    [Column("total")]
    [Precision(14, 2)]
    public decimal? Total { get; set; }

    [Column("area_id_destino")]
    public long? AreaIdDestino { get; set; }


    public Guid SedeId { get; set; }
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [InverseProperty("Compra")]
    public virtual ICollection<Activo> Activo { get; set; } = new List<Activo>();

    [ForeignKey("AreaIdDestino")]
    [InverseProperty("Compra")]
    public virtual Area? AreaIdDestinoNavigation { get; set; }

    [InverseProperty("Compra")]
    public virtual ICollection<DetalleCompra> DetalleCompra { get; set; } = new List<DetalleCompra>();

    [ForeignKey("ProveedorId")]
    [InverseProperty("Compra")]
    public virtual Proveedor Proveedor { get; set; } = null!;
}
