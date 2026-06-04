using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PennyWise.Controllers
{
    [Authorize]
    public class SavingsGoalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SavingsGoalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Birikim Hedeflerini Listeleme
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var goals = await _context.SavingsGoals
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.TargetDate)
                .ToListAsync();

            return View(goals);
        }

        // Yeni Hedef Ekleme Sayfası
        public IActionResult Create()
        {
            return View(new SavingsGoal { TargetDate = DateTime.Now.AddMonths(6) });
        }

        // Yeni Hedef Ekleme İşlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SavingsGoal goal)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            goal.UserId = userId;
            goal.TargetDate = DateTime.SpecifyKind(goal.TargetDate, DateTimeKind.Utc);

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.SavingsGoals.Add(goal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(goal);
        }

        // Birikim Hedefine Para Ekleme İşlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFunds(int id, decimal amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (amount <= 0)
            {
                TempData["Error"] = "Eklenecek tutar sıfırdan büyük olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            var goal = await _context.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (goal == null) return NotFound();

            // Net bakiye kontrolü
            var totalIncome = await _context.Transactions.Where(t => t.UserId == userId && t.Category.Type == TransactionType.Income).SumAsync(t => t.Amount);
            var totalExpense = await _context.Transactions.Where(t => t.UserId == userId && t.Category.Type == TransactionType.Expense).SumAsync(t => t.Amount);
            var netBalance = totalIncome - totalExpense;

            if (amount > netBalance)
            {
                TempData["Error"] = $"Yetersiz bakiye! Mevcut net bakiyeniz: {netBalance:C2}. Hedefe daha fazla para ekleyemezsiniz.";
                return RedirectToAction(nameof(Index));
            }

            goal.CurrentAmount += amount;
            
            // Eğer hedef miktarı aşarsa hedef miktarında sabitleyelim
            if (goal.CurrentAmount > goal.TargetAmount)
            {
                goal.CurrentAmount = goal.TargetAmount;
            }

            // "Birikim" adında bir gider kategorisi var mı kontrol et, yoksa otomatik oluştur
            var savingsCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == "Birikim" && c.Type == TransactionType.Expense);
                
            if (savingsCategory == null)
            {
                savingsCategory = new Category
                {
                    Name = "Birikim",
                    Type = TransactionType.Expense
                };
                _context.Categories.Add(savingsCategory);
                await _context.SaveChangesAsync(); // Kategoriyi kaydet ki Id'sini alabilelim
            }

            // Birikime eklenen tutarı işlemler (Transactions) tablosuna gider olarak kaydet (Bakiyeden düşmesi için)
            var transaction = new Transaction
            {
                UserId = userId,
                CategoryId = savingsCategory.Id,
                Amount = amount,
                Date = DateTime.UtcNow,
                Description = $"'{goal.Title}' hedefine para eklendi"
            };
            
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{goal.Title}' hedefine başarıyla {amount:C2} eklendi!";

            return RedirectToAction(nameof(Index));
        }

        // Hedef Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var goal = await _context.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (goal == null) return NotFound();

            _context.SavingsGoals.Remove(goal);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Hedef Güncelleme Sayfası
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var goal = await _context.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (goal == null) return NotFound();

            return View(goal);
        }

        // Hedef Güncelleme İşlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SavingsGoal updatedGoal)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (id != updatedGoal.Id) return NotFound();

            var goal = await _context.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (goal == null) return NotFound();

            goal.Title = updatedGoal.Title;
            goal.TargetAmount = updatedGoal.TargetAmount;
            
            if (goal.CurrentAmount > goal.TargetAmount)
            {
                goal.CurrentAmount = goal.TargetAmount;
            }

            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Hedef başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
