using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Zat.Shared;
using Zat.Shared.Reflection;

namespace Zat.Commander
{
    public class CommandGroup
    {
        public static readonly CommandGroup Empty = new CommandGroup(new UnitSystem.Army[0]);
        public enum GroupType { None, Soldiers, Archers, Mixed };

        public bool HasArmies { get { return armies != null && armies.Length > 0; } }
        public IEnumerable<UnitSystem.Army> Armies { get { return armies; } }
        public IEnumerable<Guid> Guids { get { return armies.Select(a => a.guid); } }
        public IEnumerable<Vector3> Positions { get { return armies.Select(u => u.generalPos); } }
        public float MaxHealth { get { return armies.Sum(a => a.MaxHealth()); } }
        public float CurrHealth { get { return armies.Sum(a => a.CurrHealth()); } }
        public float Health { get { return HasArmies ? (CurrHealth / MaxHealth) : 0; } }
        public float Count { get { return armies.Length; } }
        public GroupType Type
        {
            get
            {
                if (armies == null || armies.Length == 0) return GroupType.None;
                var hasSoldiers = armies.Any(a => a.armyType == UnitSystem.ArmyType.Default);
                var hasArchers = armies.Any(a => a.armyType == UnitSystem.ArmyType.Archer);
                if (hasSoldiers && hasArchers) return GroupType.Mixed;
                if (hasSoldiers) return GroupType.Soldiers;
                if (hasArchers) return GroupType.Archers;
                return GroupType.None;
            }
        }

        private UnitSystem.Army[] armies;

        public CommandGroup(UnitSystem.Army[] armies)
        {
            this.armies = armies;
        }

        /// <summary>
        /// Returns true if armies were removed (e.g. died)
        /// </summary>
        /// <returns></returns>
        public bool CheckForInvalidArmies()
        {
            var validArmies = new UnitSystem.Army[armies.Length];
            var validCount = 0;
            for (int i = 0; i < armies.Length; i++)
                if (UnitSystem.inst.IsAlive(armies[i]))
                    validArmies[validCount++] = armies[i];
            if (armies.Length != validCount)
            {
                armies = new UnitSystem.Army[validCount];
                Array.Copy(validArmies, 0, armies, 0, validCount);
                return true;
            }
            return false;
        }
        public bool Contains(UnitSystem.Army other)
        {
            return Contains(other.guid);
        }
        public bool Contains(Guid guid)
        {
            return armies.Any(a => a.guid == guid);
        }
        public CommandGroup ReduceBy(IEnumerable<Guid> armiesToRemove)
        {
            return new CommandGroup(armies.Where(a => !armiesToRemove.Any(rem => rem == a.guid)).ToArray());
        }
        public void Select()
        {
            if (!HasArmies) return;
            var selectedObjs = GameUI.inst?.GetField<List<ISelectable>>("selectedObjs");
            if (selectedObjs == null) return;
            selectedObjs.Clear();
            foreach (var army in armies) GameUI.inst.AddToSelected(army);
        }
        public void MoveCamera()
        {
            Debugging.Log("CommandGroup", "MoveCamera");
            if (!HasArmies) return;
            var positions = Positions.ToArray();
            var positionsArr = new ArrayExt<Vector3>(positions.Length);
            foreach (var pos in positions) positionsArr.Add(pos);
            Cam.inst.BringIntoView(positions[0], positionsArr);
        }
    }
}
