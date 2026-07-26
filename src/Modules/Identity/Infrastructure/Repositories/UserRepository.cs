using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Domain.Interfaces;
using Medshop.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MedshopDbContext _context;

    public UserRepository(MedshopDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        => await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task<bool> ExistsByMobileAsync(string mobile, CancellationToken cancellationToken)
        => await _context.Users.AnyAsync(u => u.Mobile == mobile, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task UpdatePasswordAsync(string email, string passwordHash, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        if (user is null)
        {
            return;
        }

        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
