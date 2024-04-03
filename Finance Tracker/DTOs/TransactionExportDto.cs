namespace Finance_Tracker.DTOs
{
    public class TransactionExportDto
    {
        public DateTime Date { get; set; }
        public string Note { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public string Amount { get; set; }
    }

}
