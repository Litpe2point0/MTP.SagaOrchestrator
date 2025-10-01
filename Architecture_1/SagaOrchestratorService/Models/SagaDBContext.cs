using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace SagaOrchestratorService.Models
{
    public class SagaDBContext : DbContext
    {
        public SagaDBContext(DbContextOptions<SagaDBContext> options) : base(options)
        {
        }

        public DbSet<SagaInstanceEntity> SagaInstances { get; set; }
        public DbSet<SagaStepExecutionEntity> SagaStepExecutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaInstanceEntity>(entity =>
            {
                entity.HasKey(e => e.SagaId);
                entity.Property(e => e.FlowName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CurrentStep).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.StartTime).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.ContextJson).HasColumnType("nvarchar(max)");
                
                // Add concurrency token for optimistic concurrency control
                entity.Property(e => e.RowVersion)
                      .IsRowVersion()
                      .IsConcurrencyToken();
                
                // Configure relationship
                entity.HasMany(e => e.Steps)
                      .WithOne(e => e.SagaInstance)
                      .HasForeignKey(e => e.SagaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SagaStepExecutionEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Step).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Service).HasMaxLength(100);
                entity.Property(e => e.Action).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.StartedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.Property(e => e.ResultJson).HasColumnType("nvarchar(max)");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
