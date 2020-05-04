using System.Reflection;

namespace Zat.Shared.Reflection
{
    public static class ReflectionExtensions
    {
        private static FieldInfo GetFieldInfo(object obj, string name)
        {
            return obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        private static PropertyInfo GetPropertyInfo(object obj, string name)
        {
            return obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static T GetField<T>(this object obj, string name)
        {
            return (T)GetFieldInfo(obj, name)?.GetValue(obj);
        }
        public static void SetField<T>(this object obj, string name, T value)
        {
            var field = GetFieldInfo(obj, name);
            if (field != null) field.SetValue(obj, value);
        }
        public static T GetProperty<T>(this object obj, string name)
        {
            return (T)GetPropertyInfo(obj, name)?.GetValue(obj, null);
        }
        public static void SetProperty<T>(this object obj, string name, T value)
        {
            var prop = GetPropertyInfo(obj, name);
            if (prop != null) prop.SetValue(obj, value, null);
        }
    }
}
