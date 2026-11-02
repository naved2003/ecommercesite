using ecommercesite.Models;

namespace ecommercesite.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetLatestPaymentForOrderAsync(int orderId);
        Task<IReadOnlyList<Payment>> GetPaymentsForOrderAsync(int orderId);
        Task<bool> HasSuccessfulPaymentAsync(int orderId);
    }
}