using Microsoft.EntityFrameworkCore;
using FinanceTracker.Models;

namespace FinanceTracker.Data;

public class AppDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
    options.UseSqlite("Data Source=finance.db");
            

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");
    }
}
