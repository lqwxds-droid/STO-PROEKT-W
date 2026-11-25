using Green.Data;
using Green.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Green.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Services
        // Отображение всего прейскуранта с возможностью поиска
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.ServiceCatalogs.AsQueryable();
            var canManage = User.IsInRole("Admin");

            if (!canManage)
            {
                query = query.Where(s => s.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Name.Contains(search) ||
                                         (s.Description != null && s.Description.Contains(search)));
            }

            var services = await query
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.Name)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CanManage = canManage;
            return View(services);
        }

        // GET: Services/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
        {
            return View(new ServiceCatalog());
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("Name,Description,Price,IsActive")] ServiceCatalog service)
        {
            if (ModelState.IsValid)
            {
                _context.ServiceCatalogs.Add(service);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Услуга '{service.Name}' успешно добавлена в каталог!";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // GET: Services/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.ServiceCatalogs.FindAsync(id);
            if (service == null)
            {
                TempData["Error"] = "Услуга не найдена в прейскуранте.";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,IsActive")] ServiceCatalog service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Услуга '{service.Name}' успешно обновлена!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // POST: Services/ToggleStatus/5
        // Экспресс-переключение активности (доступности) работы без ее удаления
        [HttpPost]
        [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(int id)
        {
            var service = await _context.ServiceCatalogs.FindAsync(id);
            if (service == null)
            {
                TempData["Error"] = "Услуга не найдена.";
                return RedirectToAction(nameof(Index));
            }

            service.IsActive = !service.IsActive;
            await _context.SaveChangesAsync();

            string statusText = service.IsActive ? "активирована" : "деактивирована";
            TempData["Success"] = $"Услуга '{service.Name}' {statusText}.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Services/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.ServiceCatalogs.FindAsync(id);
            if (service != null)
            {
                _context.ServiceCatalogs.Remove(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Услуга удалена из базы данных.";
            }
            else
            {
                TempData["Error"] = "Услуга не найдена.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.ServiceCatalogs.Any(e => e.Id == id);
        }
    }
}