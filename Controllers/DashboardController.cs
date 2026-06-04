using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Models;
using PennyWise.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PennyWise.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var currentDate = DateTime.UtcNow;
            var model = await GetMonthlySummaryAsync(userId, currentDate.Month, currentDate.Year);
            return View(model);
        }

        // PDF Raporlama Görünümü
        [HttpGet]
        public async Task<IActionResult> PrintReport()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var currentDate = DateTime.UtcNow;
            var model = await GetMonthlySummaryAsync(userId, currentDate.Month, currentDate.Year);
            return View(model);
        }

        // CSV Raporu İndirme Özelliği
        [HttpGet]
        public async Task<IActionResult> ExportTransactionsCsv()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            var builder = new StringBuilder();
            // UTF-8 BOM ekliyoruz ki Türkçe karakterler Excel'de bozulmasın
            builder.AppendLine("Tarih,Açıklama,Kategori,Tür,Tutar");

            foreach (var transaction in transactions)
            {
                var typeStr = transaction.Category.Type == TransactionType.Income ? "Gelir" : "Gider";
                var dateStr = transaction.Date.ToString("dd.MM.yyyy");
                var descStr = transaction.Description?.Replace(",", " ") ?? "";
                
                builder.AppendLine($"{dateStr},{descStr},{transaction.Category.Name},{typeStr},{transaction.Amount}");
            }

            // Türkçe karakterlerin Excel'de düzgün görünmesi için Encoding.UTF8 kullanıyoruz
            var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            return File(csvBytes, "text/csv", $"PennyWise_Finans_Raporu_{DateTime.Now:yyyyMMdd}.csv");
        }

        private async Task<DashboardViewModel> GetMonthlySummaryAsync(string userId, int month, int year)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date.Month == month && t.Date.Year == year)
                .Include(t => t.Category)
                .ToListAsync();

            var categorySummary = transactions
                .Where(t => t.Category.Type == TransactionType.Expense)
                .GroupBy(t => t.Category.Name)
                .Select(g => new CategorySummaryDto
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                }).ToList();

            var incomeSummary = transactions
                .Where(t => t.Category.Type == TransactionType.Income)
                .GroupBy(t => t.Category.Name)
                .Select(g => new CategorySummaryDto
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                }).ToList();

            decimal totalIncome = transactions.Where(t => t.Category.Type == TransactionType.Income).Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Category.Type == TransactionType.Expense).Sum(t => t.Amount);

            // Yaklaşan ödenmemiş faturaları getir
            var upcomingBills = await _context.Bills
                .Where(b => b.UserId == userId && !b.IsPaid)
                .Include(b => b.Category)
                .OrderBy(b => b.DueDate)
                .Take(5)
                .ToListAsync();

            // Birikim hedeflerini getir
            var savingsGoals = await _context.SavingsGoals
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.TargetDate)
                .ToListAsync();

            return new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetBalance = totalIncome - totalExpense,
                CategoryDistribution = categorySummary,
                IncomeDistribution = incomeSummary,
                UpcomingBills = upcomingBills,
                SavingsGoals = savingsGoals
            };
        }
    }
}
