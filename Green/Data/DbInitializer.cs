using Green.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Green.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }

        // Удаление всех пользователей, кроме основных
        string[] allowedEmails = ["admin@green.com", "master@green.com", "client@green.com"];
        var usersToDelete = await context.Users
            .Where(u => !allowedEmails.Contains(u.Email))
            .ToListAsync();

        foreach (var userToDelete in usersToDelete)
        {
            var result = await userManager.DeleteAsync(userToDelete);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Не удалось удалить пользователя {userToDelete.Email}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }

        string[] roles = ["Admin", "Employee", "Client"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Не удалось создать роль {role}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
                }
            }
        }

        var adminPassword = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Задайте InitialAdmin:Password через секреты или переменные окружения перед запуском.");
        }

        var seedUsers = new (string Email, string FullName, string Role, UserRole UserRole)[]
        {
            ("admin@green.com", "Администратор", "Admin", UserRole.Admin),
            ("master@green.com", "Мастер", "Employee", UserRole.Employee),
            ("client@green.com", "Клиент", "Client", UserRole.Client)
        };

        foreach (var seedUser in seedUsers)
        {
            await EnsureSeedUserAsync(userManager, seedUser.Email, seedUser.FullName, seedUser.Role, seedUser.UserRole, adminPassword);
        }

        // 3. Заполнение базового каталога услуг
        if (!await context.ServiceCatalogs.AnyAsync())
        {
            context.ServiceCatalogs.AddRange(
                new ServiceCatalog { Name = "Компьютерная диагностика", Description = "Полное сканирование электронных систем автомобиля", Price = 1500 },
                new ServiceCatalog { Name = "Замена моторного масла и фильтра", Description = "Замена масла, масляного и воздушного фильтров", Price = 2500 },
                new ServiceCatalog { Name = "Обслуживание тормозной системы", Description = "Замена передних и задних тормозных колодок", Price = 4000 },
                new ServiceCatalog { Name = "Заправка и диагностика кондиционера", Description = "Проверка герметичности и заправка фреоном", Price = 3000 }
            );

            await context.SaveChangesAsync();
        }

        var basicServices = new ServiceCatalog[]
        {
            new() { Name = "Замена колёс", Description = "Снятие и установка колёс автомобиля", Price = 0 },
            new() { Name = "Осмотр ТО", Description = "Базовый осмотр автомобиля перед техническим обслуживанием", Price = 0 },
            new() { Name = "Осмотр электриком", Description = "Проверка электрооборудования автомобиля", Price = 0 },
            new() { Name = "Замена масла", Description = "Замена моторного масла; стоимость уточняется после осмотра", Price = 0 }
        };

        var basicServiceNames = basicServices.Select(service => service.Name).ToArray();
        var existingBasicServices = await context.ServiceCatalogs
            .Where(service => basicServiceNames.Contains(service.Name))
            .ToListAsync();
        var missingBasicServices = new List<ServiceCatalog>();
        foreach (var basicService in basicServices)
        {
            var existingService = existingBasicServices.FirstOrDefault(service =>
                string.Equals(service.Name, basicService.Name, StringComparison.OrdinalIgnoreCase));

            if (existingService == null)
                missingBasicServices.Add(basicService);
        }

        if (missingBasicServices.Count > 0)
        {
            context.ServiceCatalogs.AddRange(missingBasicServices);
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureSeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string role,
        UserRole userRole,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Role = userRole
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Не удалось создать пользователя {email}: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }
        else
        {
            user.Role = userRole;
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = fullName;
            }

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Не удалось обновить пользователя {email}: {string.Join(", ", updateResult.Errors.Select(error => error.Description))}");
            }

            var passwordCheck = user.PasswordHash == null
                ? PasswordVerificationResult.Failed
                : userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (passwordCheck == PasswordVerificationResult.Failed)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await userManager.ResetPasswordAsync(user, resetToken, password);
                if (!passwordResult.Succeeded)
                {
                    throw new InvalidOperationException($"Не удалось задать общий пароль для {email}: {string.Join(", ", passwordResult.Errors.Select(error => error.Description))}");
                }
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Where(currentRole => currentRole != role).ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException($"Не удалось обновить роли пользователя {email}: {string.Join(", ", removeResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var addResult = await userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException($"Не удалось назначить роль {role} пользователю {email}: {string.Join(", ", addResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}