using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("auditoria")]
public partial class Auditoria
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    [Column("usuario")]
    public string Usuario { get; set; } = null!;

    [Column("accion")]
    public string Accion { get; set; } = null!;

    [Column("entidad")]
    public string Entidad { get; set; } = null!;

    [Column("id_entidad")]
    public long? IdEntidad { get; set; }

    [Column("detalle", TypeName = "jsonb")]
    public string? Detalle { get; set; }

    [Column("sede_id")]
    public Guid? SedeId { get; set; }

    [Column("transaction_id")]
    public string? TransactionId { get; set; }

    [Column("accion_usuario")]
    public string? AccionUsuario { get; set; }
}
