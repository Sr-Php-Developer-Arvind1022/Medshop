using Medshop.Modules.Customers.Application.DTOs.Response;

namespace Medshop.Modules.Customers.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse?> SearchByMobileAsync(string mobile, Guid loginId, CancellationToken cancellationToken);
}
