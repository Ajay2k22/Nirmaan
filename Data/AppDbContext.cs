using Microsoft.EntityFrameworkCore;
using Nirmaan.Api.Models;

namespace Nirmaan.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<MasterData> MasterDatas => Set<MasterData>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Code> Codes => Set<Code>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<MasterData>()
            .HasIndex(m => new { m.MNC, m.DEPT })
            .IsUnique();

        modelBuilder.Entity<Complaint>()
            .HasOne(c => c.User)
            .WithMany(u => u.Complaints)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Complaint>()
            .Property(c => c.PhotoBase64)
            .HasColumnType("text");
    }
}