using Assets.Code;
using Harmony;
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
    [HarmonyPatch("OnYieldResources")]
    public static class PatchField
    {
        static bool Prefix(Field __instance, ref float YieldAmt, FreeResourceType t)
        {
            if (Loader.Settings?.Field == null || !Loader.Settings.Field.Enabled) return true;

            switch (Loader.Settings.Field.ModificationMode)
            {
                case ResourceManipulation.ModificationMode.Multiply:
                    YieldAmt *= Loader.Settings.Field.Factor.Value;
                    break;
                case ResourceManipulation.ModificationMode.Fixed:
                    YieldAmt += Loader.Settings.Field.Factor.Value;
                    break;
            }
            return true;
        }
    }

    //[HarmonyPatch(typeof(IronMine))]
    //[HarmonyPatch("OnYieldResources")]
    //public static class PatchIronMine
    //{
    //    static bool Prefix(Field __instance, ref ResourceAmount Yield)
    //    {
    //        if (Loader.Settings?.IronMine == null || !Loader.Settings.IronMine.Enabled) return true;

    //        ResourceManipulation.SetYield(
    //            ref Yield,
    //            FreeResourceType.IronOre,
    //            Loader.Settings.IronMine.ModificationMode,
    //            Loader.Settings.IronMine.Factor.Value
    //        );
    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(Orchard))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchOrchard
    {
        static bool Prefix(Orchard __instance, ref float amt, ref FreeResourceType t)
        {
            if (Loader.Settings?.Orchard == null || !Loader.Settings.Orchard.Enabled) return true;

            
            switch(Loader.Settings.Orchard.ModificationMode)
            {
                case ResourceManipulation.ModificationMode.Multiply:
                    amt *= Loader.Settings.Orchard.Factor.Value;
                    break;
                case ResourceManipulation.ModificationMode.Fixed:
                    amt += Loader.Settings.Orchard.Factor.Value;
                    break;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(ProducerBase))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchProducerBase
    {
        static bool Prefix(ProducerBase __instance, ref ResourceAmount Yield)
        {
            BuildingSettings settings;
            if (__instance is Quarry)
            {
                settings = Loader.Settings.Quarry;
            }
            else if (__instance is IronMine)
            {
                settings = Loader.Settings.IronMine;
            }
            else
            {
                return true;
            }
            if (!settings?.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                settings == Loader.Settings.Quarry ? FreeResourceType.Stone : FreeResourceType.IronOre,
                settings.ModificationMode,
                settings.Factor.Value
            );
            return true;
        }
    }

    //[HarmonyPatch(typeof(Quarry))]
    //[HarmonyPatch("OnYieldResources")]
    //public static class PatchQuarry
    //{
    //    static bool Prefix(Field __instance, ref ResourceAmount Yield)
    //    {
    //        if (Loader.Settings?.Quarry == null || !Loader.Settings.Quarry.Enabled) return true;

    //        ResourceManipulation.SetYield(
    //            ref Yield,
    //            FreeResourceType.Stone,
    //            Loader.Settings.Quarry.ModificationMode,
    //            Loader.Settings.Quarry.Factor.Value
    //        );
    //        return true;
    //    }
    //}
}
