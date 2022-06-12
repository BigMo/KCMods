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
            if (Loader.Settings?.Goods.CharcoalMaker == null || !Loader.Settings.Goods.CharcoalMaker.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Charcoal,
                Loader.Settings.Goods.CharcoalMaker.ModificationMode,
                Loader.Settings.Goods.CharcoalMaker.Factor.Value
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
            if (Loader.Settings?.Food.Baker == null || !Loader.Settings.Food.Baker.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                FreeResourceType.Wheat,
                Loader.Settings.Food.Baker.ModificationMode,
                Loader.Settings.Food.Baker.Factor.Value
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
            if (Loader.Settings?.Food.Field == null || !Loader.Settings.Food.Field.Enabled) return true;

            switch (Loader.Settings.Food.Field.ModificationMode)
            {
                case ResourceManipulation.ModificationMode.Multiply:
                    YieldAmt *= Loader.Settings.Food.Field.Factor.Value;
                    break;
                case ResourceManipulation.ModificationMode.Fixed:
                    YieldAmt += Loader.Settings.Food.Field.Factor.Value;
                    break;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Orchard))]
    [HarmonyPatch("OnYieldResources")]
    public static class PatchOrchard
    {
        static bool Prefix(Orchard __instance, ref float amt, ref FreeResourceType t)
        {
            if (Loader.Settings?.Food.Orchard == null || !Loader.Settings.Food.Orchard.Enabled) return true;


            switch (Loader.Settings.Food.Orchard.ModificationMode)
            {
                case ResourceManipulation.ModificationMode.Multiply:
                    amt *= Loader.Settings.Food.Orchard.Factor.Value;
                    break;
                case ResourceManipulation.ModificationMode.Fixed:
                    amt += Loader.Settings.Food.Orchard.Factor.Value;
                    break;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FishingHut))]
    [HarmonyPatch("AddFish")]
    public static class PatchFishingHut
    {
        static bool Prefix(FishingHut __instance, ref int count)
        {
            if (Loader.Settings?.Food.FishingHut == null || !Loader.Settings.Food.FishingHut.Enabled) return true;


            switch (Loader.Settings.Food.FishingHut.ModificationMode)
            {
                case ResourceManipulation.ModificationMode.Multiply:
                    count *= (int)UnityEngine.Mathf.Ceil(Loader.Settings.Food.FishingHut.Factor.Value);
                    break;
                case ResourceManipulation.ModificationMode.Fixed:
                    count += (int)UnityEngine.Mathf.Ceil(Loader.Settings.Food.FishingHut.Factor.Value);
                    break;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Forester))]
    [HarmonyPatch("AddToWoodPile")]
    public static class PatchForester
    {
        static bool Prefix(FishingHut __instance, ref int num)
        {
            if (Loader.Settings?.Resources.Forester == null || !Loader.Settings.Resources.Forester.Enabled) return true;


            switch (Loader.Settings.Resources.Forester.ModificationMode)
            {
                case ResourceManipulation.ModificationMode.Multiply:
                    num *= (int)UnityEngine.Mathf.Ceil(Loader.Settings.Resources.Forester.Factor.Value);
                    break;
                case ResourceManipulation.ModificationMode.Fixed:
                    num += (int)UnityEngine.Mathf.Ceil(Loader.Settings.Resources.Forester.Factor.Value);
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
                settings = Loader.Settings.Resources.Quarry;
            }
            else if (__instance is IronMine)
            {
                settings = Loader.Settings.Resources.IronMine;
            }
            else
            {
                return true;
            }
            if (!settings?.Enabled) return true;

            ResourceManipulation.SetYield(
                ref Yield,
                settings == Loader.Settings.Resources.Quarry ? FreeResourceType.Stone : FreeResourceType.IronOre,
                settings.ModificationMode,
                settings.Factor.Value
            );
            return true;
        }
    }

    [HarmonyPatch(typeof(ProducerBasePlural))]
    [HarmonyPatch("DoYield")]
    public static class PatchProducerBasePlural
    {
        static bool Prefix(ProducerBasePlural __instance, ref ResourceAmount Yield)
        {
            if (Loader.Settings?.Goods.Blacksmith == null || !Loader.Settings.Goods.Blacksmith.Enabled) return true;

            if (__instance is Blacksmith)
            {
                ResourceManipulation.SetYield(
                    ref Yield,
                    FreeResourceType.Armament,
                    Loader.Settings.Goods.Blacksmith.Armament.ModificationMode,
                    Loader.Settings.Goods.Blacksmith.Armament.Factor.Value
                );
                ResourceManipulation.SetYield(
                    ref Yield,
                    FreeResourceType.Tools,
                    Loader.Settings.Goods.Blacksmith.Tools.ModificationMode,
                    Loader.Settings.Goods.Blacksmith.Tools.Factor.Value
                );
            }
            else
            {
                return true;
            }

            return true;
        }
    }
}
