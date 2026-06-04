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

            // Otomatik tekrarlayan işlemleri tetikle
            await RecurringTransactionController.ProcessRecurringTransactionsAsync(_context, userId);

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

        // GET: Transaction/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction == null) return NotFound();

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", transaction.CategoryId);

            var model = new TransactionViewModel
            {
                Id = transaction.Id,
                CategoryId = transaction.CategoryId,
                Amount = transaction.Amount,
                Date = transaction.Date,
                Description = transaction.Description
            };

            return View(model);
        }

        // POST: Transaction/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (id != model.Id) return NotFound();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Net bakiye kontrolü (Eğer gider eklenecekse bakiyeyi kontrol et)
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category != null && category.Type == TransactionType.Expense)
                {
                    var totalIncome = await _context.Transactions
                        .Where(t => t.UserId == userId && t.Category.Type == TransactionType.Income)
                        .SumAsync(t => t.Amount);
                        
                    var totalExpense = await _context.Transactions
                        .Where(t => t.UserId == userId && t.Category.Type == TransactionType.Expense)
                        .SumAsync(t => t.Amount);
                        
                    // Orijinal işlemin tutarını hesaptan çıkarıp yenisini ekliyoruz
                    var originalExpense = transaction.Amount;
                    var netBalance = totalIncome - totalExpense + originalExpense;
                    
                    if (model.Amount > netBalance)
                    {
                        ModelState.AddModelError("", $"Hata: Yetersiz bakiye! Mevcut net bakiyeniz: {netBalance:C2}. Net bakiyeden daha fazla harcama yapamazsınız.");
                        var categoriesForError = await _context.Categories.ToListAsync();
                        ViewBag.Categories = new SelectList(categoriesForError, "Id", "Name");
                        return View(model);
                    }
                }

                // Bütçe aşımı kontrolü (Orijinal miktarı hesaptan düşerek)
                var isExceeded = await IsBudgetExceededOnEditAsync(userId, model.CategoryId, model.Amount, transaction.Id);
                if (isExceeded)
                {
                    ModelState.AddModelError("", "Uyarı: Bu harcama ile bu ayki bütçe limitinizi aşıyorsunuz!");
                    var categoriesForError = await _context.Categories.ToListAsync();
                    ViewBag.Categories = new SelectList(categoriesForError, "Id", "Name");
                    return View(model);
                }

                transaction.CategoryId = model.CategoryId;
                transaction.Amount = model.Amount;
                transaction.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
                transaction.Description = model.Description;

                _context.Update(transaction);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Dashboard");
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        // POST: Transaction/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (transaction == null) return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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

        private async Task<bool> IsBudgetExceededOnEditAsync(string userId, int categoryId, decimal newAmount, int originalTransactionId)
        {
            var now = DateTime.UtcNow;

            var budget = await _context.BudgetLimits
                .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == categoryId && b.Month == now.Month && b.Year == now.Year);

            if (budget == null) return false;

            var currentExpenses = await _context.Transactions
                .Where(t => t.UserId == userId && t.CategoryId == categoryId && t.Id != originalTransactionId && t.Date.Month == now.Month && t.Date.Year == now.Year)
                .SumAsync(t => t.Amount);

            return (currentExpenses + newAmount) > budget.LimitAmount;
        }
    }
}
