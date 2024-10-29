using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SongRequestModel> SongRequests { get; set; }
    public DbSet<UserModel> Users { get; set; }
    public DbSet<LightningInvoice> LightningInvoices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SongRequestModel>()
            .Property(y => y.YoutubeLink).IsRequired();
        
        modelBuilder.Entity<SongRequestModel>()
            .Property(y => y.Invoice).IsRequired();
        
        modelBuilder.Entity<SongRequestModel>()
            .HasOne(y => y.User)
            .WithMany(u => u.YoutubeRequests)
            .HasForeignKey(y => y.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModel>()
            .Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        modelBuilder.Entity<UserModel>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<LightningInvoice>()
            .HasIndex(l => l.Id)
            .IsUnique();
    }
}