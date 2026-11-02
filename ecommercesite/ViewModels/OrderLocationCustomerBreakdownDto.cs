namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One row in the "Orders by Location, broken down by Customer" matrix.
    /// Powers the stacked bar chart on the Orders page where each address
    /// bar is segmented by customer.
    /// </summary>
    public class OrderLocationCustomerBreakdownDto
    {
        /// <summary>The shipping address (or "(no address)" if blank).</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>The customer name (or "(unknown)" if the customer record was deleted).</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Number of orders at this address from this customer.</summary>
        public int OrderCount { get; set; }
    }
}
