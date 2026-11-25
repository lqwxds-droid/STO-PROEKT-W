using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Green.Models
{
    public class ServiceCatalog
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название услуги")]
        [Display(Name = "Название работы")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание / Подробности")]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Цена (₽)")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        // Флаг выполнения работы
        public bool IsCompleted { get; set; } = false;

        // Связь многие-ко-многим с заказами
        public List<ServiceRequest> ServiceRequests { get; set; } = new();
    }
}