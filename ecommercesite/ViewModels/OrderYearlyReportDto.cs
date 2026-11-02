namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One row in the year-wise order report. Powers the column chart on
    /// the Yearly Report page.
    /// </summary>
    public class OrderYearlyReportDto
    {
        /// <summary>Calendar year (e.g. 2026) of the orders in this row.</summary>
        public int Year { get; set; }

        /// <summary>Number of orders placed in this year.</summary>
        public int OrderCount { get; set; }

        /// <summary>Total revenue from orders in this year.</summary>
        public decimal TotalRevenue { get; set; }
    }
}
