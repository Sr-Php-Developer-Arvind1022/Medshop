using AutoMapper;
using Medshop.Modules.Customers.Application.DTOs.Response;
using Medshop.Modules.Customers.Application.Interfaces;
using Medshop.Modules.Customers.Domain.Interfaces;

namespace Medshop.Modules.Customers.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public CustomerService(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<CustomerResponse?> SearchByMobileAsync(string mobile, Guid loginId, CancellationToken cancellationToken)
    {
        var normalizedMobile = mobile.Trim();
        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            return null;
        }

        var customer = await _customerRepository.GetByMobileAsync(loginId, normalizedMobile, cancellationToken);
        return customer is null ? null : _mapper.Map<CustomerResponse>(customer);
    }
}
