using Finance_Tracker.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Finance_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
			CultureInfo cultureUSD = new CultureInfo("en-US");

			// Last 7 days 
			DateTime StartDate = DateTime.Today.AddDays(-6);
			DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            // Total income
            decimal TotalIncome = SelectedTransactions.Where(e => e.Category.Type == "Income").Sum(x => x.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C2", cultureUSD);

			// Total expense
			decimal TotalExpense = SelectedTransactions.Where(e => e.Category.Type == "Expense").Sum(x => x.Amount);
			ViewBag.TotalExpense = TotalExpense.ToString("C2", cultureUSD);

            // Balance
            decimal Balance = TotalIncome - TotalExpense;

			CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            ViewBag.Balance = String.Format(culture, "{0:C2}", Balance);

            // Doughnutchart - expense by category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(e => e.Category.Type == "Expense")
                .GroupBy(e => e.Category.CategoryId)
                .Select(e => new
                {
                    categoryTitleWithIcon = e.First().Category.TitleWithIcon, //e.First().Category.Icon + " " + e.First().Category.Title,
					amount = e.Sum(l => l.Amount),
                    formattedAmount = e.Sum(l => l.Amount).ToString("C2", cultureUSD),
                })
                .ToList();

			return View();
        }



    }
}
