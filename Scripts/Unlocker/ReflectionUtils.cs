using System;
using System.Reflection;
using Zat.Shared.Reflection;

namespace Zat.Unlocker
{
    public class ReflectionUtils
    {
        private const string DNLIB_ASSEMBLY_QUALIFIED_NAME = "dnlib, Version=3.3.2.0, Culture=neutral, PublicKeyToken=50e96378b6e77999";

        public static Type GetTypeByName(string name)
        {
            return Type.GetType(name);
        }

        public static Type GetAssemblyTypeByName(string typeName, string assemblyQualifiedName)
        {
            return Type.GetType($"{typeName}, {assemblyQualifiedName}");
        }

        public static Type GetAssemblyTypeByName(string typeName, Assembly assembly)
        {
            return Type.GetType($"{typeName}, {assembly.FullName}");
        }

        public static Type GetDnLibTypeByName(string typeName)
        {
            return GetAssemblyTypeByName(typeName, DNLIB_ASSEMBLY_QUALIFIED_NAME);
        }

        public static byte[] File_ReadAllBytes(string path)
        {
            var tFile = GetTypeByName("System.IO.File");
            if (tFile == null) throw new Exception("Could not find System.IO.File");
            var data = (byte[])ZatsReflection.CallStaticMethodWithReturn(tFile, "ReadAllBytes", new Type[] { typeof(string) }, path);
            return data;
        }

        public static string Path_Combine(params string[] paths)
        {
            var tPath = GetTypeByName("System.IO.Path");
            if (tPath == null) throw new Exception("Could not find System.IO.Path");
            var path = ZatsReflection.CallStaticMethodWithReturn(tPath, "Combine", new Type[] { typeof(string[])}, new object[] { paths }) as string;
            return path;
        }

        public static object Assembly_LoadAssembly(string path)
        {
            var data = File_ReadAllBytes(path);
            return Assembly_LoadAssembly(data);
        }

        public static Assembly Assembly_LoadAssembly(byte[] rawbytes)
        {
            var tAssembly = typeof(Assembly);
            var assembly = ZatsReflection.CallStaticMethodWithReturn(tAssembly, "Load", new Type[] { typeof(byte[]) }, rawbytes) as Assembly;
            return assembly;
        }

        public static Assembly Payload_LoadDnLib()
        {
            return Assembly_LoadAssembly(Payloads.dnlibdll);
        }

        public static object Instantiate(Type t, params object[] parameters)
        {
            return Activator.CreateInstance(t, parameters);
        }
    }
}
