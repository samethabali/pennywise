using PennyWise.Models;
using System.Collections.Generic;

namespace PennyWise.ViewModels
{
    public class AdminUserDetailsViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        
        public int TotalTransactionCount { get; set; }
        public decimal TotalTransactionVolume { get; set; }
        public int TotalBudgetLimitsCount { get; set; }

        public List<Transaction> RecentTransactions { get; set; }
        public List<BudgetLimit> BudgetLimits { get; set; }
    }
}
