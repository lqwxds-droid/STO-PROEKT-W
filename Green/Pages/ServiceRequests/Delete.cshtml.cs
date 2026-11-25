using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Authorization;

namespace Green.Pages.ServiceRequests;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ServiceRequest? ServiceRequest { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ServiceRequest = await FindAllowedAsync(id);

        if (ServiceRequest == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var entity = await FindAllowedAsync(id);
        if (entity != null)
        {
            _context.ServiceRequests.Remove(entity);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }

    private async Task<ServiceRequest?> FindAllowedAsync(int id)
    {
        return await _context.ServiceRequests
            .Include(r => r.Vehicle)
            .Include(r => r.Client)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
