using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("login_auditoria")]
[Index("UsuarioId", "FechaHora", Name = "idx_login_auditoria_user_fecha", IsDescending = new[] { false, true })]
public partial class LoginAuditoria
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("usuario_id")]
    public Guid? UsuarioId { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    [Column("exito")]
    public bool Exito { get; set; }

    [Column("detalle")]
    public string? Detalle { get; set; }

    [ForeignKey("UsuarioId")]
    [InverseProperty("LoginAuditoria")]
    public virtual Usuario? Usuario { get; set; }
}
