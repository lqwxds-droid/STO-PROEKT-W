using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Green.Data;
using Green.Models;
using Green.ViewModels;

namespace Green.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Employees = await _userManager.GetUsersInRoleAsync("Employee");
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync();
        var rolesByUser = await (from userRole in _context.UserRoles
                                 join role in _context.Roles on userRole.RoleId equals role.Id
                                 select new { userRole.UserId, role.Name })
            .ToListAsync();
        var roleNamesByUser = rolesByUser
            .GroupBy(userRole => userRole.UserId)
            .ToDictionary(group => group.Key, group => group.Select(userRole => userRole.Name!).ToList());
        var userViewModels = new List<UserListViewModel>(users.Count);
        foreach (var user in users)
        {
            var roles = roleNamesByUser.GetValueOrDefault(user.Id) ?? new List<string>();
            var role = roles.FirstOrDefault() switch
            {
                "Admin" => UserRole.Admin,
                "Employee" => UserRole.Employee,
                _ => UserRole.Client
            };

            userViewModels.Add(new UserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = role,
                IdentityRoles = roles
            });
        }

        var requests = await _context.ServiceRequests
            .Include(r => r.Vehicle)
            .Include(r => r.Client)
            .Include(r => r.AssignedEmployee)
            .Include(r => r.Services)
            .Include(r => r.SelectedServices)
            .Include(r => r.Tasks)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(new AdminDashboardViewModel
        {
            Users = userViewModels,
            Requests = requests
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        var request = await _context.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null) return NotFound();
        if (request.Status is not (RequestStatus.AwaitingApproval or RequestStatus.Planned))
        {
            TempData["Error"] = "Отменить заказ можно только до начала ремонта.";
            return RedirectToAction(nameof(Index));
        }

        request.Status = RequestStatus.Cancelled;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Заказ №{request.Id} отменён.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Services()
    {
        var services = await _context.ServiceCatalogs.ToListAsync();
        return View(services);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, UserRole newRole)
    {
        if (!Enum.IsDefined(newRole))
        {
            TempData["Error"] = "Указана неизвестная роль.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var result = await ReplaceUserRoleAsync(user, newRole);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Роль пользователя {user.Email} изменена.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CreateEmployee() => View(new CreateEmployeeViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, PhoneNumber = model.PhoneNumber, Role = UserRole.Employee };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Employee");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = "Сотрудник создан.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        return View(new EditUserViewModel { Id = user.Id, FullName = user.FullName, Email = user.Email ?? string.Empty, PhoneNumber = user.PhoneNumber, Role = Enum.TryParse<UserRole>(role, out var parsed) ? parsed : UserRole.Client });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!Enum.IsDefined(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Указана неизвестная роль.");
        }
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var roleUpdate = await ReplaceUserRoleAsync(user, model.Role, currentRoles);
        if (!roleUpdate.Succeeded)
        {
            foreach (var error in roleUpdate.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = "Данные пользователя обновлены.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IdentityResult> ReplaceUserRoleAsync(
        ApplicationUser user,
        UserRole role,
        IList<string>? existingRoles = null)
    {
        var targetRole = role.ToString();
        existingRoles ??= await _userManager.GetRolesAsync(user);
        var addedTargetRole = false;

        if (!existingRoles.Contains(targetRole))
        {
            var addResult = await _userManager.AddToRoleAsync(user, targetRole);
            if (!addResult.Succeeded) return addResult;
            addedTargetRole = true;
        }

        var rolesToRemove = existingRoles.Where(existingRole => existingRole != targetRole).ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                if (addedTargetRole) await _userManager.RemoveFromRoleAsync(user, targetRole);
                return removeResult;
            }
        }

        user.Role = role;
        return await _userManager.UpdateAsync(user);
    }

    [HttpGet]
    public IActionResult ClearData()
    {
        // Показываем страницу подтверждения очистки данных
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearDataConfirmed()
    {
        // Удаляем записи в связующей таблице many-to-many, затем сервисы, заявки и автомобили
        try
        {
            // Удаляем связи many-to-many
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"ServiceRequestServiceCatalogs\"");

            // Удаляем конкретные Service записи
            _context.Services.RemoveRange(_context.Services);

            // Удаляем заявки
            _context.ServiceRequests.RemoveRange(_context.ServiceRequests);

            // Удаляем автомобили
            _context.Vehicles.RemoveRange(_context.Vehicles);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Данные по автомобилям и заявкам успешно удалены.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Не удалось очистить данные заказов и автомобилей.");
            TempData["Error"] = "Не удалось удалить данные. Повторите попытку или обратитесь к администратору.";
        }

        return RedirectToAction(nameof(Index));
    }
}
