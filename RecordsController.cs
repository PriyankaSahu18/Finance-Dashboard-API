using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinanceDashboard.Data;
using FinanceDashboard.Models;

namespace FinanceDashboard.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/transactions")] 
    public class RecordsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RecordsController(AppDbContext context)
        {
            _db = context;
        }

        // CREATE TRANSACTION (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddTransaction(FinancialRecord input)
        {
            if (input.Amount <= 0)
                return BadRequest("Amount should be greater than zero");

            if (string.IsNullOrWhiteSpace(input.Type))
                return BadRequest("Transaction type is required");

            _db.FinancialRecords.Add(input);
            _db.SaveChanges();

            return Ok(new { message = "Transaction added", data = input });
        }

        // GET ALL TRANSACTIONS (Admin, Analyst)
        [Authorize(Roles = "Admin,Analyst")]
        [HttpGet]
        public IActionResult GetTransactions()
        {
            var transactions = _db.FinancialRecords.ToList();
            return Ok(transactions);
        }

        // UPDATE (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateTransaction(int id, FinancialRecord input)
        {
            var existing = _db.FinancialRecords.Find(id);

            if (existing == null)
                return NotFound("Transaction not found");

            existing.Amount = input.Amount;
            existing.Type = input.Type;
            existing.Category = input.Category;
            existing.Date = input.Date;
            existing.Description = input.Description;

            _db.SaveChanges();

            return Ok(new { message = "Transaction updated", data = existing });
        }

        // DELETE (Admin)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public IActionResult RemoveTransaction(int id)
        {
            var item = _db.FinancialRecords.Find(id);

            if (item == null)
                return NotFound("Transaction not found");

            _db.FinancialRecords.Remove(item);
            _db.SaveChanges();

            return Ok(new { message = "Transaction deleted" });
        }

        // FILTER (Admin, Analyst)
        [Authorize(Roles = "Admin,Analyst")]
        [HttpGet("search")]
        public IActionResult Search(string? type, string? category)
        {
            var data = _db.FinancialRecords.AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                data = data.Where(x => x.Type.ToLower() == type.ToLower());

            if (!string.IsNullOrWhiteSpace(category))
                data = data.Where(x => x.Category.ToLower() == category.ToLower());

            return Ok(data.ToList());
        }

        // DATE RANGE FILTER
        [Authorize(Roles = "Admin,Analyst")]
        [HttpGet("date-range")]
        public IActionResult GetByDateRange(DateTime start, DateTime end)
        {
            if (start > end)
                return BadRequest("Invalid date range");

            var result = _db.FinancialRecords
                .Where(x => x.Date >= start && x.Date <= end)
                .ToList();

            return Ok(result);
        }

        // DASHBOARD SUMMARY 
        [HttpGet("overview")]
        public IActionResult GetOverview()
        {
            var allData = _db.FinancialRecords.ToList();

            var income = allData.Where(x => x.Type == "Income").Sum(x => x.Amount);
            var expense = allData.Where(x => x.Type == "Expense").Sum(x => x.Amount);

            return Ok(new
            {
                TotalIncome = income,
                TotalExpense = expense,
                NetBalance = income - expense
            });
        }

        // CATEGORY ANALYSIS
        [HttpGet("category-analysis")]
        public IActionResult CategoryAnalysis()
        {
            var result = _db.FinancialRecords
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                });

            return Ok(result);
        }

        // MONTHLY REPORT 
        [HttpGet("monthly-report")]
        public IActionResult MonthlyReport()
        {
            var result = _db.FinancialRecords
                .GroupBy(x => x.Date.Month)
                .Select(g => new
                {
                    Month = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                    Total = g.Sum(x => x.Amount)
                });

            return Ok(result);
        }
        //WEEKLY REPORT
        [HttpGet("weekly-report")]
        public IActionResult WeeklyReport()
        {
            var result = _db.FinancialRecords
                .GroupBy(x => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    x.Date,
                    System.Globalization.CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday))
                .Select(g => new
                {
                    Week = "Week " + g.Key,
                    Total = g.Sum(x => x.Amount)
                });

            return Ok(result);
        }

       //RECENT TRANSACTIONS
        [HttpGet("recent")]
        public IActionResult RecentTransactions()
        {
            var latest = _db.FinancialRecords
                .OrderByDescending(x => x.Date)
                .Take(5)
                .ToList();

            return Ok(latest);
        }

        //TOTAL TRANSACTIONS + HIGHEST EXPENSE
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var totalRecords = _db.FinancialRecords.Count();
            var highestExpense = _db.FinancialRecords
                .Where(x => x.Type == "Expense")
                .OrderByDescending(x => x.Amount)
                .FirstOrDefault();

            return Ok(new
            {
                TotalTransactions = totalRecords,
                HighestExpense = highestExpense
            });
        }
    }
}