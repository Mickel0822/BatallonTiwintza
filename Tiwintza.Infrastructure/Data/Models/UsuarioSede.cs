using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tiwintza.Infrastructure.Data.Models;

[Table("usuario_sede")]
public partial class UsuarioSede
{
    [Column("usuario_id")]
    public Guid UsuarioId { get; set; }

    [Column("sede_id")]
    public Guid SedeId { get; set; }

    [ForeignKey("SedeId")]
    [InverseProperty("UsuarioSede")]
    public virtual Sede Sede { get; set; } = null!;

    [ForeignKey("UsuarioId")]
    [InverseProperty("UsuarioSede")]
    public virtual Usuario Usuario { get; set; } = null!;
}
