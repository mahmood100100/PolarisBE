using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Polaris.Domain.Entities;
using Polaris.Infrastructure.Identity;

namespace Polaris.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await SeedRoles(roleManager);
            await SeedAdminUser(userManager, context);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
        {
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            const string adminEmail = "admin@polaris.com";

            // Check if admin already exists
            if (await userManager.FindByEmailAsync(adminEmail) != null)
                return;

            // Create ApplicationUser (Identity)
            var adminId = Guid.NewGuid();
            var appUser = new ApplicationUser
            {
                Id = adminId,
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(appUser, "Admin@123456");

            if (result.Succeeded)
            {
                // Add to Admin role
                await userManager.AddToRoleAsync(appUser, "Admin");

                // Create LocalUser profile
                var localUser = new LocalUser
                {
                    Id = adminId,
                    FullName = "System Administrator",
                    UserName = "admin",
                    Email = adminEmail,
                    ImageUrl = "https://ui-avatars.com/api/?name=Admin&background=0D8F81&color=fff&size=128",
                    CreatedAt = DateTime.UtcNow,
                };

                await context.Set<LocalUser>().AddAsync(localUser);
                await context.SaveChangesAsync();
            }
        }
    }
}