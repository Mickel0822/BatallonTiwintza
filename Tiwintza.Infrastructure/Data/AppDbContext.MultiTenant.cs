using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data.Models;

namespace Tiwintza.Infrastructure.Data;

public partial class AppDbContext
{
    
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureSede(modelBuilder);
        ConfigureUsuarioSede(modelBuilder);

        // Configurar entidades multi-tenant (por sede)
        ConfigureSedeScoped<Area>(modelBuilder, entity =>
        {
            entity.HasIndex(e => new { e.SedeId, e.Nombre, e.AreaPadreId })
                  .HasDatabaseName("uq_area_por_sede")
                  .IsUnique();
        });

        ConfigureSedeScoped<Activo>(modelBuilder, entity =>
        {
            entity.HasIndex(e => new { e.SedeId, e.CodigoInventario })
                  .HasDatabaseName("uq_activo_codigo_por_sede")
                  .IsUnique();
        });

        ConfigureSedeScoped<Existencia>(modelBuilder, entity =>
        {
            entity.HasIndex(e => new { e.SedeId, e.Codigo })
                  .HasDatabaseName("uq_existencia_codigo_por_sede")
                  .IsUnique();
        });

        ConfigureSedeScoped<ExistenciaAreaStock>(modelBuilder);

        ConfigureSedeScoped<Compra>(modelBuilder, entity =>
        {
            entity.HasIndex(e => new { e.SedeId, e.ProveedorId, e.NumFactura })
                  .HasDatabaseName("uq_compra_factura_por_sede")
                  .IsUnique();
        });

        ConfigureSedeScoped<DetalleCompra>(modelBuilder);
        ConfigureSedeScoped<Salida>(modelBuilder);
        ConfigureSedeScoped<BajaActivo>(modelBuilder);
        ConfigureSedeScoped<TrasladoActivo>(modelBuilder);
    }

    private void ConfigureSede(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sede>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sede_pkey");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => e.Clave)
                  .IsUnique()
                  .HasDatabaseName("sede_clave_key");
        });
    }

    private void ConfigureUsuarioSede(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsuarioSede>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.SedeId })
                  .HasName("usuario_sede_pkey");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.SedeId).HasColumnName("sede_id");

            entity.HasOne(d => d.Usuario)
                  .WithMany(p => p.UsuarioSede)
                  .HasForeignKey(d => d.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("usuario_sede_usuario_id_fkey");

            entity.HasOne(d => d.Sede)
                  .WithMany(p => p.UsuarioSede)
                  .HasForeignKey(d => d.SedeId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("usuario_sede_sede_id_fkey");
        });
    }

    
    private void ConfigureSedeScoped<TEntity>(
        ModelBuilder modelBuilder,
        Action<EntityTypeBuilder<TEntity>>? configure = null)
        where TEntity : class, ISedeScoped
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.Property(e => e.SedeId).HasColumnName("sede_id");

            entity.HasQueryFilter(e => !_tenant.HasTenant || e.SedeId == _tenant.Current!.Id);

            configure?.Invoke(entity);
        });
    }
}
