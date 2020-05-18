using System;
using System.Reflection;

namespace Zat.Shared.Reflection
{
    public static class ZatsReflection
    {
        private static FieldInfo GetFieldInfo(Type t, string name, bool instance = true)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            if (instance) flags |= BindingFlags.Instance;
            return t.GetField(name, flags);
        }
        private static PropertyInfo GetPropertyInfo(Type t, string name, bool instance = true)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            if (instance) flags |= BindingFlags.Instance;
            return t.GetProperty(name, flags);
        }
        private static MethodInfo GetMethodInfo(Type t, string name, bool instance = true)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            if (instance) flags |= BindingFlags.Instance;
            return t.GetMethod(name, flags);
        }

        public static T GetField<T>(this object obj, string name)
        {
            return (T)GetFieldInfo(obj.GetType(), name)?.GetValue(obj);
        }
        public static V GetStaticField<T, V>(string name)
        {
            return (V)GetFieldInfo(typeof(T), name, false)?.GetValue(null);
        }

        public static void SetField<T>(this object obj, string name, T value)
        {
            GetFieldInfo(obj.GetType(), name)?.SetValue(obj, value);
        }
        public static void SetStaticField<T, V>(string name, V value)
        {
            GetFieldInfo(typeof(T), name, false)?.SetValue(null, value);
        }

        public static T GetProperty<T>(this object obj, string name)
        {
            return (T)GetPropertyInfo(obj.GetType(), name)?.GetValue(obj, null);
        }
        public static V GetStaticProperty<T, V>(string name)
        {
            return (V)GetPropertyInfo(typeof(T), name, false)?.GetValue(null, null);
        }

        public static void SetProperty<T>(this object obj, string name, T value)
        {
            GetPropertyInfo(obj.GetType(), name)?.SetValue(obj, value, null);
        }
        public static void SetStaticProperty<T, V>(string name, T value)
        {
            GetPropertyInfo(typeof(T), name)?.SetValue(null, value, null);
        }

        public static void CallMethod(this object obj, string methodName, params object[] parameters)
        {
            GetMethodInfo(obj.GetType(), methodName)?.Invoke(obj, parameters);
        }
        public static void CallStaticMethod<T>(string methodName, params object[] parameters)
        {
            GetMethodInfo(typeof(T), methodName)?.Invoke(null, parameters);
        }

        public static T CallMethod<T>(this object obj, string methodName, params object[] parameters)
        {
            return (T)(GetMethodInfo(obj.GetType(), methodName)?.Invoke(obj, parameters) ?? default(T));
        }
        public static V CallStaticMethod<T, V>(string methodName, params object[] parameters)
        {
            return (V)(GetMethodInfo(typeof(T), methodName)?.Invoke(null, parameters) ?? default(V));
        }
    }
}
