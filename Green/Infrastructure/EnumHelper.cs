using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Green.Infrastructure
{
    public static class EnumHelper
    {
        public static string GetDisplayName(this Enum value)
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString()).FirstOrDefault();

            if (memberInfo == null)
                return value.ToString();

            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();

            if (displayAttribute != null)
                return displayAttribute.Name ?? value.ToString();

            return value.ToString();
        }

        public static List<(string Value, string Text)> GetEnumSelectList<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => (e.ToString(), e.GetDisplayName()))
                .ToList();
        }
    }
}
