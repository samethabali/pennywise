using System.Collections.Generic;
using PennyWise.Models;

namespace PennyWise.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; }
        public List<CategorySummaryDto> CategoryDistribution { get; set; }
        public List<Bill> UpcomingBills { get; set; }
        public List<SavingsGoal> SavingsGoals { get; set; }
    }

    public class CategorySummaryDto
    {
        public string? CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
