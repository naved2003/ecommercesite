using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ecommercesite.Services
{
    public class OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IDeliveryPersonRepository deliveryPersonRepository,
        IRegisterRepository registerRepository,
        ILogger<OrderService> logger) : IOrderService
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IDeliveryPersonRepository _deliveryPersonRepository = deliveryPersonRepository;
        private readonly IRegisterRepository _registerRepository = registerRepository;
        private readonly ILogger<OrderService> _logger = logger;

        // Allowed status transitions (kept simple for an educational app).
        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"
        };

        // Allowed delivery-status values.
        private static readonly HashSet<string> ValidDeliveryStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Assigned", "Out for Delivery", "Delivered", "Failed"
        };

        public async Task<PagedResult<Order>> GetOrdersAsync(
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize,
            string? registerIdFilter = null)
        {
            return await _orderRepository.GetOrdersAsync(search, startDate, endDate, page, pageSize, registerIdFilter);
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _orderRepository.GetOrderByIdAsync(orderId);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(string custId)
        {
            return await _orderRepository.GetOrdersByCustomerAsync(custId);
        }

        public async Task<IReadOnlyList<Order>> GetRecentOrdersForUserAsync(string registerId, int count = 5)
        {
            if (string.IsNullOrEmpty(registerId))
            {
                return [];
            }

            return await _orderRepository.GetRecentOrdersForUserAsync(registerId, count);
        }

        public async Task<IReadOnlyList<CustomerProductOrderDto>> GetCustomerProductOrdersAsync(
            string registerId,
            DateTime? startDate,
            DateTime? endDate,
            string? productName)
        {
            if (string.IsNullOrEmpty(registerId))
            {
                return [];
            }

            return await _orderRepository.GetCustomerProductOrdersAsync(
                registerId, startDate, endDate, productName);
        }

        public async Task<IReadOnlyList<CustomerProductDistributionDto>> GetCustomerProductDistributionAsync(
            string registerId,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (string.IsNullOrEmpty(registerId))
            {
                return [];
            }

            return await _orderRepository.GetCustomerProductDistributionAsync(
                registerId, startDate, endDate);
        }

        public async Task<IReadOnlyList<TopProductByCustomerDto>> GetTopProductPerCustomerAsync()
        {
            return await _orderRepository.GetTopProductPerCustomerAsync();
        }

        public async Task<IReadOnlyList<TopProductInYearDto>> GetTopProductInYearAsync(int year)
        {
            return await _orderRepository.GetTopProductInYearAsync(year);
        }

        public async Task<IReadOnlyList<OrderLocationDistributionDto>> GetOrderLocationDistributionAsync(
            DateTime? startDate, DateTime? endDate)
        {
            return await _orderRepository.GetLocationDistributionAsync(startDate, endDate);
        }

        public async Task<IReadOnlyList<OrderCustomerDistributionDto>> GetOrderCustomerDistributionAsync(
            DateTime? startDate, DateTime? endDate)
        {
            return await _orderRepository.GetCustomerDistributionAsync(startDate, endDate);
        }

        public async Task<IReadOnlyList<OrderLocationCustomerBreakdownDto>> GetOrderCustomerBreakdownByLocationAsync(
            DateTime? startDate, DateTime? endDate)
        {
            return await _orderRepository.GetCustomerBreakdownByLocationAsync(startDate, endDate);
        }

        public async Task<IReadOnlyList<OrderYearlyReportDto>> GetOrderYearlyReportAsync()
        {
            return await _orderRepository.GetYearlyReportAsync();
        }

        public async Task<(int OrderId, bool Success, string? Error)> CreateOrderAsync(CreateOrderViewModel model)
        {
            // ---- Validate customer ----------------------------------------
            var customer = await _customerRepository.GetCustomerByIdAsync(custId: model.CustId);
            if (customer is null)
            {
                return (0, false, "Selected customer does not exist.");
            }

            // ---- De-duplicate product picks (merge same product) ----------
            var dedupedItems = model.Items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            if (dedupedItems.Count == 0)
            {
                return (0, false, "Add at least one product with quantity > 0.");
            }

            // ---- Fetch all products in one round-trip ---------------------
            var productIds = dedupedItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _productRepository.FindAsync(p => productIds.Contains(p.ProductId));
            var productById = products.ToDictionary(p => p.ProductId);

            foreach (var item in dedupedItems)
            {
                if (!productById.ContainsKey(item.ProductId))
                {
                    return (0, false, $"Product with ID {item.ProductId} does not exist.");
                }
            }

            // ---- Build the order -----------------------------------------
            var order = new Order
            {
                CustId = customer.CustId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                ShippingAddress = string.IsNullOrWhiteSpace(model.ShippingAddress)
                    ? customer.CustAddress
                    : model.ShippingAddress.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            decimal total = 0m;
            foreach (var item in dedupedItems)
            {
                var product = productById[item.ProductId];
                var line = new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity
                };
                order.Items.Add(line);
                total += line.LineTotal;
            }
            order.TotalAmount = total;

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveAsync();

            _logger.LogInformation("Created order {OrderId} for customer {CustId} with {ItemCount} items, total {Total:C}",
                order.OrderId, order.CustId, order.Items.Count, order.TotalAmount);

            return (order.OrderId, true, null);
        }

        public async Task<(int OrderId, bool Success, string? Error)> CreateOrderForCustomerAsync(
            string registerId, string? shippingAddress, List<CreateOrderItemViewModel> items)
        {
            if (string.IsNullOrWhiteSpace(registerId))
            {
                return (0, false, "You must be logged in to place an order.");
            }

            // Validate the user account exists.
            var user = await _registerRepository.GetRegisterByIdAsync(registerId);
            if (user is null)
            {
                return (0, false, "User account not found.");
            }

            // De-duplicate + validate line items.
            var dedupedItems = items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            if (dedupedItems.Count == 0)
            {
                return (0, false, "Add at least one product with quantity > 0.");
            }

            var productIds = dedupedItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _productRepository.FindAsync(p => productIds.Contains(p.ProductId));
            var productById = products.ToDictionary(p => p.ProductId);

            foreach (var item in dedupedItems)
            {
                if (!productById.ContainsKey(item.ProductId))
                {
                    return (0, false, $"Product with ID {item.ProductId} does not exist.");
                }
            }

            // Build the order — tied to the Register (user account), not a
            // Customer record. CustId is explicitly null (the column is
            // nullable; if we left it as the default empty string the FK
            // constraint would reject the insert because no Customer has
            // CustId == "").
            var order = new Order
            {
                CustId = null,
                RegisterId = user.UserId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                ShippingAddress = string.IsNullOrWhiteSpace(shippingAddress)
                    ? null
                    : shippingAddress.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            decimal total = 0m;
            foreach (var item in dedupedItems)
            {
                var product = productById[item.ProductId];
                var line = new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity
                };
                order.Items.Add(line);
                total += line.LineTotal;
            }
            order.TotalAmount = total;

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveAsync();

            // ---- Auto-assign a delivery person ----------------------------
            // Customers do NOT choose a delivery person — the system
            // automatically picks the first Available + active delivery
            // person. If none are available, the order is created without
            // an assignment and the admin can assign one later.
            var availableDrivers = await _deliveryPersonRepository.GetAvailableDeliveryPeopleAsync();
            if (availableDrivers.Count > 0)
            {
                var driver = availableDrivers[0];
                order.DeliveryPersonId = driver.Id;
                order.DeliveryStatus = "Assigned";
                await _orderRepository.SaveAsync();

                _logger.LogInformation(
                    "Auto-assigned delivery person {DriverName} ({DriverId}) to order {OrderId}",
                    driver.Name, driver.Id, order.OrderId);
            }
            else
            {
                _logger.LogWarning(
                    "No available delivery person to auto-assign to order {OrderId}. Admin must assign manually.",
                    order.OrderId);
            }

            _logger.LogInformation("Customer {Email} placed order {OrderId} with {ItemCount} items, total {Total:C}",
                user.Email, order.OrderId, order.Items.Count, order.TotalAmount);

            return (order.OrderId, true, null);
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var stub = new Order { OrderId = orderId };
            _orderRepository.Delete(stub);
            await _orderRepository.SaveAsync();

            _logger.LogInformation("Deleted order {OrderId}", orderId);
            return true;
        }

        public async Task<(bool Success, string? Error)> UpdateStatusAsync(int orderId, string status)
        {
            if (!ValidStatuses.Contains(status))
            {
                return (false, $"Invalid status '{status}'. Allowed: {string.Join(", ", ValidStatuses)}.");
            }

            // Fetch the existing row as a TRACKED entity. Using a stub +
            // Update() would mark every column Modified and try to write
            // NULL for CustId, violating the FK_Orders_Customers_CustId
            // constraint. Modifying a tracked entity's property only marks
            // that one column Modified.
            var existing = await _orderRepository.GetByIdAsync(orderId);
            if (existing is null)
            {
                return (false, "Order not found.");
            }

            existing.Status = status;
            await _orderRepository.SaveAsync();

            _logger.LogInformation("Updated order {OrderId} status -> {Status}", orderId, status);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> AssignDeliveryPersonAsync(int orderId, int? deliveryPersonId)
        {
            // GetByIdAsync returns a tracked entity (no AsNoTracking), which
            // is what we need for the assignment mutation to stick.
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                return (false, "Order not found.");
            }

            // Unassign case: clear delivery person + reset delivery status.
            if (deliveryPersonId is null or 0)
            {
                order.DeliveryPersonId = null;
                order.DeliveryStatus = null;
                order.DeliveredAt = null;
                await _orderRepository.SaveAsync();

                _logger.LogInformation("Unassigned delivery person from order {OrderId}", orderId);
                return (true, null);
            }

            // Validate the chosen delivery person exists + is active.
            var person = await _deliveryPersonRepository.GetByIdAsync(deliveryPersonId.Value);
            if (person is null)
            {
                return (false, "Selected delivery person does not exist.");
            }
            if (!person.IsActive)
            {
                return (false, "Selected delivery person is inactive.");
            }

            order.DeliveryPersonId = deliveryPersonId;
            order.DeliveryStatus = "Assigned";
            order.DeliveredAt = null;
            await _orderRepository.SaveAsync();

            _logger.LogInformation("Assigned delivery person {PersonId} ({PersonName}) to order {OrderId}",
                person.Id, person.Name, orderId);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateDeliveryStatusAsync(int orderId, string deliveryStatus)
        {
            if (!ValidDeliveryStatuses.Contains(deliveryStatus))
            {
                return (false, $"Invalid delivery status '{deliveryStatus}'. Allowed: {string.Join(", ", ValidDeliveryStatuses)}.");
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                return (false, "Order not found.");
            }

            if (order.DeliveryPersonId is null)
            {
                return (false, "Assign a delivery person before updating the delivery status.");
            }

            order.DeliveryStatus = deliveryStatus;

            // Stamp the delivered timestamp + auto-promote the order status.
            if (deliveryStatus == "Delivered")
            {
                order.DeliveredAt = DateTime.UtcNow;
                if (order.Status != "Delivered")
                {
                    order.Status = "Delivered";
                }
            }
            else if (deliveryStatus == "Out for Delivery" && order.Status == "Pending")
            {
                // Auto-confirm the order when it goes out for delivery.
                order.Status = "Confirmed";
            }

            await _orderRepository.SaveAsync();

            _logger.LogInformation("Updated order {OrderId} delivery status -> {DeliveryStatus}",
                orderId, deliveryStatus);
            return (true, null);
        }
    }
}
