using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PennyWise.ViewComponents
{
    public class BalanceViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public BalanceViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return View(0m);
            }

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .ToListAsync();

            decimal totalIncome = transactions.Where(t => t.Category.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Category.Type == TransactionType.Expense).Sum(t => t.Amount);

            decimal remainingBalance = totalIncome - totalExpense;

            return View(remainingBalance);
        }
    }
}
