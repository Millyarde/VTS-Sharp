using System.ComponentModel;

namespace VTS
{
    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : null;
        }
    }
}