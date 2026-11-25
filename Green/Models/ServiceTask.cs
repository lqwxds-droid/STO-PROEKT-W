using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Green.Models
{
    public class ServiceTask
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        public bool? IsApprovedByClient { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [Display(Name = "Плановая дата")]
        public DateTime? PlannedDate { get; set; }

        public int ServiceRequestId { get; set; }
        public ServiceRequest? ServiceRequest { get; set; }
    }
}