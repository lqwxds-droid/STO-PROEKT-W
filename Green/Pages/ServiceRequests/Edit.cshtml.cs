using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Green.Pages.ServiceRequests;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public ServiceRequest ServiceRequest { get; set; } = default!;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? MasterComment { get; set; }

    [BindProperty]
    public string? AssignedEmployeeId { get; set; }

    public List<ServiceCatalog> AvailableServices { get; set; } = new();
    public IList<ApplicationUser> Employees { get; set; } = new List<ApplicationUser>();

    [BindProperty]
    public List<int> SelectedServiceIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ServiceRequest = await FindAllowedAsync(id);

        if (ServiceRequest == null)
        {
            TempData["Error"] = $"Заявка №{id} не найдена. Возможно, она уже удалена или используется другая база данных.";
            return RedirectToPage("./Index");
        }

        Description = ServiceRequest.Description;
        MasterComment = ServiceRequest.MasterComment;
        AssignedEmployeeId = ServiceRequest.AssignedEmployeeId;
        SelectedServiceIds = ServiceRequest.SelectedServices.Select(s => s.Id).ToList();
        await LoadEditorOptionsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var existing = await FindAllowedAsync(id);
        if (existing == null)
        {
            TempData["Error"] = $"Заявка №{id} не найдена. Обновите список заявок и выберите запись повторно.";
            return RedirectToPage("./Index");
        }

        if (existing.Status is not (RequestStatus.AwaitingApproval or RequestStatus.Planned))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            Description = existing.Description;
            ModelState.Remove(nameof(Description));
        }

        ApplicationUser? assignedEmployee = null;
        if (!string.IsNullOrWhiteSpace(AssignedEmployeeId))
        {
            assignedEmployee = await _userManager.FindByIdAsync(AssignedEmployeeId);
            if (assignedEmployee == null || !await _userManager.IsInRoleAsync(assignedEmployee, "Employee"))
            {
                ModelState.AddModelError(nameof(AssignedEmployeeId), "Выберите действующего мастера.");
            }
        }

        var selectedServiceIds = SelectedServiceIds?.Distinct().ToList() ?? new List<int>();
        var selectedCatalogServices = selectedServiceIds.Count == 0
            ? new List<ServiceCatalog>()
            : await _context.ServiceCatalogs
                .Where(service => selectedServiceIds.Contains(service.Id) && service.IsActive)
                .ToListAsync();
        if (selectedCatalogServices.Count != selectedServiceIds.Count)
        {
            ModelState.AddModelError(nameof(SelectedServiceIds), "Одна или несколько выбранных услуг недоступны.");
        }

        if (!ModelState.IsValid)
        {
            ServiceRequest = existing;
            ServiceRequest.Description = Description;
            ServiceRequest.MasterComment = MasterComment;
            ServiceRequest.AssignedEmployeeId = AssignedEmployeeId;
            await LoadEditorOptionsAsync();
            return Page();
        }

        existing.Description = Description;
        existing.MasterComment = MasterComment;
        _context.Entry(existing).Property(request => request.AssignedEmployeeId).CurrentValue = assignedEmployee?.Id;
        _context.Entry(existing).Property(request => request.AssignedEmployeeId).IsModified = true;
        if (assignedEmployee != null && existing.Status == RequestStatus.AwaitingApproval)
        {
            existing.Status = RequestStatus.Planned;
        }

        existing.Services.Clear();
        existing.SelectedServices.Clear();
        foreach (var catalogService in selectedCatalogServices)
        {
            existing.Services.Add(new Service
            {
                ServiceCatalogId = catalogService.Id,
                Price = catalogService.Price,
                Name = catalogService.Name
            });
            existing.SelectedServices.Add(catalogService);
        }

        existing.Price = (existing.Services?.Sum(s => s.Price) ?? 0) + (existing.Tasks?.Sum(t => t.Cost) ?? 0);
        existing.EstimatedCost = existing.Price;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.ServiceRequests.AnyAsync(e => e.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        var savedEmployeeId = await _context.ServiceRequests
            .AsNoTracking()
            .Where(request => request.Id == id)
            .Select(request => request.AssignedEmployeeId)
            .SingleAsync();
        if (savedEmployeeId != assignedEmployee?.Id)
        {
            TempData["Error"] = $"Не удалось сохранить назначение мастера для заявки №{existing.Id}. Повторите попытку.";
            return RedirectToPage("./Index");
        }

        TempData["Success"] = assignedEmployee == null
            ? $"Заявка №{existing.Id} сохранена без назначенного мастера."
            : $"Заявка №{existing.Id} назначена мастеру {GetEmployeeDisplayName(assignedEmployee)} ({assignedEmployee.Email}).";
        return RedirectToPage("./Index");
    }

    private static string GetEmployeeDisplayName(ApplicationUser employee) =>
        string.IsNullOrWhiteSpace(employee.FullName) ? employee.UserName ?? employee.Email ?? employee.Id : employee.FullName;

    private async Task LoadEditorOptionsAsync()
    {
        AvailableServices = await _context.ServiceCatalogs.Where(s => s.IsActive).ToListAsync();
        Employees = await _userManager.GetUsersInRoleAsync("Employee");
    }

    private async Task<ServiceRequest?> FindAllowedAsync(int id)
    {
        var query = _context.ServiceRequests
            .Include(r => r.Services)
            .Include(r => r.Tasks)
            .Include(r => r.SelectedServices)
            .AsQueryable();
        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }
}
