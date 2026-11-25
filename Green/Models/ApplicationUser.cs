using Microsoft.AspNetCore.Identity;

namespace Green.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty; // ФИО
        // Role хранится в Identity через роли AspNetRoles; этот enum остаётся для внутренних нужд.
        public UserRole Role { get; set; } = UserRole.Client; // Роль по умолчанию (дублирующая информация)

        // Связь с автомобилями (для клиентов)
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}