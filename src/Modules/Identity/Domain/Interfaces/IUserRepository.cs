namespace Medshop.Modules.Identity.Domain.Interfaces;

using Medshop.Modules.Identity.Domain.Entities;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByMobileAsync(string mobile, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task UpdatePasswordAsync(string email, string passwordHash, CancellationToken cancellationToken);
}
