using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Repositories
{
    /// <summary>
    /// Order-specific queries on top of the generic CRUD surface.
    /// Order reads always Include <see cref="Customer"/> + <see cref="Order.Items"/>
    /// so the UI can render a full order without N+1.
    /// </summary>
    public interface IOrderRepository : IGenericRepository<Order>
    {
        /// <param name="search">Free-text term matched against order #,
        /// customer ID/name, or status.</param>
        /// <param name="startDate">When set, only orders whose <c>OrderDate</c>
        /// is on or after this date (inclusive) are returned.</param>
        /// <param name="endDate">When set, only orders whose <c>OrderDate</c>
        /// is on or before this date (inclusive of the whole end day) are
        /// returned.</param>
        Task<PagedResult<Order>> GetOrdersAsync(
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize,
            string? registerIdFilter = null);

        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(string custId);

        /// <summary>
        /// Returns the N most recent orders placed by the supplied user
        /// (matched on RegisterId). Powers the customer dashboard.
        /// </summary>
        Task<IReadOnlyList<Order>> GetRecentOrdersForUserAsync(string registerId, int count = 5);

        /// <summary>
        /// Returns one row per line item across the customer's orders,
        /// filtered by an optional date range + optional product-name
        /// search term. Powers the customer's "My Products" page table.
        /// </summary>
        Task<IReadOnlyList<CustomerProductOrderDto>> GetCustomerProductOrdersAsync(
            string registerId,
            DateTime? startDate,
            DateTime? endDate,
            string? productName);

        /// <summary>
        /// Groups the customer's order line items by product name and
        /// returns the total quantity + total spent per product. Powers
        /// the pie chart on the customer's "My Products" page.
        /// </summary>
        Task<IReadOnlyList<CustomerProductDistributionDto>> GetCustomerProductDistributionAsync(
            string registerId,
            DateTime? startDate,
            DateTime? endDate);

        /// <summary>
        /// For each customer (Register with Role == Customer), returns the
        /// single product they've ordered the most of (by total quantity)
        /// across all their orders. Powers the admin "Max product per
        /// customer" report.
        /// </summary>
        Task<IReadOnlyList<TopProductByCustomerDto>> GetTopProductPerCustomerAsync();

        /// <summary>
        /// Returns all products ordered in the supplied calendar year,
        /// with total quantity + total revenue + order count per product,
        /// sorted descending by quantity. The first row is the
        /// most-ordered product that year. Powers the admin
        /// "Max product in year" report.
        /// </summary>
        Task<IReadOnlyList<TopProductInYearDto>> GetTopProductInYearAsync(int year);

        /// <summary>
        /// GROUP BY ShippingAddress, COUNT(*) + SUM(TotalAmount). Returns one
        /// row per distinct shipping address (blank addresses bucketed as
        /// "(no address)"). Powers the pie + bar charts on the Orders page.
        /// </summary>
        Task<IReadOnlyList<OrderLocationDistributionDto>> GetLocationDistributionAsync(
            DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// GROUP BY Customer.CustName, COUNT(*) + SUM(TotalAmount). Returns
        /// one row per customer (deleted customers bucketed as "(unknown)").
        /// Powers the "By Customer" pie + bar charts on the Orders page.
        /// </summary>
        Task<IReadOnlyList<OrderCustomerDistributionDto>> GetCustomerDistributionAsync(
            DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// GROUP BY (ShippingAddress, Customer.CustName), COUNT(*). Returns
        /// one row per (address, customer) pair. Powers the stacked bar chart
        /// on the Orders page where each location bar is segmented by the
        /// customers who placed orders there.
        /// </summary>
        Task<IReadOnlyList<OrderLocationCustomerBreakdownDto>> GetCustomerBreakdownByLocationAsync(
            DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// GROUP BY YEAR(OrderDate), COUNT(*) + SUM(TotalAmount). Returns one
        /// row per calendar year that has at least one order, ordered by year
        /// ascending. Powers the year-wise column chart on the Yearly Report
        /// page.
        /// </summary>
        Task<IReadOnlyList<OrderYearlyReportDto>> GetYearlyReportAsync();
    }
}