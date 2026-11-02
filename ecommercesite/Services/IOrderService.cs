using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public interface IOrderService
    {
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
        /// (matched on RegisterId). Powers the customer dashboard's
        /// "Recent Orders" panel.
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
        /// Returns the single most-purchased product per customer (Role ==
        /// Customer). Powers the admin "Max product per customer" report.
        /// </summary>
        Task<IReadOnlyList<TopProductByCustomerDto>> GetTopProductPerCustomerAsync();

        /// <summary>
        /// Returns all products ordered in the supplied calendar year,
        /// sorted descending by quantity. The first row is the most-ordered
        /// product that year. Powers the admin "Max product in year" report.
        /// </summary>
        Task<IReadOnlyList<TopProductInYearDto>> GetTopProductInYearAsync(int year);

        /// <summary>
        /// Returns one row per distinct shipping address with the count of
        /// orders and total revenue at that address. Honors the same date
        /// range as <see cref="GetOrdersAsync"/> so the charts reflect the
        /// currently-filtered order list.
        /// </summary>
        Task<IReadOnlyList<OrderLocationDistributionDto>> GetOrderLocationDistributionAsync(
            DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Returns one row per customer with the count of orders and total
        /// revenue from that customer. Honors the same date range as
        /// <see cref="GetOrdersAsync"/> so the charts reflect the currently-
        /// filtered order list.
        /// </summary>
        Task<IReadOnlyList<OrderCustomerDistributionDto>> GetOrderCustomerDistributionAsync(
            DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Returns one row per (shipping address, customer) pair with the
        /// count of orders at that address from that customer. Powers the
        /// stacked bar chart on the Orders page where each location bar is
        /// segmented by customer. Honors the same date range as
        /// <see cref="GetOrdersAsync"/>.
        /// </summary>
        Task<IReadOnlyList<OrderLocationCustomerBreakdownDto>> GetOrderCustomerBreakdownByLocationAsync(
            DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Returns one row per calendar year that has at least one order,
        /// with the count of orders and total revenue in that year. Powers
        /// the year-wise column chart on the Yearly Report page.
        /// </summary>
        Task<IReadOnlyList<OrderYearlyReportDto>> GetOrderYearlyReportAsync();

        /// <summary>
        /// Creates an order from the supplied view model. Validates that the
        /// customer exists, that every product exists, and snapshots the
        /// product name + price into <see cref="OrderItem"/>s. Returns the
        /// new <see cref="Order.OrderId"/> on success. (Admin flow — picks
        /// a Customer from a dropdown.)
        /// </summary>
        Task<(int OrderId, bool Success, string? Error)> CreateOrderAsync(CreateOrderViewModel model);

        /// <summary>
        /// Creates an order for the currently-logged-in customer. No
        /// customer dropdown — the order is auto-tied to the supplied
        /// <paramref name="registerId"/> (user account). Returns the new
        /// <see cref="Order.OrderId"/> on success. (Customer flow.)
        /// </summary>
        Task<(int OrderId, bool Success, string? Error)> CreateOrderForCustomerAsync(
            string registerId, string? shippingAddress, List<CreateOrderItemViewModel> items);

        Task<bool> DeleteOrderAsync(int orderId);

        Task<(bool Success, string? Error)> UpdateStatusAsync(int orderId, string status);

        /// <summary>
        /// Assigns a delivery person to an order. Sets DeliveryStatus to
        /// "Assigned" and bumps the delivery person's Status to "Busy" if
        /// they were "Available". Pass deliveryPersonId = null to unassign.
        /// </summary>
        Task<(bool Success, string? Error)> AssignDeliveryPersonAsync(int orderId, int? deliveryPersonId);

        /// <summary>
        /// Updates the delivery status of an order. Allowed values: Assigned,
        /// Out for Delivery, Delivered, Failed. When "Delivered" is set,
        /// DeliveredAt is stamped with the current UTC time.
        /// </summary>
        Task<(bool Success, string? Error)> UpdateDeliveryStatusAsync(int orderId, string deliveryStatus);
    }
}
