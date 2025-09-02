using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("usuario")]
[Index("Email", Name = "usuario_email_key", IsUnique = true)]
[Index("Username", Name = "usuario_username_key", IsUnique = true)]
public partial class Usuario
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("nombre_completo")]
    [StringLength(120)]
    public string NombreCompleto { get; set; } = null!;

    [Column("email")]
    [StringLength(120)]
    public string? Email { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("failed_attempts")]
    public short FailedAttempts { get; set; }

    [Column("lockout_end")]
    public DateTime? LockoutEnd { get; set; }

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("area_id")]
    public long? AreaId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("AreaId")]
    [InverseProperty("Usuario")]
    public virtual Area? Area { get; set; }

    [InverseProperty("Usuario")]
    public virtual ICollection<LoginAuditoria> LoginAuditoria { get; set; } = new List<LoginAuditoria>();

    [ForeignKey("UsuarioId")]
    [InverseProperty("Usuario")]
    public virtual ICollection<Rol> Rol { get; set; } = new List<Rol>();
}
