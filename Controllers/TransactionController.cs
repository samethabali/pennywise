using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Models;
using PennyWise.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PennyWise.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
            return View(transactions);
        }

        public async Task<IActionResult> Create()
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new TransactionViewModel { Date = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (ModelState.IsValid)
            {
                // Net Bakiye Kontrolü (Gider eklenecekse bakiyeyi kontrol et)
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category != null && category.Type == TransactionType.Expense)
                {
                    var totalIncome = await _context.Transactions
                        .Where(t => t.UserId == userId && t.Category.Type == TransactionType.Income)
                        .SumAsync(t => t.Amount);
                        
                    var totalExpense = await _context.Transactions
                        .Where(t => t.UserId == userId && t.Category.Type == TransactionType.Expense)
                        .SumAsync(t => t.Amount);
                        
                    var netBalance = totalIncome - totalExpense;
                    
                    if (model.Amount > netBalance)
                    {
                        ModelState.AddModelError("", $"Hata: Yetersiz bakiye! Mevcut net bakiyeniz: {netBalance:C2}. Net bakiyeden daha fazla harcama yapamazsınız.");
                        var categoriesForError = await _context.Categories.ToListAsync();
                        ViewBag.Categories = new SelectList(categoriesForError, "Id", "Name");
                        return View(model);
                    }
                }

                var isExceeded = await IsBudgetExceededAsync(userId, model.CategoryId, model.Amount);
                if (isExceeded)
                {
                    ModelState.AddModelError("", "Uyarı: Bu harcama ile bu ayki bütçe limitinizi aşıyorsunuz!");
                    // Gerekirse uyarıya rağmen kaydetme opsiyonu eklenebilir, şimdilik engelliyoruz
                    var categoriesForError = await _context.Categories.ToListAsync();
                    ViewBag.Categories = new SelectList(categoriesForError, "Id", "Name");
                    return View(model);
                }

                var transaction = new Transaction
                {
                    UserId = userId,
                    CategoryId = model.CategoryId,
                    Amount = model.Amount,
                    // Npgsql 8.x: timestamptz kolonları için UTC zorunlu
                    Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc),
                    Description = model.Description
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Dashboard");
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(model);
        }

        private async Task<bool> IsBudgetExceededAsync(string userId, int categoryId, decimal newAmount)
        {
            var now = DateTime.UtcNow;

            var budget = await _context.BudgetLimits
                .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == categoryId && b.Month == now.Month && b.Year == now.Year);

            if (budget == null) return false;

            var currentExpenses = await _context.Transactions
                .Where(t => t.UserId == userId && t.CategoryId == categoryId && t.Date.Month == now.Month && t.Date.Year == now.Year)
                .SumAsync(t => t.Amount);

            return (currentExpenses + newAmount) > budget.LimitAmount;
        }
    }
}
