using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zat.Shared.Reflection
{
    public static class ZatsReflection
    {
        public static FieldInfo GetFieldInfo(Type t, string name, bool instance = true)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            if (instance) flags |= BindingFlags.Instance;
            else flags |= BindingFlags.Static;
            return t.GetField(name, flags);
        }
        public static PropertyInfo GetPropertyInfo(Type t, string name, bool instance = true)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            if (instance) flags |= BindingFlags.Instance;
            else flags |= BindingFlags.Static;
            return t.GetProperty(name, flags);
        }
        public static MethodInfo GetMethodInfo(Type t, string name, bool instance = true, Type[] parameterTypes = null)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            if (instance) flags |= BindingFlags.Instance;
            else flags |= BindingFlags.Static;
            var methods = t.GetMethods(flags).Where(m => m.Name == name);
            if (parameterTypes != null)
                methods = methods.Where(m => ParametersMatch(m.GetParameters().Select(pi => pi.ParameterType).ToArray(), parameterTypes));
            return methods.FirstOrDefault(); //t.GetMethod(name, flags);
        }

        private static bool ParametersMatch(Type[] methodParameters, Type[] searchParameters)
        {
            if (methodParameters.Length != searchParameters.Length) return false;
            for (int i = 0; i < methodParameters.Length; i++)
                if (methodParameters[i].FullName != searchParameters[i].FullName) return false;

            return true;
        }

        public static T GetField<T>(this object obj, string name)
        {
            return (T)GetFieldInfo(obj.GetType(), name)?.GetValue(obj);
        }
        public static V GetStaticField<T, V>(string name)
        {
            return (V)GetFieldInfo(typeof(T), name, false)?.GetValue(null);
        }
        public static V GetStaticField<V>(Type type, string name)
        {
            return (V)GetFieldInfo(type, name, false)?.GetValue(null);
        }

        public static void SetField<T>(this object obj, string name, T value)
        {
            GetFieldInfo(obj.GetType(), name)?.SetValue(obj, value);
        }
        public static void SetStaticField<T, V>(string name, V value)
        {
            GetFieldInfo(typeof(T), name, false)?.SetValue(null, value);
        }
        public static void SetStaticField<V>(Type type, string name, V value)
        {
            GetFieldInfo(type, name, false)?.SetValue(null, value);
        }

        public static T GetProperty<T>(this object obj, string name)
        {
            return (T)GetPropertyInfo(obj.GetType(), name)?.GetValue(obj, null);
        }
        public static object GetProperty(this object obj, string name)
        {
            return GetPropertyInfo(obj.GetType(), name)?.GetValue(obj, null);
        }
        public static V GetStaticProperty<T, V>(string name)
        {
            return (V)GetPropertyInfo(typeof(T), name, false)?.GetValue(null, null);
        }
        public static V GetStaticProperty<V>(Type type, string name)
        {
            return (V)GetPropertyInfo(type, name, false)?.GetValue(null, null);
        }

        public static void SetProperty<T>(this object obj, string name, T value)
        {
            GetPropertyInfo(obj.GetType(), name)?.SetValue(obj, value, null);
        }
        public static void SetStaticProperty<T, V>(string name, T value)
        {
            GetPropertyInfo(typeof(T), name)?.SetValue(null, value, null);
        }
        public static void SetStaticProperty<V>(Type type, string name, V value)
        {
            GetPropertyInfo(type, name)?.SetValue(null, value, null);
        }

        public static void CallMethod(this object obj, string methodName, params object[] parameters)
        {
            GetMethodInfo(obj.GetType(), methodName)?.Invoke(obj, parameters);
        }
        public static void CallMethod(this object obj, string methodName, Type[] parameterTypes, params object[] parameters)
        {
            GetMethodInfo(obj.GetType(), methodName, true, parameterTypes)?.Invoke(obj, parameters);
        }
        public static object CallMethodWithReturn(this object obj, string methodName, Type[] parameterTypes, params object[] parameters)
        {
            return GetMethodInfo(obj.GetType(), methodName, true, parameterTypes)?.Invoke(obj, parameters);
        }
        public static void CallStaticMethod<T>(string methodName, params object[] parameters)
        {
            GetMethodInfo(typeof(T), methodName, false)?.Invoke(null, parameters);
        }
        public static void CallStaticMethod(Type type, string methodName, params object[] parameters)
        {
            GetMethodInfo(type, methodName, false)?.Invoke(null, parameters);
        }
        public static void CallStaticMethod(Type type, string methodName, Type[] parameterTypes, params object[] parameters)
        {
            GetMethodInfo(type, methodName, false, parameterTypes)?.Invoke(null, parameters);
        }
        public static object CallStaticMethodWithReturn(Type type, string methodName, Type[] parameterTypes, params object[] parameters)
        {
            return GetMethodInfo(type, methodName, false, parameterTypes)?.Invoke(null, parameters);
        }

        public static T CallMethod<T>(this object obj, string methodName, params object[] parameters)
        {
            return (T)(GetMethodInfo(obj.GetType(), methodName)?.Invoke(obj, parameters) ?? default(T));
        }
        public static V CallStaticMethod<T, V>(string methodName, params object[] parameters)
        {
            return (V)(GetMethodInfo(typeof(T), methodName)?.Invoke(null, parameters) ?? default(V));
        }
        public static V CallStaticMethod<V>(Type type, string methodName, params object[] parameters)
        {
            return (V)(GetMethodInfo(type, methodName)?.Invoke(null, parameters) ?? default(V));
        }
    }
}
