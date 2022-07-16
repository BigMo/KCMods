using Assets.Code;
using System;
using UnityEngine;
using Zat.Shared.Reflection;
using static World;

namespace Zat.Productivity
{
    public class ResourceManipulation
    {
        public enum ModificationMode
        {
            Multiply = 0,
            Fixed = 1
        };
        public static string[] MODES = Enum.GetNames(typeof(ModificationMode));
        public static void SetYield(ref ResourceAmount yield, FreeResourceType type, ModificationMode mode, float num)
        {
            var count = yield.Get(type);
            switch (mode)
            {
                case ModificationMode.Fixed:
                    count = (int)Mathf.Ceil(num);
                    break;
                case ModificationMode.Multiply:
                    count = (int)Mathf.Ceil(count * num);
                    break;
            }
            yield.Set(type, count);
        }

        public static int GetTeamId(Building b)
        {
            return b?.TeamID() ?? int.MinValue;
        }
        public static int GetTeamId(Forester f)
        {
            return GetTeamId(f.GetField<Building>("b"));
        }
        public static int GetTeamId(ProducerBase b)
        {
            return GetTeamId(b.GetField<Building>("b"));
        }
        public static int GetTeamId(ProducerBasePlural b)
        {
            return GetTeamId(b.GetField<Building>("b"));
        }
        public static int GetTeamId(FishingHut f)
        {
            return GetTeamId(f.GetField<Building>("b"));
        }
        public static int GetTeamId(Orchard f)
        {
            return GetTeamId(f.GetField<Building>("b"));
        }
        public static int GetTeamId(Field f)
        {
            return GetTeamId(f.GetField<Building>("b"));
        }
        public static int GetTeamId(CharcoalMaker f)
        {
            return GetTeamId(f.GetField<Building>("b"));
        }
        public static int GetTeamId(Baker f)
        {
            return GetTeamId(f.GetField<Building>("b"));
        }
        public static Relations GetRelation(int teamId)
        {
            return World.inst.RelationBetween(teamId, 0);
        }
    }
}
