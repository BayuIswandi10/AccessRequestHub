using AccessRequestHub.Domain.Entities;
using AccessRequestHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using AppEntity = AccessRequestHub.Domain.Entities.Application;

namespace AccessRequestHub.Infrastructure.Data;

public class AccessRequestDbContext : DbContext
{
    public AccessRequestDbContext(DbContextOptions<AccessRequestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AppEntity> Applications => Set<AppEntity>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Manager)
                  .WithMany(m => m.DirectReports)
                  .HasForeignKey(e => e.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.SystemOwner)
                  .WithMany()
                  .HasForeignKey(e => e.SystemOwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccessRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientRequestId).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.BusinessJustification).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.PolicyVersion).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Environment).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.AccessLevel).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(e => e.Requester)
                  .WithMany()
                  .HasForeignKey(e => e.RequesterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Application)
                  .WithMany()
                  .HasForeignKey(e => e.ApplicationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(2000);

            entity.HasOne(e => e.AccessRequest)
                  .WithMany(a => a.AuditEvents)
                  .HasForeignKey(e => e.AccessRequestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Actor)
                  .WithMany()
                  .HasForeignKey(e => e.ActorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
