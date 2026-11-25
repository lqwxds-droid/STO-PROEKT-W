using Green.Data;
using Green.Models;
using Green.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Green.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleService(int serviceId, bool isCompleted)
        {
            var employee = await _userManager.GetUserAsync(User);
            if (employee == null) return Challenge();

            var service = await _context.Services
                .Include(s => s.ServiceRequest)
                .ThenInclude(request => request!.Services)
                .Include(s => s.ServiceRequest)
                .ThenInclude(request => request!.Tasks)
                .FirstOrDefaultAsync(s => s.Id == serviceId &&
                    s.ServiceRequest!.AssignedEmployeeId == employee.Id &&
                    (s.ServiceRequest.Status == RequestStatus.Planned || s.ServiceRequest.Status == RequestStatus.InProgress));

            if (service == null) return NotFound();

            service.IsCompleted = isCompleted;
            UpdateOrderStatus(service.ServiceRequest!);
            RecalculateOrderPrice(service.ServiceRequest!);
            await _context.SaveChangesAsync();
            TempData["Success"] = isCompleted ? "Услуга отмечена как выполненная." : "Отметка о выполнении услуги снята.";
            return RedirectToAction(nameof(ManageOrder), new { id = service.ServiceRequestId });
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var query = _context.ServiceRequests
                .Include(r => r.Vehicle)
                .Include(r => r.Client)
                .Include(r => r.Services)
                .Include(r => r.SelectedServices)
                .Include(r => r.Tasks)
                .AsNoTracking()
                .AsSplitQuery()
                .AsQueryable();

            query = query.Where(r => r.AssignedEmployeeId == currentUser.Id);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> ManageOrder(int id)
        {
            var employee = await _userManager.GetUserAsync(User);
            if (employee == null) return Challenge();

            var request = await _context.ServiceRequests
                .Include(r => r.Vehicle)
                .Include(r => r.Client)
                .Include(r => r.Tasks)
                .Include(r => r.Services)
                .Include(r => r.SelectedServices)
                .FirstOrDefaultAsync(r => r.Id == id && r.AssignedEmployeeId == employee.Id);
            if (request == null) return NotFound();

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(int requestId, string title, decimal cost, DateTime? plannedDate)
        {
            var request = await GetEmployeeRequestAsync(requestId);
            if (request == null) return NotFound();
            if (!CanEditWork(request))
            {
                TempData["Error"] = "Добавлять работы можно только в открытый заказ-наряд.";
                return RedirectToAction(nameof(ManageOrder), new { id = requestId });
            }
            if (string.IsNullOrWhiteSpace(title) || cost < 0)
            {
                TempData["Error"] = "Укажите название работы и корректную стоимость.";
                return RedirectToAction(nameof(ManageOrder), new { id = requestId });
            }

            var normalizedPlannedDate = plannedDate.HasValue
                ? DateTime.SpecifyKind(plannedDate.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : (DateTime?)null;

            request.Tasks.Add(new ServiceTask
            {
                Title = title.Trim(),
                Cost = cost,
                PlannedDate = normalizedPlannedDate,
                IsApprovedByClient = null
            });
            UpdateOrderStatus(request);
            RecalculateOrderPrice(request);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Дополнительная работа отправлена клиенту на согласование.";
            return RedirectToAction(nameof(ManageOrder), new { id = requestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTask(int taskId, string title, decimal cost, DateTime? plannedDate)
        {
            var employee = await _userManager.GetUserAsync(User);
            var task = employee == null ? null : await _context.ServiceTasks
                .Include(t => t.ServiceRequest)
                .ThenInclude(request => request!.Services)
                .Include(t => t.ServiceRequest)
                .ThenInclude(request => request!.Tasks)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ServiceRequest!.AssignedEmployeeId == employee.Id);

            if (task == null) return NotFound();
            if (!CanEditWork(task.ServiceRequest!)) return Forbid();

            if (string.IsNullOrWhiteSpace(title) || cost < 0)
            {
                TempData["Error"] = "Укажите название работы и корректную стоимость.";
                return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
            }

            if (task.IsApprovedByClient == true && (task.Title != title.Trim() || task.Cost != cost))
            {
                TempData["Error"] = "Согласованную работу нельзя менять. Создайте отдельную работу и запросите согласование клиента.";
                return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
            }

            task.Title = title.Trim();
            task.Cost = cost;
            task.PlannedDate = plannedDate.HasValue
                ? DateTime.SpecifyKind(plannedDate.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : null;
            task.IsApprovedByClient = null;
            task.IsCompleted = false;
            RecalculateOrderPrice(task.ServiceRequest!);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Этап работы обновлён.";
            return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRejectedTask(int taskId)
        {
            var employee = await _userManager.GetUserAsync(User);
            var task = employee == null ? null : await _context.ServiceTasks
                .Include(t => t.ServiceRequest)
                .ThenInclude(request => request!.Services)
                .Include(t => t.ServiceRequest)
                .ThenInclude(request => request!.Tasks)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ServiceRequest!.AssignedEmployeeId == employee.Id);

            if (task == null) return NotFound();
            if (!CanEditWork(task.ServiceRequest!)) return Forbid();
            if (task.IsApprovedByClient != false)
            {
                TempData["Error"] = "Удалить можно только работу, отклонённую клиентом.";
                return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
            }

            var request = task.ServiceRequest!;
            request.Tasks.Remove(task);
            _context.ServiceTasks.Remove(task);
            RecalculateOrderPrice(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Отклонённая клиентом работа удалена из заказ-наряда.";
            return RedirectToAction(nameof(ManageOrder), new { id = request.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, RequestStatus status)
        {
            var request = await GetEmployeeRequestAsync(id);
            if (request == null) return NotFound();

            if (request.Status is not (RequestStatus.Planned or RequestStatus.InProgress) ||
                status is not (RequestStatus.InProgress or RequestStatus.Completed))
            {
                TempData["Error"] = "Укажите допустимый переход статуса.";
                return RedirectToAction(nameof(ManageOrder), new { id });
            }
            if (status == RequestStatus.InProgress && request.Tasks.Any(task => task.IsApprovedByClient == false))
            {
                TempData["Error"] = "Отклонённые клиентом работы нужно удалить из заказ-наряда перед продолжением.";
                return RedirectToAction(nameof(ManageOrder), new { id });
            }
            if (status == RequestStatus.Completed &&
                (!request.Services.Any() && !request.Tasks.Any() ||
                 request.Services.Any(service => !service.IsCompleted) ||
                 request.Tasks.Any(task => task.IsApprovedByClient == null) ||
                 request.Tasks.Any(task => task.IsApprovedByClient == true && !task.IsCompleted)))
            {
                TempData["Error"] = "Для закрытия наряда выполните все услуги и согласованные работы; дождитесь решения клиента по дополнительным работам.";
                return RedirectToAction(nameof(ManageOrder), new { id });
            }

            if (status == RequestStatus.Completed && request.Tasks.Any(task => task.IsApprovedByClient == false))
            {
                TempData["Error"] = "Удалите отклонённые клиентом работы из заказ-наряда перед его закрытием.";
                return RedirectToAction(nameof(ManageOrder), new { id });
            }

            request.Status = status;
            RecalculateOrderPrice(request);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Заявка №{id} обновлена.";
            return RedirectToAction(nameof(ManageOrder), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderDetails(int id, string? estimatedTime, string? masterComment)
        {
            var request = await GetEmployeeRequestAsync(id);
            if (request == null) return NotFound();
            if (!CanEditWork(request)) return Forbid();

            request.EstimatedTime = estimatedTime?.Trim();
            request.MasterComment = masterComment?.Trim();
            RecalculateOrderPrice(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Данные заказ-наряда №{id} обновлены.";
            return RedirectToAction(nameof(ManageOrder), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTask(int taskId, bool isCompleted)
        {
            var employee = await _userManager.GetUserAsync(User);
            var task = employee == null ? null : await _context.ServiceTasks
                .Include(t => t.ServiceRequest)
                .ThenInclude(request => request!.Services)
                .Include(t => t.ServiceRequest)
                .ThenInclude(request => request!.Tasks)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ServiceRequest!.AssignedEmployeeId == employee.Id);
            if (task == null) return NotFound();
            if (!CanEditWork(task.ServiceRequest!)) return Forbid();
            if (task.IsApprovedByClient == false)
            {
                TempData["Error"] = "Нельзя менять этап, который клиент отклонил.";
                return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
            }
            if (task.IsApprovedByClient == null)
            {
                TempData["Error"] = "Выполнять дополнительную работу можно только после согласия клиента.";
                return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
            }
            task.IsCompleted = isCompleted;
            UpdateOrderStatus(task.ServiceRequest!);
            RecalculateOrderPrice(task.ServiceRequest!);
            await _context.SaveChangesAsync();
            TempData["Success"] = isCompleted ? "Дополнительная работа отмечена как выполненная." : "Отметка о выполнении дополнительной работы снята.";
            return RedirectToAction(nameof(ManageOrder), new { id = task.ServiceRequestId });
        }

        private static void RecalculateOrderPrice(ServiceRequest request)
        {
            request.Price = request.Services.Sum(service => service.Price) + request.Tasks.Where(task => task.IsApprovedByClient != false).Sum(task => task.Cost);
            request.EstimatedCost = request.Price;
        }

        private async Task<ServiceRequest?> GetEmployeeRequestAsync(int id)
        {
            var employee = await _userManager.GetUserAsync(User);
            if (employee == null) return null;
            return await _context.ServiceRequests
                .Include(r => r.Services)
                .Include(r => r.Tasks)
                .FirstOrDefaultAsync(r => r.Id == id && r.AssignedEmployeeId == employee.Id);
        }

        private static bool CanEditWork(ServiceRequest request) =>
            request.Status is RequestStatus.Planned or RequestStatus.InProgress;

        private static void UpdateOrderStatus(ServiceRequest request)
        {
            if (request.Status == RequestStatus.Planned)
            {
                request.Status = RequestStatus.InProgress;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, RequestStatus status)
        {
            if (status is not (RequestStatus.InProgress or RequestStatus.Completed))
            {
                TempData["Error"] = "Мастер может начать работу или закрыть заказ-наряд.";
                return RedirectToAction(nameof(Index));
            }

            var request = await GetEmployeeRequestAsync(requestId);
            if (request == null)
            {
                TempData["Error"] = "Заказ-наряд не найден.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status is not (RequestStatus.Planned or RequestStatus.InProgress) ||
                status == RequestStatus.Completed &&
                (!request.Services.Any() && !request.Tasks.Any() ||
                 request.Services.Any(service => !service.IsCompleted) ||
                 request.Tasks.Any(task => task.IsApprovedByClient == null) ||
                 request.Tasks.Any(task => task.IsApprovedByClient == true && !task.IsCompleted)))
            {
                TempData["Error"] = "Нельзя выполнить выбранный переход статуса: проверьте состояние наряда и всех работ.";
                return RedirectToAction(nameof(ManageOrder), new { id = requestId });
            }

            request.Status = status;
            RecalculateOrderPrice(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Статус заказа №{requestId} успешно обновлен.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int requestId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["Error"] = "Текст заметки не может быть пустым.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var request = currentUser == null ? null : await _context.ServiceRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.AssignedEmployeeId == currentUser.Id);
            if (request == null)
            {
                TempData["Error"] = "Заказ-наряд не найден.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            string authorName = user?.UserName ?? User.Identity?.Name ?? "Механик";
            string formattedNote = $"\n[{DateTime.Now:dd.MM.yyyy HH:mm} — {authorName}]: {commentText.Trim()}";

            if (string.IsNullOrEmpty(request.Notes))
            {
                request.Notes = formattedNote.TrimStart();
            }
            else
            {
                request.Notes += formattedNote;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Заметка к заказу №{requestId} успешно добавлена!";
            return RedirectToAction(nameof(Index));
        }
    }
}