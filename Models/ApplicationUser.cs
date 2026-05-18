using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace PennyWise.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Transaction> Transactions { get; set; }
        public ICollection<BudgetLimit> BudgetLimits { get; set; }
    }
}
