using ecommercesite.Models;
using ecommercesite.ViewModels;

namespace ecommercesite.Repositories
{
    public interface IDeliveryPersonRepository : IGenericRepository<DeliveryPerson>
    {
        Task<PagedResult<DeliveryPerson>> GetDeliveryPeopleAsync(string? search, int page, int pageSize);
        Task<DeliveryPerson?> GetDeliveryPersonByIdAsync(int id);
        Task<DeliveryPerson?> GetDeliveryPersonByPhoneAsync(string phone);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null);
        Task<IReadOnlyList<DeliveryPerson>> GetAvailableDeliveryPeopleAsync();
    }
}
