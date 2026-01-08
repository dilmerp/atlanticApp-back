using Common.Domain.Entities;
using Common.Domain.Interfaces;
using Common.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Persistence.Data
{
    public class AppDbContext : DbContext, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // -----------------------------------
        // DbSets
        // -----------------------------------

        public DbSet<CargaArchivo> CargaArchivos { get; set; } = default!;

        public DbSet<DataProcesada> DataProcesadas { get; set; } = default!;

        // -----------------------------------
        // Configuración de entidades
        // -----------------------------------

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbatlantic"); 
            base.OnModelCreating(modelBuilder);

            // -------------------------------
            // CargaArchivo
            // -------------------------------
            modelBuilder.Entity<CargaArchivo>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Estado)
                      .HasConversion(
                          v => v.ToString().ToUpper(),
                          v => (EstadoCarga)Enum.Parse(typeof(EstadoCarga), v, true))
                      .IsRequired();

                entity.HasIndex(e => e.Estado);
            });

            // -------------------------------
            // DataProcesada
            // -------------------------------
            modelBuilder.Entity<DataProcesada>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CodigoProducto)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(e => e.CodigoProducto)
                      .IsUnique();

                entity.Property(e => e.NombreProducto)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.Precio)
                      .HasColumnType("numeric(18,2)");

                entity.Property(e => e.Periodo)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.HasOne(e => e.CargaArchivo)
                      .WithMany()
                      .HasForeignKey(e => e.CargaArchivoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // -----------------------------------
        // Persistencia
        // -----------------------------------

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
