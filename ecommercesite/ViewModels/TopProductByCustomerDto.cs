namespace ecommercesite.ViewModels
{
    /// <summary>
    /// The single most-purchased product for one customer, plus the total
    /// quantity they've ordered of it. One row per customer.
    /// </summary>
    public class TopProductByCustomerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalSpent { get; set; }

        /// <summary>How many distinct products this customer has ordered in total.</summary>
        public int DistinctProducts { get; set; }
    }

    /// <summary>
    /// One row in the "most-ordered product in a year" report. The product
    /// with the highest TotalQuantity for the chosen year is the headline.
    /// </summary>
    public class TopProductInYearDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }
}