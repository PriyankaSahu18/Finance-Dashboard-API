namespace FinanceDashboard.Models
{
    public class FinancialRecord
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }

        public int UserId { get; set; }
    }
}