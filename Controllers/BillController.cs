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
    [Authorize]
    public class BillController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Faturaları listeleme
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var bills = await _context.Bills
                .Where(b => b.UserId == userId)
                .Include(b => b.Category)
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return View(bills);
        }

        // Yeni fatura oluşturma ekranı
        public async Task<IActionResult> Create()
        {
            var faturaCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Fatura" && c.Type == TransactionType.Expense);
            if (faturaCategory == null)
            {
                faturaCategory = new Category { Name = "Fatura", Type = TransactionType.Expense };
                _context.Categories.Add(faturaCategory);
                await _context.SaveChangesAsync();
            }

            return View(new Bill 
            { 
                DueDate = DateTime.Now.AddDays(7),
                CategoryId = faturaCategory.Id
            });
        }

        // Yeni fatura oluşturma işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bill bill)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // UserId'yi otomatik ata
            bill.UserId = userId;

            // Fatura kategorisini bul veya oluştur
            var faturaCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Fatura" && c.Type == TransactionType.Expense);
            if (faturaCategory == null)
            {
                faturaCategory = new Category { Name = "Fatura", Type = TransactionType.Expense };
                _context.Categories.Add(faturaCategory);
                await _context.SaveChangesAsync();
            }

            bill.CategoryId = faturaCategory.Id;

            // Npgsql 8.x için timestamptz UTC zorunluluğu
            bill.DueDate = DateTime.SpecifyKind(bill.DueDate, DateTimeKind.Utc);

            // ModelState'ten UserId ve Category/User nesne doğrulamalarını yoksay (Navigation properties)
            ModelState.Remove("UserId");
            ModelState.Remove("Category");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(bill);
        }

        // Faturayı Ödendi Olarak İşaretleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var bill = await _context.Bills.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (bill == null) return NotFound();

            if (!bill.IsPaid)
            {
                bill.IsPaid = true;

                // Fatura ödendiğinde bunu otomatik olarak "Transactions" tablosuna gider olarak kaydet!
                var transaction = new Transaction
                {
                    UserId = userId,
                    CategoryId = bill.CategoryId,
                    Amount = bill.Amount,
                    Date = DateTime.UtcNow,
                    Description = $"{bill.Title} Faturası Ödemesi"
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Fatura Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (bill == null) return NotFound();

            _context.Bills.Remove(bill);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
