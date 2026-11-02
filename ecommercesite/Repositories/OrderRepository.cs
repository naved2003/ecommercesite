using ecommercesite.Data;
using ecommercesite.Models;
using ecommercesite.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ecommercesite.Repositories
{
    public class OrderRepository(ApplicationDbContext context) : GenericRepository<Order>(context), IOrderRepository
    {
        public async Task<PagedResult<Order>> GetOrdersAsync(
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize,
            string? registerIdFilter = null)
        {
            page = Math.Max(1, page);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var term = search?.Trim();
            IQueryable<Order> query = _context.Orders
                                              .AsNoTracking()
                                              .Include(o => o.Customer)
                                              .Include(o => o.Register)
                                              .Include(o => o.Items);

            // Customer-scoped filter: when set, only return orders placed by
            // this Register (user account). Used by the customer-facing My
            // Orders page.
            if (!string.IsNullOrEmpty(registerIdFilter))
            {
                query = query.Where(o => o.RegisterId == registerIdFilter);
            }

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(term) ||
                    (o.Customer != null && o.Customer.CustName.Contains(term)) ||
                    (o.Customer != null && o.Customer.CustId.Contains(term)) ||
                    (o.Register != null && (o.Register.FirstName + " " + o.Register.LastName).Contains(term)) ||
                    o.Status.Contains(term));
            }

            // Date-range filter (inclusive on both ends).
            if (startDate.HasValue)
            {
                var from = startDate.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }
            if (endDate.HasValue)
            {
                var toExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Order>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                                 .AsNoTracking()
                                 .Include(o => o.Customer)
                                 .Include(o => o.Register)
                                 .Include(o => o.DeliveryPerson)
                                 .Include(o => o.Items)
                                    .ThenInclude(i => i.Product)
                                 .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(string custId)
        {
            // Match either CustId (legacy admin-created orders) or
            // RegisterId (customer-placed orders tied to a user account).
            return await _context.Orders
                                 .AsNoTracking()
                                 .Include(o => o.Items)
                                 .Where(o => o.CustId == custId || o.RegisterId == custId)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetRecentOrdersForUserAsync(string registerId, int count = 5)
        {
            return await _context.Orders
                                 .AsNoTracking()
                                 .Include(o => o.Items)
                                 .Where(o => o.RegisterId == registerId)
                                 .OrderByDescending(o => o.OrderDate)
                                 .Take(count)
                                 .ToListAsync();
        }

        public async Task<IReadOnlyList<CustomerProductOrderDto>> GetCustomerProductOrdersAsync(
            string registerId,
            DateTime? startDate,
            DateTime? endDate,
            string? productName)
        {
            // Build the base query: this customer's orders + their line items.
            var query = from o in _context.Orders.AsNoTracking()
                        join i in _context.OrderItems.AsNoTracking()
                          on o.OrderId equals i.OrderId
                        where o.RegisterId == registerId
                        select new { o, i };

            // Apply date-range filter (inclusive on both ends, like the
            // admin Orders page).
            if (startDate.HasValue)
            {
                var from = startDate.Value.Date;
                query = query.Where(x => x.o.OrderDate >= from);
            }
            if (endDate.HasValue)
            {
                var toExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.o.OrderDate < toExclusive);
            }

            // Apply optional product-name search (case-insensitive contains).
            var term = productName?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(x => x.i.ProductName.Contains(term));
            }

            var rows = await query
                .OrderByDescending(x => x.o.OrderDate)
                .ThenBy(x => x.i.ProductName)
                .Select(x => new CustomerProductOrderDto
                {
                    OrderId = x.o.OrderId,
                    OrderDate = x.o.OrderDate,
                    Status = x.o.Status,
                    ProductId = x.i.ProductId,
                    ProductName = x.i.ProductName,
                    Quantity = x.i.Quantity,
                    UnitPrice = x.i.UnitPrice
                })
                .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<CustomerProductDistributionDto>> GetCustomerProductDistributionAsync(
            string registerId,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = from o in _context.Orders.AsNoTracking()
                        join i in _context.OrderItems.AsNoTracking()
                          on o.OrderId equals i.OrderId
                        where o.RegisterId == registerId
                        select new { o, i };

            if (startDate.HasValue)
            {
                var from = startDate.Value.Date;
                query = query.Where(x => x.o.OrderDate >= from);
            }
            if (endDate.HasValue)
            {
                var toExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.o.OrderDate < toExclusive);
            }

            // Group by product name → total quantity + total spent per product.
            var rows = await query
                .GroupBy(x => x.i.ProductName)
                .Select(g => new CustomerProductDistributionDto
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(x => x.i.Quantity),
                    TotalSpent = g.Sum(x => x.i.UnitPrice * x.i.Quantity)
                })
                .OrderByDescending(r => r.TotalQuantity)
                .ThenBy(r => r.ProductName)
                .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<TopProductByCustomerDto>> GetTopProductPerCustomerAsync()
        {
            // For each customer, find the product they've ordered the most of
            // (by total quantity). Implemented as: group line items by
            // (UserId, ProductName), then per user pick the row with the
            // max quantity.
            var rows = await (
                from o in _context.Orders.AsNoTracking()
                join i in _context.OrderItems.AsNoTracking() on o.OrderId equals i.OrderId
                join u in _context.Registers.AsNoTracking() on o.RegisterId equals u.UserId
                where u.Role == "Customer"
                group new { i, u } by new { u.UserId, u.FirstName, u.LastName, u.Email, i.ProductName } into g
                select new
                {
                    g.Key.UserId,
                    g.Key.FirstName,
                    g.Key.LastName,
                    g.Key.Email,
                    g.Key.ProductName,
                    TotalQuantity = g.Sum(x => x.i.Quantity),
                    TotalSpent = g.Sum(x => x.i.UnitPrice * x.i.Quantity)
                }
            ).ToListAsync();

            // Per customer, keep only the product with the highest quantity,
            // and count how many distinct products they've ordered overall.
            var result = rows
                .GroupBy(r => r.UserId)
                .Select(g => new
                {
                    // Top product for this customer = max by TotalQuantity,
                    // tie-break on TotalSpent, then ProductName.
                    Top = g.OrderByDescending(x => x.TotalQuantity)
                           .ThenByDescending(x => x.TotalSpent)
                           .ThenBy(x => x.ProductName)
                           .First(),
                    DistinctProducts = g.Count()
                })
                .OrderByDescending(x => x.Top.TotalQuantity)
                .Select(x => new TopProductByCustomerDto
                {
                    UserId = x.Top.UserId,
                    CustomerName = $"{x.Top.FirstName} {x.Top.LastName}".Trim(),
                    Email = x.Top.Email,
                    ProductName = x.Top.ProductName,
                    TotalQuantity = x.Top.TotalQuantity,
                    TotalSpent = x.Top.TotalSpent,
                    DistinctProducts = x.DistinctProducts
                })
                .ToList();

            return result;
        }

        public async Task<IReadOnlyList<TopProductInYearDto>> GetTopProductInYearAsync(int year)
        {
            // Group all line items in the chosen year by product name.
            var rows = await (
                from o in _context.Orders.AsNoTracking()
                join i in _context.OrderItems.AsNoTracking() on o.OrderId equals i.OrderId
                where o.OrderDate.Year == year
                group i by i.ProductName into g
                select new TopProductInYearDto
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity),
                    OrderCount = g.Select(x => x.OrderId).Distinct().Count()
                }
            )
            .OrderByDescending(r => r.TotalQuantity)
            .ThenByDescending(r => r.TotalRevenue)
            .ThenBy(r => r.ProductName)
            .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<OrderLocationDistributionDto>> GetLocationDistributionAsync(
            DateTime? startDate, DateTime? endDate)
        {
            IQueryable<Order> query = _context.Orders.AsNoTracking();

            // Apply the same date-range filter as the main list so the
            // charts reflect what the user is currently viewing.
            if (startDate.HasValue)
            {
                var from = startDate.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }
            if (endDate.HasValue)
            {
                var toExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            var rows = await query
                .GroupBy(o => string.IsNullOrWhiteSpace(o.ShippingAddress)
                              ? "(no address)"
                              : o.ShippingAddress.Trim())
                .Select(g => new OrderLocationDistributionDto
                {
                    Address = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.OrderCount)
                .ThenBy(r => r.Address)
                .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<OrderCustomerDistributionDto>> GetCustomerDistributionAsync(
            DateTime? startDate, DateTime? endDate)
        {
            IQueryable<Order> query = _context.Orders
                                              .AsNoTracking()
                                              .Include(o => o.Customer);

            // Apply the same date-range filter as the main list so the
            // charts reflect what the user is currently viewing.
            if (startDate.HasValue)
            {
                var from = startDate.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }
            if (endDate.HasValue)
            {
                var toExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            var rows = await query
                .GroupBy(o => o.Customer != null && !string.IsNullOrWhiteSpace(o.Customer.CustName)
                              ? o.Customer.CustName.Trim()
                              : "(unknown)")
                .Select(g => new OrderCustomerDistributionDto
                {
                    CustomerName = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.OrderCount)
                .ThenBy(r => r.CustomerName)
                .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<OrderLocationCustomerBreakdownDto>> GetCustomerBreakdownByLocationAsync(
            DateTime? startDate, DateTime? endDate)
        {
            IQueryable<Order> query = _context.Orders
                                              .AsNoTracking()
                                              .Include(o => o.Customer);

            // Apply the same date-range filter as the main list so the
            // stacked bar chart reflects what the user is currently viewing.
            if (startDate.HasValue)
            {
                var from = startDate.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }
            if (endDate.HasValue)
            {
                var toExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            // Group by BOTH address and customer name so the stacked bar
            // can build one dataset per customer with a 1-D array indexed
            // by the address list.
            var rows = await query
                .GroupBy(o => new
                {
                    Address = string.IsNullOrWhiteSpace(o.ShippingAddress)
                              ? "(no address)"
                              : o.ShippingAddress.Trim(),
                    CustomerName = o.Customer != null && !string.IsNullOrWhiteSpace(o.Customer.CustName)
                              ? o.Customer.CustName.Trim()
                              : "(unknown)"
                })
                .Select(g => new OrderLocationCustomerBreakdownDto
                {
                    Address = g.Key.Address,
                    CustomerName = g.Key.CustomerName,
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Address)
                .ThenByDescending(r => r.OrderCount)
                .ThenBy(r => r.CustomerName)
                .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<OrderYearlyReportDto>> GetYearlyReportAsync()
        {
            // GROUP BY YEAR(OrderDate), COUNT(*) + SUM(TotalAmount).
            // Materialize year as a small int projection so SQL Server does
            // the grouping, then map to the DTO in memory.
            var rows = await _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            return [.. rows
                .Select(x => new OrderYearlyReportDto
                {
                    Year = x.Year,
                    OrderCount = x.OrderCount,
                    TotalRevenue = x.TotalRevenue
                })];
        }
    }
}
