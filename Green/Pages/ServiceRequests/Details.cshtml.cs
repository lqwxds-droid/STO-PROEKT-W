using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Green.Pages.ServiceRequests;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public ServiceRequest? ServiceRequest { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Structural context for authorization checks
        var isAdmin = User.IsInRole("Admin");
        var isEmployee = User.IsInRole("Employee");

        var query = _context.ServiceRequests
            .Include(r => r.Vehicle)
            .Include(r => r.Client)
            .Include(r => r.AssignedEmployee)
            .Include(r => r.Tasks)
            .Include(r => r.Services)
            .Include(r => r.SelectedServices)
            .AsQueryable();

        if (isEmployee && !isAdmin)
        {
            query = query.Where(request => request.AssignedEmployeeId == user.Id);
        }
        else if (!isAdmin)
        {
            query = query.Where(r => r.ClientId == user.Id);
        }

        ServiceRequest = await query.FirstOrDefaultAsync(r => r.Id == id);

        if (ServiceRequest == null)
        {
            return NotFound();
        }

        return Page();
    }
}
