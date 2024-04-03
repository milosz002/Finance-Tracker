using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Finance_Tracker.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }


        // CategoryId - foreign key
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }


        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, int.MaxValue, ErrorMessage = "Amount should be greater then 0.")]
        public decimal Amount { get; set; }


        [Column(TypeName = "nvarchar(1000)")]
        public string? Note { get; set; }


        public DateTime Date { get; set; } = DateTime.Now;


		[NotMapped]
        public string? CategoryTitleWithIcon 
        { 
            get
            {
                return Category == null ? "" : Category.Icon + " " + Category.Title;
            }
        }

        [NotMapped]
        public string? FormattedAmount
        {
            get
            {
                CultureInfo culture = new CultureInfo("en-US");
                return ((Category == null || Category.Type == "Expense") ? "- " : "+ ") + Amount.ToString("C2", culture);
            }
        }

    }
}
