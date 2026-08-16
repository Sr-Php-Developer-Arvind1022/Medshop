using Medshop.Modules.Categories.Domain.Entities;
using Medshop.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Categories.Infrastructure.Seed;

public static class CategorySeeder
{
    public static async Task SeedAsync(MedshopDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var totalExistingCategories = await dbContext.Categories
            .CountAsync(c => !c.IsDeleted, cancellationToken);

        if (totalExistingCategories >= 40)
        {
            return;
        }

        var loginId = await dbContext.Users
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (loginId == Guid.Empty)
        {
            return;
        }

        var categoriesToCreate = new List<Category>();
        var existingNames = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.LoginId == loginId && !c.IsDeleted)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        for (var i = 1; i <= 40; i++)
        {
            var name = $"Category {i}";

            if (existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (categoriesToCreate.Count >= 40)
            {
                break;
            }

            var now = DateTime.UtcNow;
            categoriesToCreate.Add(new Category
            {
                Id = Guid.NewGuid(),
                LoginId = loginId,
                Name = name,
                Description = $"Auto-generated category {i}",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (categoriesToCreate.Count == 0)
        {
            return;
        }

        await dbContext.Categories.AddRangeAsync(categoriesToCreate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
