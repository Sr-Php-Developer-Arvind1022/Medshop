using Medshop.Modules.Customers.Domain.Entities;
using Medshop.Modules.Customers.Domain.Interfaces;
using Medshop.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Customers.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly MedshopDbContext _context;

    public CustomerRepository(MedshopDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByMobileAsync(Guid loginId, string mobile, CancellationToken cancellationToken)
        => await _context.Customers.FirstOrDefaultAsync(c => c.LoginId == loginId && c.Mobile == mobile, cancellationToken);

    public async Task<Customer?> GetByIdAsync(long customerIdPk, Guid loginId, CancellationToken cancellationToken)
        => await _context.Customers.FirstOrDefaultAsync(c => c.CustomerIdPk == customerIdPk && c.LoginId == loginId, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
