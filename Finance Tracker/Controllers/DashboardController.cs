using Finance_Tracker.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;

namespace Finance_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        public DashboardController(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
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
                .OrderByDescending(e => e.amount)
                .ToList();


            // Spline Chart - income vs expense

            // Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(e => e.Category.Type == "Income")
                .GroupBy(e => e.Date)
                .Select(e => new SplineChartData()
                {
                    day = e.First().Date.ToString("dd-MMM-yyyy"),
                    income = e.Sum(i => i.Amount),
                })
                .ToList();

			// Expense
			List<SplineChartData> ExpenseSummary = SelectedTransactions
				.Where(e => e.Category.Type == "Expense")
				.GroupBy(e => e.Date)
				.Select(e => new SplineChartData()
				{
					day = e.First().Date.ToString("dd-MMM-yyyy"),
					expense = e.Sum(i => i.Amount),
				})
				.ToList();

            // Combine income and expense
            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(e => StartDate.AddDays(e).ToString("dd-MMM-yyyy"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into dayExpenseJoined
                                      from expense in dayExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            // Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(e => e.Category)
                .OrderByDescending(e => e.Date)
                .Take(5)
                .ToListAsync();


            // Downloading currency rates
            var client = _clientFactory.CreateClient();
            string apiUrl = "https://cdn.kurs-walut.info/api/latest.json";
            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var currencyData = JsonConvert.DeserializeObject<CurrencyRates>(responseData);

                // Preparation of code and currency rate data
                var CurrencyRatesData = currencyData.Rates.Select(rate => new {
                    Text = rate.Key,
                    Value = rate.Value
                }).ToList();

                ViewBag.CurrencyRatesData = CurrencyRatesData;
            }


            return View();
        }



    }

    public class SplineChartData
    {
        public string day;
        public decimal income;
        public decimal expense;
    }

    public class CurrencyRates
    {
        public string Table { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
        public DateTime LastUpdate { get; set; }
    }

}
