namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One slice in the order-location pie / bar chart.
    /// </summary>
    public class OrderLocationDistributionDto
    {
        /// <summary>The distinct shipping address (or "(no address)" if blank).</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Number of orders shipped to this address.</summary>
        public int OrderCount { get; set; }

        /// <summary>Total revenue from orders at this address.</summary>
        public decimal TotalRevenue { get; set; }
    }
}