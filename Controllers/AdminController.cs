using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Models;
using PennyWise.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PennyWise.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Index (Admin Dashboard)
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            
            var totalTransactionsCount = await _context.Transactions.CountAsync();
            var totalTransactionVolume = await _context.Transactions.SumAsync(t => t.Amount);

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalCategories = totalCategories,
                TotalTransactionsCount = totalTransactionsCount,
                TotalTransactionVolume = totalTransactionVolume
            };

            return View(model);
        }

        // GET: /Admin/Users (User Management)
        public async Task<IActionResult> Users()
        {
            // List all users except the currently logged in Admin (to prevent self-deletion)
            var currentUserId = _userManager.GetUserId(User);
            
            // Getting users with stats
            var users = await _context.Users
                .Where(u => u.Id != currentUserId)
                .Select(u => new 
                {
                    User = u,
                    TransactionCount = _context.Transactions.Count(t => t.UserId == u.Id),
                    TransactionVolume = _context.Transactions.Where(t => t.UserId == u.Id).Sum(t => (decimal?)t.Amount) ?? 0m,
                    BudgetLimitCount = _context.BudgetLimits.Count(b => b.UserId == u.Id)
                })
                .ToListAsync();

            ViewBag.Users = users;
            return View();
        }

        // POST: /Admin/DeleteUser/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent deleting another Admin to be safe, or just allow it. Let's allow but ensure we can't delete self.
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "Kendinizi silemezsiniz!";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Kullanıcı ({user.Email}) başarıyla silindi. Bu kullanıcıya ait tüm harcamalar da temizlendi.";
            }
            else
            {
                TempData["Error"] = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/UserDetails/id
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == id)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToListAsync();

            var budgetLimits = await _context.BudgetLimits
                .Include(b => b.Category)
                .Where(b => b.UserId == id)
                .ToListAsync();

            var model = new AdminUserDetailsViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email,
                TotalTransactionCount = await _context.Transactions.CountAsync(t => t.UserId == id),
                TotalTransactionVolume = await _context.Transactions.Where(t => t.UserId == id).SumAsync(t => (decimal?)t.Amount) ?? 0m,
                TotalBudgetLimitsCount = budgetLimits.Count,
                RecentTransactions = transactions,
                BudgetLimits = budgetLimits
            };

            return View(model);
        }
    }
}
