using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data.Models;

namespace Tiwintza.Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activo> Activo { get; set; }

    public virtual DbSet<Area> Area { get; set; }

    public virtual DbSet<Auditoria> Auditoria { get; set; }

    public virtual DbSet<BajaActivo> BajaActivo { get; set; }

    public virtual DbSet<Compra> Compra { get; set; }

    public virtual DbSet<DetalleCompra> DetalleCompra { get; set; }

    public virtual DbSet<Estado> Estado { get; set; }

    public virtual DbSet<Existencia> Existencia { get; set; }

    public virtual DbSet<ExistenciaAreaStock> ExistenciaAreaStock { get; set; }

    public virtual DbSet<LoginAuditoria> LoginAuditoria { get; set; }

    public virtual DbSet<Proveedor> Proveedor { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<Salida> Salida { get; set; }

    public virtual DbSet<TipoBien> TipoBien { get; set; }

    public virtual DbSet<TrasladoActivo> TrasladoActivo { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<VExistenciasNiveles> VExistenciasNiveles { get; set; }

    public virtual DbSet<VExistenciasPorArea> VExistenciasPorArea { get; set; }

    public virtual DbSet<VMovimientosExistencia> VMovimientosExistencia { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=batallon_tiwintza;Username=appuser;Password=Usuario12345");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Activo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activo_pkey");

            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Area).WithMany(p => p.Activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("activo_area_id_fkey");

            entity.HasOne(d => d.Compra).WithMany(p => p.Activo)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activo_compra_id_fkey");

            entity.HasOne(d => d.Estado).WithMany(p => p.Activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("activo_estado_id_fkey");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.Activo)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activo_proveedor_id_fkey");

            entity.HasOne(d => d.Tipo).WithMany(p => p.Activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("activo_tipo_id_fkey");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("area_pkey");

            entity.HasOne(d => d.AreaPadre).WithMany(p => p.InverseAreaPadre)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("area_area_padre_id_fkey");
        });

        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auditoria_pkey");

            entity.Property(e => e.FechaHora).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<BajaActivo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("baja_activo_pkey");

            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");
            entity.Property(e => e.FechaBaja).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Activo).WithMany(p => p.BajaActivo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("baja_activo_activo_id_fkey");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("compra_pkey");

            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");

            entity.HasOne(d => d.AreaIdDestinoNavigation).WithMany(p => p.Compra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("compra_area_id_destino_fkey");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.Compra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("compra_proveedor_id_fkey");
        });

        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("detalle_compra_pkey");

            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Compra).WithMany(p => p.DetalleCompra).HasConstraintName("detalle_compra_compra_id_fkey");

            entity.HasOne(d => d.Existencia).WithMany(p => p.DetalleCompra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_detcomp_exi");
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("estado_pkey");

            entity.Property(e => e.EsBaja).HasDefaultValue(false);
            entity.Property(e => e.EsOperativo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Existencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("existencia_pkey");

            entity.Property(e => e.StockActual).HasDefaultValue(0);

            entity.HasOne(d => d.ProveedorPref).WithMany(p => p.Existencia)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("existencia_proveedor_pref_id_fkey");
        });

        modelBuilder.Entity<ExistenciaAreaStock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("existencia_area_stock_pkey");

            entity.Property(e => e.StockArea).HasDefaultValue(0);

            entity.HasOne(d => d.Area).WithMany(p => p.ExistenciaAreaStock).HasConstraintName("existencia_area_stock_area_id_fkey");

            entity.HasOne(d => d.Existencia).WithMany(p => p.ExistenciaAreaStock).HasConstraintName("existencia_area_stock_existencia_id_fkey");
        });

        modelBuilder.Entity<LoginAuditoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("login_auditoria_pkey");

            entity.Property(e => e.FechaHora).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Usuario).WithMany(p => p.LoginAuditoria)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("login_auditoria_usuario_id_fkey");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proveedor_pkey");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rol_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<Salida>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("salida_pkey");

            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Area).WithMany(p => p.Salida)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("salida_area_id_fkey");

            entity.HasOne(d => d.Existencia).WithMany(p => p.Salida)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("salida_existencia_id_fkey");
        });

        modelBuilder.Entity<TipoBien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tipo_bien_pkey");
        });

        modelBuilder.Entity<TrasladoActivo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("traslado_activo_pkey");

            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");
            entity.Property(e => e.Fecha).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Activo).WithMany(p => p.TrasladoActivo).HasConstraintName("traslado_activo_activo_id_fkey");

            entity.HasOne(d => d.AreaDestino).WithMany(p => p.TrasladoActivoAreaDestino)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("traslado_activo_area_destino_id_fkey");

            entity.HasOne(d => d.AreaOrigen).WithMany(p => p.TrasladoActivoAreaOrigen)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("traslado_activo_area_origen_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuario_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()");
            entity.Property(e => e.FailedAttempts).HasDefaultValue((short)0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Area).WithMany(p => p.Usuario)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("usuario_area_id_fkey");

            entity.HasMany(d => d.Rol).WithMany(p => p.Usuario)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioRol",
                    r => r.HasOne<Rol>().WithMany()
                        .HasForeignKey("RolId")
                        .HasConstraintName("usuario_rol_rol_id_fkey"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("UsuarioId")
                        .HasConstraintName("usuario_rol_usuario_id_fkey"),
                    j =>
                    {
                        j.HasKey("UsuarioId", "RolId").HasName("usuario_rol_pkey");
                        j.ToTable("usuario_rol");
                        j.IndexerProperty<Guid>("UsuarioId").HasColumnName("usuario_id");
                        j.IndexerProperty<Guid>("RolId").HasColumnName("rol_id");
                    });
        });

        modelBuilder.Entity<VExistenciasNiveles>(entity =>
        {
            entity.ToView("v_existencias_niveles");
        });

        modelBuilder.Entity<VExistenciasPorArea>(entity =>
        {
            entity.ToView("v_existencias_por_area");
        });

        modelBuilder.Entity<VMovimientosExistencia>(entity =>
        {
            entity.ToView("v_movimientos_existencia");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
