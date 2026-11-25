using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Identity;
using Green.Infrastructure;
using System.Globalization;

namespace Green.Pages.ServiceRequests;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public ServiceRequest ServiceRequest { get; set; } = new();

    public List<ServiceCatalog> AvailableServices { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();

    [BindProperty]
    public List<int> SelectedServiceIds { get; set; } = new();

    [BindProperty]
    public string AppointmentDate { get; set; } = string.Empty;

    [BindProperty]
    public string AppointmentTime { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        AvailableServices = await _context.ServiceCatalogs.Where(s => s.IsActive).ToListAsync();
        var user = await _userManager.GetUserAsync(User);
        Vehicles = user == null
            ? new List<Vehicle>()
            : await _context.Vehicles.Where(v => v.OwnerId == user.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        ServiceRequest.ClientId = user.Id;
        ServiceRequest.AssignedEmployeeId = null;
        ServiceRequest.Tasks.Clear();
        ServiceRequest.Services.Clear();
        ServiceRequest.SelectedServices.Clear();
        if (string.IsNullOrWhiteSpace(ServiceRequest.Description))
        {
            ServiceRequest.Description = string.IsNullOrWhiteSpace(ServiceRequest.Notes)
                ? "Заявка на обслуживание автомобиля"
                : ServiceRequest.Notes.Trim();
        }

        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.ClientId)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.Client)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.AssignedEmployeeId)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.AssignedEmployee)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.Services)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.Tasks)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.SelectedServices)}");
        ModelState.Remove($"{nameof(ServiceRequest)}.{nameof(ServiceRequest.Description)}");
        if (!await _context.Vehicles.AnyAsync(v => v.Id == ServiceRequest.VehicleId && v.OwnerId == user.Id))
        {
            ModelState.AddModelError(nameof(ServiceRequest.VehicleId), "Выберите автомобиль из своего гаража.");
        }

        var selectedServiceIds = SelectedServiceIds?.Distinct().ToList() ?? new List<int>();
        if (selectedServiceIds.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Выберите хотя бы одну услугу.");
        }

        var services = selectedServiceIds.Count == 0
            ? new List<ServiceCatalog>()
            : await _context.ServiceCatalogs
                .Where(service => selectedServiceIds.Contains(service.Id) && service.IsActive)
                .ToListAsync();
        if (services.Count != selectedServiceIds.Count)
        {
            ModelState.AddModelError(string.Empty, "Одна или несколько выбранных услуг недоступны.");
        }

        var hasValidAppointment = DateTime.TryParseExact($"{AppointmentDate} {AppointmentTime}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var scheduledDate);
        if (!hasValidAppointment || scheduledDate < DateTime.Now || !ServiceSchedule.IsValidAppointment(scheduledDate))
        {
            ModelState.AddModelError(nameof(AppointmentDate), "Запись доступна с понедельника по субботу с 10:00 до 18:00 с шагом 30 минут.");
        }
        else
        {
            scheduledDate = DateTime.SpecifyKind(scheduledDate, DateTimeKind.Local).ToUniversalTime();
            if (await _context.ServiceRequests.AnyAsync(r => r.ScheduledDate == scheduledDate && r.Status != RequestStatus.Cancelled))
            {
                ModelState.AddModelError(nameof(AppointmentTime), "Это время уже занято. Выберите другой слот.");
            }
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        foreach (var service in services)
        {
            ServiceRequest.Services.Add(new Service { ServiceCatalogId = service.Id, Price = service.Price, Name = service.Name });
            ServiceRequest.SelectedServices.Add(service);
        }

        ServiceRequest.CreatedAt = DateTime.UtcNow;
        ServiceRequest.ScheduledDate = scheduledDate;
        ServiceRequest.Status = RequestStatus.AwaitingApproval;

        // Вычисляем итоговую сумму выбранных услуг
        ServiceRequest.Price = ServiceRequest.Services.Sum(x => x.Price);
        ServiceRequest.EstimatedCost = ServiceRequest.Price;

        _context.ServiceRequests.Add(ServiceRequest);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
