using ecommercesite.Models;
using ecommercesite.Repositories;
using ecommercesite.ViewModels;

namespace ecommercesite.Services
{
    public class DeliveryPersonService(
        IDeliveryPersonRepository deliveryPersonRepository,
        ILogger<DeliveryPersonService> logger) : IDeliveryPersonService
    {
        private readonly IDeliveryPersonRepository _deliveryPersonRepository = deliveryPersonRepository;
        private readonly ILogger<DeliveryPersonService> _logger = logger;

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Available", "Busy", "Offline"
        };

        public async Task<PagedResult<DeliveryPerson>> GetDeliveryPeopleAsync(string? search, int page, int pageSize)
        {
            return await _deliveryPersonRepository.GetDeliveryPeopleAsync(search, page, pageSize);
        }

        public async Task<DeliveryPerson?> GetDeliveryPersonByIdAsync(int id)
        {
            return await _deliveryPersonRepository.GetDeliveryPersonByIdAsync(id);
        }

        public async Task<IReadOnlyList<DeliveryPerson>> GetAvailableDeliveryPeopleAsync()
        {
            return await _deliveryPersonRepository.GetAvailableDeliveryPeopleAsync();
        }

        public async Task<(bool Success, string? Error)> AddDeliveryPersonAsync(DeliveryPerson person)
        {
            if (!ValidStatuses.Contains(person.Status))
            {
                return (false, $"Invalid status '{person.Status}'. Allowed: {string.Join(", ", ValidStatuses)}.");
            }

            if (!await _deliveryPersonRepository.IsPhoneUniqueAsync(person.Phone))
            {
                return (false, "A delivery person with this phone number already exists.");
            }

            person.CreatedAt = DateTime.UtcNow;
            person.IsActive = true;

            await _deliveryPersonRepository.AddAsync(person);
            await _deliveryPersonRepository.SaveAsync();

            _logger.LogInformation("Created delivery person {Name} ({Id})", person.Name, person.Id);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateDeliveryPersonAsync(DeliveryPerson person)
        {
            if (!ValidStatuses.Contains(person.Status))
            {
                return (false, $"Invalid status '{person.Status}'. Allowed: {string.Join(", ", ValidStatuses)}.");
            }

            // Fetch tracked entity to avoid the stub-Update NULL-out bug.
            var existing = await _deliveryPersonRepository.GetByIdAsync(person.Id);
            if (existing is null)
            {
                return (false, "Delivery person not found.");
            }

            if (!await _deliveryPersonRepository.IsPhoneUniqueAsync(person.Phone, excludeId: person.Id))
            {
                return (false, "A delivery person with this phone number already exists.");
            }

            existing.Name = person.Name;
            existing.Phone = person.Phone;
            existing.Email = person.Email;
            existing.VehicleNumber = person.VehicleNumber;
            existing.Status = person.Status;
            existing.AssignedArea = person.AssignedArea;
            existing.IsActive = person.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _deliveryPersonRepository.SaveAsync();

            _logger.LogInformation("Updated delivery person {Id}", person.Id);
            return (true, null);
        }

        public async Task<bool> DeleteDeliveryPersonAsync(int id)
        {
            var stub = new DeliveryPerson { Id = id };
            _deliveryPersonRepository.Delete(stub);
            await _deliveryPersonRepository.SaveAsync();

            _logger.LogInformation("Deleted delivery person {Id}", id);
            return true;
        }
    }
}
