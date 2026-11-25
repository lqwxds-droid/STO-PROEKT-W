using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Green.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название услуги")]
        [Display(Name = "Название услуги")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Укажите стоимость")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000, ErrorMessage = "Цена должна быть положительным числом")]
        [Display(Name = "Стоимость (руб)")]
        public decimal Price { get; set; }

        // Флаг выполнения конкретной услуги
        public bool IsCompleted { get; set; } = false;

        // Навигационное свойство для связи с заказ-нарядами
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

        public int ServiceRequestId { get; set; }
        [ForeignKey("ServiceRequestId")]
        public ServiceRequest? ServiceRequest { get; set; }

        public int ServiceCatalogId { get; set; }
        [ForeignKey("ServiceCatalogId")]
        public ServiceCatalog? ServiceCatalog { get; set; }
    }
}