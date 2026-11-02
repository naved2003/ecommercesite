using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public interface IDeliveryPersonService
    {
        Task<PagedResult<DeliveryPerson>> GetDeliveryPeopleAsync(string? search, int page, int pageSize);
        Task<DeliveryPerson?> GetDeliveryPersonByIdAsync(int id);
        Task<IReadOnlyList<DeliveryPerson>> GetAvailableDeliveryPeopleAsync();
        Task<(bool Success, string? Error)> AddDeliveryPersonAsync(DeliveryPerson person);
        Task<(bool Success, string? Error)> UpdateDeliveryPersonAsync(DeliveryPerson person);
        Task<bool> DeleteDeliveryPersonAsync(int id);
    }
}
