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
    public class RecurringTransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecurringTransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Abonelikleri Listele
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Otomatik tekrarlayan işlemleri tetikle
            await ProcessRecurringTransactionsAsync(_context, userId);

            var recurringTransactions = await _context.RecurringTransactions
                .Where(r => r.UserId == userId)
                .Include(r => r.Category)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.NextOccurrenceDate)
                .ToListAsync();

            return View(recurringTransactions);
        }

        // Yeni Abonelik Oluşturma (GET)
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            return View(new RecurringTransactionViewModel
            {
                StartDate = DateTime.Now,
                RecurrenceType = RecurrenceType.Monthly
            });
        }

        // Yeni Abonelik Oluşturma (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecurringTransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (ModelState.IsValid)
            {
                var recurring = new RecurringTransaction
                {
                    UserId = userId,
                    CategoryId = model.CategoryId,
                    Amount = model.Amount,
                    Description = model.Description,
                    RecurrenceType = model.RecurrenceType,
                    StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                    EndDate = model.EndDate.HasValue
                        ? DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc)
                        : null,
                    NextOccurrenceDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                    IsActive = true
                };

                _context.RecurringTransactions.Add(recurring);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        // Abonelik Düzenleme (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recurring == null) return NotFound();

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", recurring.CategoryId);

            var model = new RecurringTransactionViewModel
            {
                CategoryId = recurring.CategoryId,
                Amount = recurring.Amount,
                Description = recurring.Description,
                RecurrenceType = recurring.RecurrenceType,
                StartDate = recurring.StartDate,
                EndDate = recurring.EndDate
            };

            ViewBag.RecurringId = id;
            return View(model);
        }

        // Abonelik Düzenleme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecurringTransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recurring == null) return NotFound();

            if (ModelState.IsValid)
            {
                recurring.CategoryId = model.CategoryId;
                recurring.Amount = model.Amount;
                recurring.Description = model.Description;
                recurring.RecurrenceType = model.RecurrenceType;
                recurring.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
                recurring.EndDate = model.EndDate.HasValue
                    ? DateTime.SpecifyKind(model.EndDate.Value, DateTimeKind.Utc)
                    : null;

                // Eğer başlangıç tarihi değiştiyse, NextOccurrenceDate'i güncelle
                if (recurring.NextOccurrenceDate < DateTime.UtcNow)
                {
                    recurring.NextOccurrenceDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
            ViewBag.RecurringId = id;
            return View(model);
        }

        // Abonelik Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recurring == null) return NotFound();

            _context.RecurringTransactions.Remove(recurring);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Aktif/Pasif Geçiş
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var recurring = await _context.RecurringTransactions
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recurring == null) return NotFound();

            recurring.IsActive = !recurring.IsActive;

            // Yeniden aktif edildiğinde, geçmiş kalmış NextOccurrenceDate'i bugüne çek
            if (recurring.IsActive && recurring.NextOccurrenceDate < DateTime.UtcNow)
            {
                recurring.NextOccurrenceDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Vadesi gelmiş tekrarlayan işlemleri otomatik olarak Transaction tablosuna yazar.
        /// Dashboard veya sayfa yüklenirken çağrılır (Lazy Evaluation).
        /// </summary>
        public static async Task ProcessRecurringTransactionsAsync(ApplicationDbContext context, string userId)
        {
            var now = DateTime.UtcNow;

            var dueRecurrings = await context.RecurringTransactions
                .Where(r => r.UserId == userId
                         && r.IsActive
                         && r.NextOccurrenceDate <= now
                         && (r.EndDate == null || r.EndDate >= now))
                .ToListAsync();

            foreach (var recurring in dueRecurrings)
            {
                // Bitiş tarihi geçmişse pasife al
                if (recurring.EndDate.HasValue && recurring.EndDate.Value < now)
                {
                    recurring.IsActive = false;
                    continue;
                }

                // Vadesi gelmiş her periyot için işlem oluştur (birden fazla kaçırılmış olabilir)
                while (recurring.NextOccurrenceDate <= now)
                {
                    // Bitiş tarihi kontrolü (döngü içinde de kontrol et)
                    if (recurring.EndDate.HasValue && recurring.NextOccurrenceDate > recurring.EndDate.Value)
                    {
                        recurring.IsActive = false;
                        break;
                    }

                    var transaction = new Transaction
                    {
                        UserId = userId,
                        CategoryId = recurring.CategoryId,
                        Amount = recurring.Amount,
                        Date = recurring.NextOccurrenceDate,
                        Description = $"🔄 {recurring.Description}"
                    };

                    context.Transactions.Add(transaction);

                    // Sonraki tekrar tarihini hesapla
                    recurring.NextOccurrenceDate = recurring.RecurrenceType switch
                    {
                        RecurrenceType.Daily => recurring.NextOccurrenceDate.AddDays(1),
                        RecurrenceType.Weekly => recurring.NextOccurrenceDate.AddDays(7),
                        RecurrenceType.Monthly => recurring.NextOccurrenceDate.AddMonths(1),
                        RecurrenceType.Yearly => recurring.NextOccurrenceDate.AddYears(1),
                        _ => recurring.NextOccurrenceDate.AddMonths(1)
                    };
                }
            }

            if (dueRecurrings.Any())
            {
                await context.SaveChangesAsync();
            }
        }
    }
}
