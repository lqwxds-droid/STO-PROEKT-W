using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Green.Models;

namespace Green.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ServiceRequest> ServiceRequests { get; set; }
    public DbSet<ServiceCatalog> ServiceCatalogs { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceTask> ServiceTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Явная конфигурация связи ServiceRequest -> Services (один-ко-многим)
        builder.Entity<Service>()
            .HasOne(s => s.ServiceRequest)
            .WithMany(sr => sr.Services)
            .HasForeignKey(s => s.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Связь Service -> ServiceCatalog
        builder.Entity<Service>()
            .HasOne(s => s.ServiceCatalog)
            .WithMany()
            .HasForeignKey(s => s.ServiceCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь Vehicle -> Owner (ApplicationUser)
        builder.Entity<Vehicle>()
            .HasOne(v => v.Owner)
            .WithMany()
            .HasForeignKey(v => v.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-Many: ServiceRequest <-> ServiceCatalog (SelectedServices)
        builder.Entity<ServiceRequest>()
            .HasMany(sr => sr.SelectedServices)
            .WithMany(sc => sc.ServiceRequests)
            .UsingEntity<Dictionary<string, object>>(
                "ServiceRequestServiceCatalog",
                j => j.HasOne<ServiceCatalog>().WithMany().HasForeignKey("ServiceCatalogId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<ServiceRequest>().WithMany().HasForeignKey("ServiceRequestId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("ServiceRequestId", "ServiceCatalogId");
                    j.ToTable("ServiceRequestServiceCatalogs");
                }
            );
    }
}