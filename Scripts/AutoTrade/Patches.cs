using Assets.Code;
using Assets.Interface;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.Reflection;

namespace Zat.AutoTrade
{
    [HarmonyPatch(typeof(MerchantNotification))]
    [HarmonyPatch("OnShipArrival")]
    public static class HookMerchantNotification
    {
        private static ResourceAmount GetResourceAmount(Dock dock, int sellerId)
        {
            var amt = Player.inst.defaultPayCost;
            var owner = World.GetLandmassOwnerByTeamId(dock.bi.TeamID());
            if (owner != null && owner.teamId != sellerId) amt = owner.GetPayCosts(sellerId);
            return amt;
        }
        static bool Prefix(MerchantNotification __instance)
        {
            Debugging.Log("Hook", "Triggered autotrade in hook");
            if (!Loader.Settings?.Enabled.Value ?? false) return true;
            try
            {
                var ship = GameObject.FindObjectsOfType<MerchantShip>().FirstOrDefault(m => m.isTargetPlayerDock);
                if (!ship)
                {
                    Debugging.Log("Hook", "Could not find ship");
                    return true;
                }

                var dock = ship.GetCurrentDock();
                var amt = GetResourceAmount(dock, ship.TeamID());

                int totalGold = 0;
                var tradeAmount = default(ResourceAmount);
                var resources = (FreeResourceType[])Enum.GetValues(typeof(FreeResourceType));
                foreach (var res in resources.Where(r => r != FreeResourceType.Gold).Where(r => r != FreeResourceType.DeadVillager))
                {
                    var available = ((IResourceStorage)dock.loadingStorageComponent).StoredPublicResources().Get(res);
                    totalGold += amt.Get(res) * available;
                    tradeAmount.Add(ResourceAmount.Make(res, available));
                }
                //Update stats
                var lm = dock.GetComponent<Building>().LandMass();
                if (lm != -1) Player.inst.GetCurrConsumption(lm).amtByShip.Add(tradeAmount);
                Player.inst.GetCurrConsumption(lm).amtByShip.Add(FreeResourceType.Gold, totalGold);

                //Transfer goods/gold
                ship.AddToHold(tradeAmount);
                ((IResourceStorage)dock.loadingStorageComponent).RemoveResources(tradeAmount);
                World.GetLandmassOwner(dock.bi.LandMass()).Gold += totalGold;

                KingdomLog.TryLog("merchantArrive", $"Sold goods for {totalGold} gold at {dock.bi.customName}.", KingdomLog.LogStatus.Neutral, 0f, dock.gameObject, false, lm);

                if (Loader.Settings?.SFX.Value ?? false)
                    SfxSystem.inst.PlayFromBank("ui_merchant_sellto", Camera.main.transform.position, null);

                if (Loader.Settings?.SendAway.Value ?? false)
                    ship.SetField("waitTimer", -1f);
            }
            catch (Exception ex)
            {
                Debugging.Log("Hook", "Raised exception: " + ex.Message);
            }
            return true;
        }
    }
}
