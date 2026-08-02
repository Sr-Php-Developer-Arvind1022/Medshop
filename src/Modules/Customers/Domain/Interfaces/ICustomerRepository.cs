using Medshop.Modules.Customers.Domain.Entities;

namespace Medshop.Modules.Customers.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByMobileAsync(Guid loginId, string mobile, CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(long customerIdPk, Guid loginId, CancellationToken cancellationToken);
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
}
