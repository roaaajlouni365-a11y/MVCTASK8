using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Models;

namespace CafeteriaSupplyTask8.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var role in AppRoles.All)
            if (!await roles.RoleExistsAsync(role)) Check(await roles.CreateAsync(new IdentityRole(role)));

        // Bootstrap only once, never elevate a previously self-registered account.
        if ((await users.GetUsersInRoleAsync(AppRoles.SuperAdmin)).Count == 0)
        {
            var email = configuration["Bootstrap:Email"]?.Trim();
            var password = configuration["Bootstrap:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("First run: set Bootstrap:Email and Bootstrap:Password using Manage User Secrets. See README-AR.md.");
            if (await users.FindByEmailAsync(email) != null)
                throw new InvalidOperationException("Choose a NEW email for Bootstrap:Email. Existing accounts are never auto-promoted.");
            await using var transaction = await db.Database.BeginTransactionAsync();
            var admin = new ApplicationUser
            {
                UserName = email, Email = email,
                FullName = configuration["Bootstrap:FullName"] ?? "Cafeteria Administrator"
            };
            Check(await users.CreateAsync(admin, password));
            Check(await users.AddToRoleAsync(admin, AppRoles.SuperAdmin));
            await transaction.CommitAsync();
        }

        // Seed demo products only for a brand-new catalogue.
        if (!await db.Products.AnyAsync() && !await db.SupplyRequests.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Name = "Coffee", Description = "Coffee pack", Price = 5.50m, Quantity = 100 },
                new Product { Name = "Milk", Description = "One litre", Price = 1.50m, Quantity = 150 },
                new Product { Name = "Juice", Description = "Individual juice carton", Price = 0.75m, Quantity = 200 },
                new Product { Name = "Sandwich", Description = "Fresh sandwich", Price = 2.00m, Quantity = 80 },
                new Product { Name = "Bottled Water", Description = "500 ml bottle", Price = 0.50m, Quantity = 300 },
                new Product { Name = "Packaging Materials", Description = "Pack of food containers", Price = 3.00m, Quantity = 60 });
            await db.SaveChangesAsync();
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
