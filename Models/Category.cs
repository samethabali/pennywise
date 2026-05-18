using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PennyWise.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Örn: Market, Kira, Maaş
        
        [Required]
        public TransactionType Type { get; set; } // Enum: Income (Gelir) / Expense (Gider)
        
        public ICollection<Transaction> Transactions { get; set; }
        public ICollection<BudgetLimit> BudgetLimits { get; set; }
    }

    public enum TransactionType { Income, Expense }
}
