using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zat.Shared;
using Zat.Shared.Reflection;

namespace Zat.Unlocker
{
    class Loader
    {
        public void Preload(KCModHelper _helper)
        {
            Debugging.Active = true;
            Debugging.Helper = _helper;
        }

        //After scene loads
        void OnScriptLoad(KCModHelper helper)
        {
            Debugging.Active = true;
            Debugging.Helper = helper;
            try
            {
                //Phase 1: Load dnlib & parse TCS assembly
                Debugging.Log("Loader", "Loading dnlib assembly...");
                var dnlibAssembly = ReflectionUtils.Payload_LoadDnLib();
                if (dnlibAssembly == null) throw new Exception($"{nameof(dnlibAssembly)} null");

                Debugging.Log("Loader", "Loading ModuleDefMD type...");
                var dnlibModuleDefMdType = ReflectionUtils.GetAssemblyTypeByName("dnlib.DotNet.ModuleDefMD", dnlibAssembly);
                if (dnlibModuleDefMdType == null) throw new Exception($"{nameof(dnlibModuleDefMdType)} null");
                
                Debugging.Log("Loader", "Loading ModuleContext type...");
                var dnlibModuleContextType = ReflectionUtils.GetAssemblyTypeByName("dnlib.DotNet.ModuleContext", dnlibAssembly);
                if (dnlibModuleContextType == null) throw new Exception($"{nameof(dnlibModuleContextType)} null");
                
                Debugging.Log("Loader", "Loading UTF8String type...");
                var dnlibUTF8StringType = ReflectionUtils.GetAssemblyTypeByName("dnlib.DotNet.UTF8String", dnlibAssembly);
                if (dnlibUTF8StringType == null) throw new Exception($"{nameof(dnlibUTF8StringType)} null");
                
                Debugging.Log("Loader", "Loading OpCodes type...");
                var dnlibOpCodeTypes = ReflectionUtils.GetAssemblyTypeByName("dnlib.DotNet.Emit.OpCodes", dnlibAssembly);
                if (dnlibOpCodeTypes == null) throw new Exception($"{nameof(dnlibOpCodeTypes)} null");

                Debugging.Log("Loader", "Crafting path to TCS...");
                var tcsAssemblyPath = ReflectionUtils.Path_Combine(UnityEngine.Application.dataPath, "Managed", "Trivial.CodeSecurity.dll");
                if (tcsAssemblyPath == null) throw new Exception($"Failed to build path");
                
                Debugging.Log("Loader", $"Reading TCS from \"{tcsAssemblyPath}\"...");
                var tcsAssemblyRaw = ReflectionUtils.File_ReadAllBytes(tcsAssemblyPath);
                if (tcsAssemblyRaw == null || tcsAssemblyRaw.Length == 0) throw new Exception($"Failed to read TCS null:{tcsAssemblyRaw == null}, len:{tcsAssemblyRaw?.Length ?? -1}");
                
                Debugging.Log("Loader", "Loading TCS with dnlib...");
                var moduleDef = ZatsReflection.CallStaticMethodWithReturn(dnlibModuleDefMdType, "Load", new Type[] { typeof(byte[]), dnlibModuleContextType }, tcsAssemblyRaw, null);
                if (moduleDef == null) throw new Exception($"Failed to load TCS");

                //Phase 2: Get CodeSecurityReport.get_IsSecurityVerified
                Debugging.Log("Loader", "Getting TCS types...");
                var allTypes = (IEnumerable<object>)(moduleDef.CallMethodWithReturn("GetTypes", null));
                if (allTypes == null || allTypes.Count() == 0) throw new Exception($"Failed to acquire TCS types");

                Debugging.Log("Loader", "Getting CSR type...");
                var csrType = allTypes.FirstOrDefault(t => t.GetProperty<string>("FullName") == "Trivial.CodeSecurity.CodeSecurityReport");
                if (csrType == null) throw new Exception($"Failed to find CSR type");

                Debugging.Log("Loader", "Crafting method name...");
                var methodName = ReflectionUtils.Instantiate(dnlibUTF8StringType, "get_IsSecurityVerified");
                if (methodName == null) throw new Exception($"Failed to craft method name");

                Debugging.Log("Loader", "Finding CSR method...");
                var csrMethod = csrType.CallMethodWithReturn("FindMethod", new Type[] { dnlibUTF8StringType }, methodName);
                if (csrMethod == null) throw new Exception($"Failed to find CSR method");

                Debugging.Log("Loader", "Getting CSR properties...");
                var csrMethodBody = csrMethod.GetProperty("Body");
                if (csrMethodBody == null) throw new Exception($"Failed to get CSR body");
                var csrMethodInstructions = csrMethodBody.GetProperty("Instructions");
                if (csrMethodBody == null) throw new Exception($"Failed to get CSR body instructions");

                //Phase 3: Overwrite method body with `return true;`
                Debugging.Log("Loader", "Clearing CSR instructions...");
                csrMethodInstructions.CallMethod("Clear");
                
                Debugging.Log("Loader", "Getting required opcodes...");
                var ldci4 = ZatsReflection.GetStaticField<object>(dnlibOpCodeTypes, "Ldc_I4_1");//dnlibOpCodeType.GetField("Ldc_I4_1", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (ldci4 == null) throw new Exception($"Failed to get ldci4");
                var ret = ZatsReflection.GetStaticField<object>(dnlibOpCodeTypes, "Ret"); //dnlibOpCodeType.GetField("Ret", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (ret == null) throw new Exception($"Failed to get ret");
                var ldci4_ins = ldci4.CallMethodWithReturn("ToInstruction", null);
                if (ldci4_ins == null) throw new Exception($"Failed to build ldci4");
                var ret_ins = ret.CallMethodWithReturn("ToInstruction", null);
                if (ret_ins == null) throw new Exception($"Failed to build ret");


                Debugging.Log("Loader", "Adding opcodes...");
                csrMethodInstructions.CallMethod("Add", ldci4_ins);
                csrMethodInstructions.CallMethod("Add", ret_ins);
                
                Debugging.Log("Loader", "Simplifying branches...");
                csrMethod.CallMethod("SimplifyBranches");

                //Phase 4: Overwrite assembly
                Debugging.Log("Loader", "Writing TCS back to disk...");
                moduleDef.CallMethod("Write", new Type[] { typeof(string) }, tcsAssemblyPath);

                //Done
                Debugging.Log("Loader", "Successfully unlocked mod restrictions!");
            }
            catch (Exception ex)
            {
                Debugging.Log("Loader", $"Failed to unlock mod restrictions: {ex.Message}");
                Debugging.Log("Loader", ex.StackTrace);
            }
        }
    }
}
