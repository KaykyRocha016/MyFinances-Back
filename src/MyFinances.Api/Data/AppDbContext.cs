using Microsoft.EntityFrameworkCore;
using MyFinances.Api.Models;

namespace MyFinances.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<Cycle> Cycles => Set<Cycle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map to lowercase snake_case tables and columns
        modelBuilder.Entity<Household>(entity =>
        {
            entity.ToTable("households");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
        });

        modelBuilder.Entity<Cycle>(entity =>
        {
            entity.ToTable("cycles");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");

            entity.HasOne(c => c.Household)
                .WithMany()
                .HasForeignKey(c => c.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.Income).HasColumnName("income").HasPrecision(18, 2);
            entity.Property(e => e.HouseholdId).HasColumnName("household_id");

            entity.HasOne(u => u.Household)
                .WithMany()
                .HasForeignKey(u => u.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(e => e.DivisionType).HasColumnName("division_type").HasMaxLength(50);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("expenses");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CycleId).HasColumnName("cycle_id");
            entity.Property(e => e.InstallmentNumber).HasColumnName("installment_number");
            entity.Property(e => e.TotalInstallments).HasColumnName("total_installments");
            entity.Property(e => e.InstallmentGroupId).HasColumnName("installment_group_id");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Category)
                .WithMany()
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Cycle)
                .WithMany()
                .HasForeignKey(d => d.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseSplit>(entity =>
        {
            entity.ToTable("expense_splits");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);

            entity.HasOne(d => d.Expense)
                .WithMany(p => p.Splits)
                .HasForeignKey(d => d.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
