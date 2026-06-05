using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PennyWise.Controllers
{
    [Authorize(Roles = "User")]
    public class BudgetLimitController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetLimitController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bütçe Limitlerini Listeleme + Harcama Durumunu Gösterme
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var now = DateTime.UtcNow;

            var budgetLimits = await _context.BudgetLimits
                .Where(b => b.UserId == userId && b.Month == now.Month && b.Year == now.Year)
                .Include(b => b.Category)
                .OrderBy(b => b.Category.Name)
                .ToListAsync();

            // LINQ ile GroupBy ve Sum: Her kategori için bu ayki toplam harcamayı hesapla
            var currentExpenses = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date.Month == now.Month && t.Date.Year == now.Year)
                .Include(t => t.Category)
                .Where(t => t.Category.Type == TransactionType.Expense)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, TotalSpent = g.Sum(t => t.Amount) })
                .ToListAsync();

            // Her limit için mevcut harcamayı ViewBag üzerinden gönder
            var expenseDict = currentExpenses.ToDictionary(e => e.CategoryId, e => e.TotalSpent);
            ViewBag.CurrentExpenses = expenseDict;
            ViewBag.CurrentMonth = now.ToString("MMMM yyyy");

            return View(budgetLimits);
        }

        // Yeni Bütçe Limiti Oluşturma Sayfası
        public async Task<IActionResult> Create()
        {
            // Sadece Expense (Gider) kategorilerini getir
            var expenseCategories = await _context.Categories
                .Where(c => c.Type == TransactionType.Expense)
                .ToListAsync();

            ViewBag.Categories = new SelectList(expenseCategories, "Id", "Name");
            
            var now = DateTime.UtcNow;
            var model = new BudgetLimit
            {
                Month = now.Month,
                Year = now.Year
            };

            return View(model);
        }

        // Yeni Bütçe Limiti Kaydetme İşlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BudgetLimit budgetLimit)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            budgetLimit.UserId = userId;

            // Aynı kategori + ay + yıl için zaten limit var mı kontrol et
            var existingLimit = await _context.BudgetLimits
                .FirstOrDefaultAsync(b => b.UserId == userId 
                    && b.CategoryId == budgetLimit.CategoryId 
                    && b.Month == budgetLimit.Month 
                    && b.Year == budgetLimit.Year);

            if (existingLimit != null)
            {
                ModelState.AddModelError("", "Bu kategori için bu ay/yıl zaten bir bütçe limiti tanımlanmış!");
            }

            // Navigation property doğrulamalarını yoksay
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                _context.BudgetLimits.Add(budgetLimit);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Bütçe limiti başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Index));
            }

            var expenseCategories = await _context.Categories
                .Where(c => c.Type == TransactionType.Expense)
                .ToListAsync();
            ViewBag.Categories = new SelectList(expenseCategories, "Id", "Name", budgetLimit.CategoryId);
            return View(budgetLimit);
        }

        // Bütçe Limiti Düzenleme Sayfası (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var budgetLimit = await _context.BudgetLimits
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budgetLimit == null) return NotFound();

            var expenseCategories = await _context.Categories
                .Where(c => c.Type == TransactionType.Expense)
                .ToListAsync();

            ViewBag.Categories = new SelectList(expenseCategories, "Id", "Name", budgetLimit.CategoryId);

            return View(budgetLimit);
        }

        // Bütçe Limiti Düzenleme İşlemi (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BudgetLimit budgetLimit)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var existing = await _context.BudgetLimits
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (existing == null) return NotFound();

            // Navigation property doğrulamalarını yoksay
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                existing.CategoryId = budgetLimit.CategoryId;
                existing.LimitAmount = budgetLimit.LimitAmount;
                existing.Month = budgetLimit.Month;
                existing.Year = budgetLimit.Year;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Bütçe limiti başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }

            var expenseCategories = await _context.Categories
                .Where(c => c.Type == TransactionType.Expense)
                .ToListAsync();
            ViewBag.Categories = new SelectList(expenseCategories, "Id", "Name", budgetLimit.CategoryId);
            return View(budgetLimit);
        }

        // Bütçe Limiti Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var budgetLimit = await _context.BudgetLimits
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budgetLimit == null) return NotFound();

            _context.BudgetLimits.Remove(budgetLimit);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bütçe limiti başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }
    }
}
