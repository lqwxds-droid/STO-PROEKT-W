using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Mvc;
using Green.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Green.Pages.ServiceRequests;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public PaginatedList<ServiceRequest> ServiceRequests { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 9;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var query = _context.ServiceRequests
            .Include(r => r.Vehicle)
            .Include(r => r.Client)
            .Include(r => r.AssignedEmployee)
            .Include(r => r.SelectedServices)
            .Include(r => r.Tasks)
            .Include(r => r.Services)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

        if (User.IsInRole("Employee") && !User.IsInRole("Admin"))
        {
            query = query.Where(r => r.AssignedEmployeeId == user.Id);
        }
        else if (!User.IsInRole("Admin"))
        {
            query = query.Where(r => r.ClientId == user.Id);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(r => r.Description.Contains(s) || r.Client.UserName.Contains(s) || r.Vehicle.Make.Contains(s) || r.Vehicle.Model.Contains(s));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(r => r.Status == StatusFilter.Value);
        }

        if (VehicleId.HasValue)
        {
            query = query.Where(r => r.VehicleId == VehicleId.Value);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        ServiceRequests = await PaginatedList<ServiceRequest>.CreateAsync(query, PageIndex == 0 ? 1 : PageIndex, PageSize);
        return Page();
    }
}
