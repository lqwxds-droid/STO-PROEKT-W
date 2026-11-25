using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Green.Infrastructure;
using System.Globalization;

namespace Green.Controllers;

[Authorize]
public class ClientController : Controller
{
    private static readonly string[] BasicBookingServiceNames =
    [
        "Замена колёс",
        "Осмотр ТО",
        "Осмотр электриком",
        "Замена масла"
    ];

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClientController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Главный кабинет клиента
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var requests = await _context.ServiceRequests
            .Include(r => r.Vehicle)
            .Include(r => r.AssignedEmployee)
            .Include(r => r.Services)
                .ThenInclude(s => s.ServiceCatalog)
            .Include(r => r.Tasks)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(r => r.ClientId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.MyVehicles = await _context.Vehicles
            .Where(v => v.OwnerId == user.Id)
            .AsNoTracking()
            .OrderBy(v => v.Make)
            .ThenBy(v => v.Model)
            .ToListAsync();

        return View(requests);
    }

    // Список автомобилей клиента
    public async Task<IActionResult> Vehicles()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var vehicles = await _context.Vehicles
            .Where(v => v.OwnerId == user.Id)
            .ToListAsync();

        return View(vehicles);
    }

    // Добавление автомобиля (GET)
    [HttpGet]
    public IActionResult AddVehicle()
    {
        return View();
    }

    // Добавление автомобиля (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVehicle(Vehicle vehicle)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        vehicle.OwnerId = user.Id;
        ModelState.Remove(nameof(vehicle.Owner));

        if (ModelState.IsValid)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Автомобиль успешно добавлен.";
            return RedirectToAction("Profile", "Account");
        }

        return View(vehicle);
    }

    // Запись на сервис (GET)
    [HttpGet]
    public async Task<IActionResult> Book(int? vehicleId = null, string? appointmentDate = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        await PopulateBookingViewBagsAsync(user.Id);
        ViewBag.SelectedVehicleId = vehicleId;
        ViewBag.SelectedAppointmentDate = appointmentDate;
        return View();
    }

    // Запись на сервис (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int vehicleId, List<int>? serviceIds, string? notes, string appointmentDate, string appointmentTime)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!TryParseAppointment(appointmentDate, appointmentTime, out var scheduledDate, out var scheduleError))
        {
            ModelState.AddModelError(nameof(appointmentDate), scheduleError!);
        }
        else if (await _context.ServiceRequests.AnyAsync(r => r.ScheduledDate == scheduledDate && r.Status != RequestStatus.Cancelled))
        {
            ModelState.AddModelError(nameof(appointmentTime), "Это время уже занято. Выберите другой слот.");
        }

        if (vehicleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Выберите автомобиль.");
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == user.Id);
        if (vehicle == null)
        {
            ModelState.AddModelError(nameof(vehicleId), "Выберите автомобиль из своего гаража.");
        }

        var selectedServiceIds = serviceIds?.Distinct().ToList() ?? new List<int>();
        ViewBag.SelectedServiceIds = selectedServiceIds;
        if (selectedServiceIds.Count == 0)
        {
            ModelState.AddModelError(nameof(serviceIds), "Выберите хотя бы одну услугу.");
        }

        var catalogs = selectedServiceIds.Count == 0
            ? new List<ServiceCatalog>()
            : await _context.ServiceCatalogs
                .Where(c => selectedServiceIds.Contains(c.Id) && c.IsActive && BasicBookingServiceNames.Contains(c.Name))
                .ToListAsync();

        if (catalogs.Count != selectedServiceIds.Count)
        {
            ModelState.AddModelError(nameof(serviceIds), "Одна или несколько выбранных услуг недоступны.");
        }

        if (ModelState.IsValid)
        {
            var serviceRequest = new ServiceRequest
            {
                ClientId = user.Id,
                VehicleId = vehicleId,
                Status = RequestStatus.AwaitingApproval,
                CreatedAt = DateTime.UtcNow,
                ScheduledDate = scheduledDate,
                Notes = notes,
                Description = string.IsNullOrWhiteSpace(notes) ? "Диагностика и осмотр автомобиля" : notes.Trim(),
                EstimatedCost = 0
            };

            foreach (var cat in catalogs)
            {
                serviceRequest.SelectedServices.Add(cat);
                serviceRequest.Services.Add(new Service
                {
                    ServiceCatalogId = cat.Id,
                    Name = cat.Name,
                    Price = 0,
                    Description = cat.Description
                });
            }

            serviceRequest.Price = serviceRequest.Services.Sum(s => s.Price);

            _context.ServiceRequests.Add(serviceRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Заявка успешно оформлена!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateBookingViewBagsAsync(user.Id);
        ViewBag.SelectedVehicleId = vehicleId;
        ViewBag.SelectedAppointmentDate = appointmentDate;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var request = await _context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ClientId == user.Id);
        if (request == null) return NotFound();

        if (request.Status is not (RequestStatus.AwaitingApproval or RequestStatus.Planned))
        {
            TempData["Error"] = "Отменить можно только заявку, по которой обслуживание ещё не началось.";
            return RedirectToAction(nameof(Index));
        }

        request.Status = RequestStatus.Cancelled;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Заявка №{request.Id} отменена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var request = await _context.ServiceRequests
            .Include(r => r.Vehicle)
            .Include(r => r.AssignedEmployee)
            .Include(r => r.Services)
            .Include(r => r.Tasks)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == user.Id);

        return request == null ? NotFound() : View(request);
    }

    private static bool TryParseAppointment(string? date, string? time, out DateTime scheduledDate, out string? error)
    {
        scheduledDate = default;
        error = null;
        if (!DateTime.TryParseExact($"{date} {time}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out scheduledDate))
        {
            error = "Выберите корректные дату и время записи.";
            return false;
        }

        if (scheduledDate < DateTime.Now)
        {
            error = "Нельзя записаться на прошедшее время.";
            return false;
        }

        if (!ServiceSchedule.IsValidAppointment(scheduledDate))
        {
            error = "Запись доступна с понедельника по субботу с 10:00 до 18:00 с шагом 30 минут.";
            return false;
        }

        scheduledDate = DateTime.SpecifyKind(scheduledDate, DateTimeKind.Local).ToUniversalTime();
        return true;
    }

    [HttpGet]
    public async Task<IActionResult> EditVehicle(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.OwnerId == user.Id);
        return vehicle == null ? NotFound() : View(vehicle);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVehicle(int id, Vehicle vehicle)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.OwnerId == user.Id);
        if (existing == null) return NotFound();

        ModelState.Remove(nameof(Vehicle.Owner));
        ModelState.Remove(nameof(Vehicle.OwnerId));
        if (!ModelState.IsValid) return View(vehicle);

        existing.Make = vehicle.Make;
        existing.Model = vehicle.Model;
        existing.Year = vehicle.Year;
        existing.VIN = vehicle.VIN;
        existing.LicensePlate = vehicle.LicensePlate;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Данные автомобиля обновлены.";
        return RedirectToAction("Profile", "Account");
    }

    [HttpGet("/Client/DeleteVehicle/{id:int}")]
    public IActionResult RedirectFromDeleteVehicleUrl(int id)
    {
        TempData["Error"] = "Удаление автомобиля выполняется кнопкой в списке автомобилей.";
        return RedirectToAction(nameof(Vehicles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.OwnerId == user.Id);
        if (vehicle == null) return NotFound();

        if (await _context.ServiceRequests.AnyAsync(r => r.VehicleId == id))
        {
            TempData["Error"] = "Автомобиль нельзя удалить: по нему уже есть заявки.";
            return RedirectToAction("Profile", "Account");
        }

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Автомобиль удалён.";
        return RedirectToAction("Profile", "Account");
    }

    private async Task PopulateBookingViewBagsAsync(string userId)
    {
        var vehicles = await _context.Vehicles.Where(v => v.OwnerId == userId).ToListAsync();

        // Преобразуем список авто в явный SelectList, исключая RuntimeBinderException
        var selectListItems = vehicles.Select(v => new SelectListItem
        {
            Value = v.Id.ToString(),
            Text = $"{v.Make} {v.Model} ({v.LicensePlate})"
        }).ToList();

        ViewBag.Vehicles = new SelectList(selectListItems, "Value", "Text");
        ViewBag.Services = await _context.ServiceCatalogs
            .Where(s => s.IsActive && BasicBookingServiceNames.Contains(s.Name))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}