using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Finance_Tracker.Models;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using NuGet.Configuration;
using System.Text;
using Finance_Tracker.DTOs;

namespace Finance_Tracker.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transaction
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Transactions.Include(t => t.Category);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Transaction/AddOrEdit
        public IActionResult AddOrEdit(int id=0)
        {
            PopulateCategories();
            if(id == 0)
            {
                return View(new Transaction());
            }
            else
            {
                return View(_context.Transactions.Find(id));
            }
        }

        // POST: Transaction/AddOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                if (transaction.TransactionId == 0)
                {
                    _context.Add(transaction);
                }
                else
                {
                    _context.Update(transaction);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateCategories();
            return View(transaction);
        }


        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Transactions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Transactions'  is null.");
            }
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public void PopulateCategories()
        {
            var CategoryCollection = _context.Categories.ToList();
            Category DefaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }

        // POST: Transaction/ImportTransactions
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTransactions(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return Json(new { success = false, errorMessage = "File is empty" });
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await csvFile.CopyToAsync(stream);
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream, Encoding.GetEncoding("Windows-1250")))
                    {
                        List<string> lines = new List<string>();
                        string currentLine;
                        bool foundHeaders = false;
                        while ((currentLine = reader.ReadLine()) != null)
                        {
                            if (!foundHeaders && currentLine.Contains("Data transakcji"))
                            {
                                foundHeaders = true;
                                lines.Add(currentLine);
                                continue;
                            }
                            if (foundHeaders)
                            {
                                lines.Add(currentLine);
                            }
                        }

                        // Adding basic categories if they don't already exist
                        var ExpenseCategoryExists = _context.Categories.Any(c => c.Title == "Expense");
                        var IncomeCategoryExists = _context.Categories.Any(c => c.Title == "Income");
                        if (!ExpenseCategoryExists)
                        {
                            _context.Categories.Add(new Category { Title = "Expense", Icon = "🔻", Type = "Expense" });
                        }
                        if (!IncomeCategoryExists)
                        {
                            _context.Categories.Add(new Category { Title = "Income", Icon = "🤑", Type = "Income" });
                        }
                        await _context.SaveChangesAsync();

                        // Getting basic category id
                        int ExpenseCategoryId = _context.Categories
                                                   .Where(c => c.Title == "Expense")
                                                   .Select(c => c.CategoryId)
                                                   .FirstOrDefault();

                        int IncomeCategoryId = _context.Categories
                                                .Where(c => c.Title == "Income")
                                                .Select(c => c.CategoryId)
                                                .FirstOrDefault();



                        if (foundHeaders)
                        {
                            using (var csvStream = new MemoryStream())
                            using (var csvWriter = new StreamWriter(csvStream))
                            {
                                foreach (var line in lines)
                                {
                                    csvWriter.WriteLine(line);
                                }
                                csvWriter.Flush();
                                csvStream.Position = 0;
                                using (var csvReader = new StreamReader(csvStream))
                                using (var csv = new CsvReader(csvReader, new CsvConfiguration(CultureInfo.InvariantCulture) { 
                                    HasHeaderRecord = true,
                                    BadDataFound = null, 
                                    Delimiter = ";", 
                                    MissingFieldFound = null, 
                                }))
                                {
                                    csv.Read();
                                    csv.ReadHeader();

                                    
                                    while (csv.Read())
                                    {
                                        string transactionDate = csv.GetField("Data transakcji");
                                        string title = csv.GetField("Tytuł") ?? "";
                                        string amount = csv.GetField("Kwota transakcji (waluta rachunku)")?.Replace(',', '.') ?? "0";

                                        if (!(string.IsNullOrWhiteSpace(transactionDate) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(amount)))
                                        {

                                            DateTime converted_date = Convert.ToDateTime(transactionDate);
                                            decimal converted_amount;
                                            if (!decimal.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out converted_amount))
                                            {
                                                converted_amount = 0;
                                            }


                                            if (converted_amount > 0)
                                            {
                                                _context.Transactions.Add(new Transaction { CategoryId = IncomeCategoryId, Amount = converted_amount, Note = title, Date = converted_date });
                                            }
                                            else
                                            {
                                                converted_amount = converted_amount * (-1);
                                                _context.Transactions.Add(new Transaction { CategoryId = ExpenseCategoryId, Amount = converted_amount, Note = title, Date = converted_date });
                                            }
                                        }


                                        if (string.IsNullOrWhiteSpace(title))
                                        {
                                            break;
                                        }
                                    }
                                    await _context.SaveChangesAsync();

                                }
                            }
                        }
                        else
                        {
                            return Json(new { success = false, errorMessage = "File is empty" });
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = $"{ex.Message}" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> ExportTransactionsToCsv()
        {
            var transactions = await _context.Transactions.Include(t => t.Category).ToListAsync();

            // Transforming to DTO object
            var transactionsToExport = transactions.Select(t => new TransactionExportDto
            {
                Date = t.Date,
                Note = t.Note,
                Category = t.CategoryTitleWithIcon,
                Type = t.Category.Type,
                Amount = t.FormattedAmount,
            }).ToList();


            var memoryStream = new MemoryStream();
            using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            using (var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "," }))
            {
                csvWriter.WriteRecords(transactionsToExport);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            return File(memoryStream, "text/csv", "transactions.csv");
        }



    }
}
