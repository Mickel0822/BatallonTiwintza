using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data.Models;

namespace Tiwintza.Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<activo> activo { get; set; }

    public virtual DbSet<area> area { get; set; }

    public virtual DbSet<auditoria> auditoria { get; set; }

    public virtual DbSet<baja_activo> baja_activo { get; set; }

    public virtual DbSet<compra> compra { get; set; }

    public virtual DbSet<detalle_compra> detalle_compra { get; set; }

    public virtual DbSet<estado> estado { get; set; }

    public virtual DbSet<existencia> existencia { get; set; }

    public virtual DbSet<existencia_area_stock> existencia_area_stock { get; set; }

    public virtual DbSet<proveedor> proveedor { get; set; }

    public virtual DbSet<salida> salida { get; set; }

    public virtual DbSet<tipo_bien> tipo_bien { get; set; }

    public virtual DbSet<traslado_activo> traslado_activo { get; set; }

    public virtual DbSet<v_existencias_niveles> v_existencias_niveles { get; set; }

    public virtual DbSet<v_existencias_por_area> v_existencias_por_area { get; set; }

    public virtual DbSet<v_movimientos_existencia> v_movimientos_existencia { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<activo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("activo_pkey");

            entity.Property(e => e.creado_en).HasDefaultValueSql("now()");

            entity.HasOne(d => d.area).WithMany(p => p.activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("activo_area_id_fkey");

            entity.HasOne(d => d.compra).WithMany(p => p.activo)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activo_compra_id_fkey");

            entity.HasOne(d => d.estado).WithMany(p => p.activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("activo_estado_id_fkey");

            entity.HasOne(d => d.proveedor).WithMany(p => p.activo)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activo_proveedor_id_fkey");

            entity.HasOne(d => d.tipo).WithMany(p => p.activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("activo_tipo_id_fkey");
        });

        modelBuilder.Entity<area>(entity =>
        {
            entity.HasKey(e => e.id).HasName("area_pkey");

            entity.HasOne(d => d.area_padre).WithMany(p => p.Inversearea_padre)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("area_area_padre_id_fkey");
        });

        modelBuilder.Entity<auditoria>(entity =>
        {
            entity.HasKey(e => e.id).HasName("auditoria_pkey");

            entity.Property(e => e.fecha_hora).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<baja_activo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("baja_activo_pkey");

            entity.Property(e => e.creado_en).HasDefaultValueSql("now()");
            entity.Property(e => e.fecha_baja).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.activo).WithMany(p => p.baja_activo)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("baja_activo_activo_id_fkey");
        });

        modelBuilder.Entity<compra>(entity =>
        {
            entity.HasKey(e => e.id).HasName("compra_pkey");

            entity.Property(e => e.creado_en).HasDefaultValueSql("now()");

            entity.HasOne(d => d.area_id_destinoNavigation).WithMany(p => p.compra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("compra_area_id_destino_fkey");

            entity.HasOne(d => d.proveedor).WithMany(p => p.compra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("compra_proveedor_id_fkey");
        });

        modelBuilder.Entity<detalle_compra>(entity =>
        {
            entity.HasKey(e => e.id).HasName("detalle_compra_pkey");

            entity.Property(e => e.creado_en).HasDefaultValueSql("now()");

            entity.HasOne(d => d.compra).WithMany(p => p.detalle_compra).HasConstraintName("detalle_compra_compra_id_fkey");

            entity.HasOne(d => d.existencia).WithMany(p => p.detalle_compra)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_detcomp_exi");
        });

        modelBuilder.Entity<estado>(entity =>
        {
            entity.HasKey(e => e.id).HasName("estado_pkey");

            entity.Property(e => e.es_baja).HasDefaultValue(false);
            entity.Property(e => e.es_operativo).HasDefaultValue(true);
        });

        modelBuilder.Entity<existencia>(entity =>
        {
            entity.HasKey(e => e.id).HasName("existencia_pkey");

            entity.Property(e => e.stock_actual).HasDefaultValue(0);

            entity.HasOne(d => d.proveedor_pref).WithMany(p => p.existencia)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("existencia_proveedor_pref_id_fkey");
        });

        modelBuilder.Entity<existencia_area_stock>(entity =>
        {
            entity.HasKey(e => e.id).HasName("existencia_area_stock_pkey");

            entity.Property(e => e.stock_area).HasDefaultValue(0);

            entity.HasOne(d => d.area).WithMany(p => p.existencia_area_stock).HasConstraintName("existencia_area_stock_area_id_fkey");

            entity.HasOne(d => d.existencia).WithMany(p => p.existencia_area_stock).HasConstraintName("existencia_area_stock_existencia_id_fkey");
        });

        modelBuilder.Entity<proveedor>(entity =>
        {
            entity.HasKey(e => e.id).HasName("proveedor_pkey");
        });

        modelBuilder.Entity<salida>(entity =>
        {
            entity.HasKey(e => e.id).HasName("salida_pkey");

            entity.Property(e => e.creado_en).HasDefaultValueSql("now()");

            entity.HasOne(d => d.area).WithMany(p => p.salida)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("salida_area_id_fkey");

            entity.HasOne(d => d.existencia).WithMany(p => p.salida)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("salida_existencia_id_fkey");
        });

        modelBuilder.Entity<tipo_bien>(entity =>
        {
            entity.HasKey(e => e.id).HasName("tipo_bien_pkey");
        });

        modelBuilder.Entity<traslado_activo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("traslado_activo_pkey");

            entity.Property(e => e.creado_en).HasDefaultValueSql("now()");
            entity.Property(e => e.fecha).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.activo).WithMany(p => p.traslado_activo).HasConstraintName("traslado_activo_activo_id_fkey");

            entity.HasOne(d => d.area_destino).WithMany(p => p.traslado_activoarea_destino)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("traslado_activo_area_destino_id_fkey");

            entity.HasOne(d => d.area_origen).WithMany(p => p.traslado_activoarea_origen)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("traslado_activo_area_origen_id_fkey");
        });

        modelBuilder.Entity<v_existencias_niveles>(entity =>
        {
            entity.ToView("v_existencias_niveles");
        });

        modelBuilder.Entity<v_existencias_por_area>(entity =>
        {
            entity.ToView("v_existencias_por_area");
        });

        modelBuilder.Entity<v_movimientos_existencia>(entity =>
        {
            entity.ToView("v_movimientos_existencia");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
