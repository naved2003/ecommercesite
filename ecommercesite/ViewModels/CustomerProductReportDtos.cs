namespace ecommercesite.ViewModels
{
    /// <summary>
    /// One row in the customer's "order details by product" table. Each row
    /// represents one line item from one of the customer's orders.
    /// </summary>
    public class CustomerProductOrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    /// <summary>
    /// One slice in the customer's "products ordered" pie chart.
    /// </summary>
    public class CustomerProductDistributionDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalSpent { get; set; }
    }
}