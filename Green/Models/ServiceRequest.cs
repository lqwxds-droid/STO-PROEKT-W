using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Green.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public string? AssignedEmployeeId { get; set; }
        public ApplicationUser? AssignedEmployee { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        // Поля для мастера, чек-листа и времени
        public string? Notes { get; set; }
        public string? MasterComment { get; set; }
        // Замените старую строку public string? Tasks { get; set; } на эта:
        public List<ServiceTask> Tasks { get; set; } = new List<ServiceTask>();
        public string? EstimatedTime { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Planned;

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; }

        public DateTime ScheduledDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Добавьте это свойство в модель ServiceRequest:
        public List<ServiceCatalog> SelectedServices { get; set; } = new();

        public decimal Price { get; set; }

        public List<Service> Services { get; set; } = new();

        [NotMapped]
        public decimal TotalCost => Services.Sum(s => s.Price) + Tasks.Sum(t => t.Cost);
    }

    public enum RequestStatus
    {
        Planned = 0,           // Принят и запланирован
        InProgress = 1,        // В работе
        Completed = 2,         // Завершён
        Cancelled = 3,         // Отменён
        AwaitingApproval = 4   // Ожидает подтверждения администратора
    }
}