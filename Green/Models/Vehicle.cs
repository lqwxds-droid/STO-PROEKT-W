using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Green.Models;

public class Vehicle
{
    public int Id { get; set; }

    [Required]
    public string Make { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    // VIN сделан необязательным (nullable string)
    [Display(Name = "VIN номер")]
    [StringLength(17, ErrorMessage = "VIN должен содержать не более 17 символов")]
    public string? VIN { get; set; }

    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    // Имя внешнего ключа владельца — OwnerId (именно его ждут контроллеры и вьюхи)
    public string? OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public ApplicationUser? Owner { get; set; }

    [NotMapped]
    public string DisplayName => $"{Make} {Model} ({LicensePlate})";
}