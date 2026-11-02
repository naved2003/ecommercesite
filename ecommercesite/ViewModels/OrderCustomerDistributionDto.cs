namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One slice in the order-by-customer pie / bar chart.
    /// </summary>
    public class OrderCustomerDistributionDto
    {
        /// <summary>The customer's display name (or "(unknown)" if the
        /// customer record was deleted).</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Number of orders placed by this customer.</summary>
        public int OrderCount { get; set; }

        /// <summary>Total revenue from this customer's orders.</summary>
        public decimal TotalRevenue { get; set; }
    }
}
