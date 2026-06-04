using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PennyWise.Models;

namespace PennyWise.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BudgetLimit> BudgetLimits { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<SavingsGoal> SavingsGoals { get; set; }
        public DbSet<RecurringTransaction> RecurringTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Decimal precision configurations
            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");
                
            builder.Entity<BudgetLimit>()
                .Property(b => b.LimitAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Bill>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<SavingsGoal>()
                .Property(s => s.TargetAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<SavingsGoal>()
                .Property(s => s.CurrentAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<RecurringTransaction>()
                .Property(r => r.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
