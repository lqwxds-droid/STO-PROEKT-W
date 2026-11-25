using System.ComponentModel.DataAnnotations;

namespace Green.Models
{
    public enum UserRole
    {
        [Display(Name = "Администратор")]
        Admin,

        [Display(Name = "Сотрудник")]
        Employee,

        [Display(Name = "Клиент")]
        Client
    }
}