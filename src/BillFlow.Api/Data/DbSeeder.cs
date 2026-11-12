using BillFlow.Api.Auth;
using BillFlow.Api.Domain;
using BillFlow.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Data;

/// <summary>Seeds the demo admin account and starter plan catalog so a fresh database is immediately usable.</summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@billflow.dev";
    public const string AdminPassword = "Admin#12345";

    public static async Task SeedAsync(BillFlowDbContext db, IPasswordHasher passwordHasher, IClock clock, CancellationToken ct = default)
    {
        if (!await db.Customers.AnyAsync(c => c.Role == CustomerRole.Admin, ct))
        {
            var (hash, salt) = passwordHasher.HashPassword(AdminPassword);
            db.Customers.Add(new Customer
            {
                Name = "BillFlow Admin",
                Email = AdminEmail,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = CustomerRole.Admin,
                CreatedAt = clock.UtcNow,
            });
        }

        if (!await db.Plans.AnyAsync(ct))
        {
            db.Plans.AddRange(
                new Plan { Code = "starter", Name = "Starter", MonthlyPrice = 9.00m, TrialDays = 14, IsActive = true },
                new Plan { Code = "pro", Name = "Pro", MonthlyPrice = 29.00m, TrialDays = 14, IsActive = true },
                new Plan { Code = "enterprise", Name = "Enterprise", MonthlyPrice = 99.00m, TrialDays = 0, IsActive = true });
        }

        await db.SaveChangesAsync(ct);
    }
}
