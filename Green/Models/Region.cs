using System.ComponentModel.DataAnnotations;

namespace Green.Models;

public class Region
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}