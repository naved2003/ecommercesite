namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One slice in the customer-address pie chart.
    /// </summary>
    public class CustomerAddressDistributionDto
    {
        /// <summary>The distinct address (or "(no address)" if blank).</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Number of customers at this address.</summary>
        public int CustomerCount { get; set; }
    }
}