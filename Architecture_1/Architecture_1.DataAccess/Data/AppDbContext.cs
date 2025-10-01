using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Architecture_1.DataAccess.Entities;

namespace Architecture_1.DataAccess.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    // DbSets
    public DbSet<SagaInstance> SagaInstances { get; set; } = null!;
    public DbSet<SagaStepExcecution> SagaStepExcecutions { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Shared JSON converter for Dictionary<string, object>
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        // Non-nullable dictionary (store as JSON)
        var dictToJson = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions)!);

        // Nullable dictionary (store as JSON)
        var nullableDictToJson = new ValueConverter<Dictionary<string, object>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(v!, jsonOptions)!);

        // SagaInstance
        modelBuilder.Entity<SagaInstance>(entity =>
        {
            entity.HasKey(e => e.SagaId);

            entity.Property(e => e.FlowName)
                  .IsRequired();

            entity.Property(e => e.InitialData)
                  .HasConversion(dictToJson)
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.ResultData)
                  .HasConversion(dictToJson)
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.FlowStatus)
                  .HasConversion<int>(); // store enum as int

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("datetime2");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnType("datetime2");

            entity.Property(e => e.CompletedAt)
                  .HasColumnType("datetime2");
        });

        // SagaStepExcecution
        modelBuilder.Entity<SagaStepExcecution>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SagaId)
                  .IsRequired();

            entity.Property(e => e.StepName)
                  .IsRequired();

            entity.Property(e => e.TopicName);

            entity.Property(e => e.StepStatus)
                  .HasConversion<int>(); // store enum as int

            entity.Property(e => e.RequestData)
                  .HasConversion(nullableDictToJson)
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.responseData)
                  .HasConversion(nullableDictToJson)
                  .HasColumnType("nvarchar(max)");

            entity.Property(e => e.ErrorMessage);

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("datetime2");

            entity.HasIndex(e => e.SagaId); // helpful for lookups
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
