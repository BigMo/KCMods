using Assets.Code;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zat.Shared.Reflection;

namespace Zat.Productivity.Buildings
{
    [HarmonyPatch(typeof(CharcoalMaker))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchCharcoalMaker
    {
        static bool Prefix(CharcoalMaker __instance, ref ResourceAmount Yield)
        {
            if (Loader.Settings?.CharcoalMaker == null || !Loader.Settings.CharcoalMaker.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Charcoal,
                Loader.Settings.CharcoalMaker.ModificationMode,
                Loader.Settings.CharcoalMaker.Factor.Value
            );
            return true;
        }
    }

    [HarmonyPatch(typeof(Baker))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchBaker
    {
        static bool Prefix(Baker __instance, ref ResourceAmount Yield)
        {
            if (Loader.Settings?.Baker == null || !Loader.Settings.Baker.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Wheat,
                Loader.Settings.Baker.ModificationMode,
                Loader.Settings.Baker.Factor.Value
            );
            return true;
        }
    }

    [HarmonyPatch(typeof(Field))]
    [HarmonyPatch("DeferredYield")]
    public static class PatchField
    {
        static bool Prefix(Field __instance)
        {
            var Yield = __instance.GetField<ResourceAmount>("deferredYield");
            if (Loader.Settings?.Field == null || !Loader.Settings.Field.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Wheat,
                Loader.Settings.Field.ModificationMode,
                Loader.Settings.Field.Factor.Value
            );
            __instance.SetField("deferredYield", Yield);
            return true;
        }
    }

    [HarmonyPatch(typeof(IronMine))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchIronMine
    {
        static bool Prefix(Field __instance, ref ResourceAmount Yield)
        {
            if (Loader.Settings?.IronMine == null || !Loader.Settings.IronMine.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.IronOre,
                Loader.Settings.IronMine.ModificationMode,
                Loader.Settings.IronMine.Factor.Value
            );
            return true;
        }
    }

    [HarmonyPatch(typeof(Orchard))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchOrchard
    {
        static bool Prefix(Field __instance, ref ResourceAmount Yield)
        {
            if (Loader.Settings?.Orchard == null || !Loader.Settings.Orchard.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Apples,
                Loader.Settings.Orchard.ModificationMode,
                Loader.Settings.Orchard.Factor.Value
            );
            return true;
        }
    }

    [HarmonyPatch(typeof(Quarry))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchQuarry
    {
        static bool Prefix(Field __instance, ref ResourceAmount Yield)
        {
            if (Loader.Settings?.Quarry == null || !Loader.Settings.Quarry.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Stone,
                Loader.Settings.Quarry.ModificationMode,
                Loader.Settings.Quarry.Factor.Value
            );
            return true;
        }
    }
}
