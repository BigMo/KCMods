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
        public static readonly CommandGroup Empty = new CommandGroup(new CommandUnit[0]);

        public bool HasUnits { get { return units != null && units.Length > 0; } }
        public IEnumerable<CommandUnit> Units
        { 
            get 
            { 
                return (units != null && units.Length > 0) ? 
                    units.Where(u => u !=null && u.Valid) :
                    Enumerable.Empty<CommandUnit>();
            }
        }
        
        public IEnumerable<Guid> Guids { get { return Units.Select(a => a.Guid); } }
        public IEnumerable<Vector3> Positions { get { return Units.Select(u => u.Position); } }
        public float Health
        {
            get
            {
                if (!HasUnits) return 0f;
                var max = Units.Sum(u => u.MaxHealth);
                var cur = Units.Sum(u => u.Health);
                if (max == 0) return 0f;
                return cur / max;
            }
        }
        public float Count { get { return units.Length; } }
        public CommandUnit.UnitType Type
        {
            get
            {
                var type = CommandUnit.UnitType.None;
                foreach (var unit in Units) type |= unit.Type;
                return type;
            }
        }

        private CommandUnit[] units;

        public CommandGroup(CommandUnit[] units)
        {
            this.units = units;
        }

        /// <summary>
        /// Returns true if armies were removed (e.g. died)
        /// </summary>
        /// <returns></returns>
        public bool CheckForInvalidArmies()
        {
            var validUnits = new CommandUnit[units.Length];
            var validCount = 0;
            for (int i = 0; i < units.Length; i++)
                if (units[i].Valid) validUnits[validCount++] = units[i];
            
            if (units.Length != validCount)
            {
                units = new CommandUnit[validCount];
                Array.Copy(validUnits, 0, units, 0, validCount);
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
            return units.Any(a => a.Guid == guid);
        }
        public CommandGroup ReduceBy(IEnumerable<Guid> armiesToRemove)
        {
            return new CommandGroup(units.Where(a => !armiesToRemove.Any(rem => rem == a.Guid)).ToArray());
        }
        public void Select()
        {
            try
            {
                if (!HasUnits) return;
                var highlightedObjs = GameUI.inst?.GetField<List<ISelectable>>("highlightedObjs");
                if (highlightedObjs == null) return;
                highlightedObjs.Clear();
                //highlightedObjs.AddRange(armies);
                var selectedObjs = GameUI.inst?.GetField<List<ISelectable>>("selectedObjs");
                if (selectedObjs == null) return;
                selectedObjs.Clear();
                foreach (var unit in units) GameUI.inst.AddToSelected(unit.Selectable);
                GameUI.inst.CallMethod("SelectCell", null, true, false);
                GameUI.inst.CallMethod("CalculateFormationOffsets");

                var army = Units.FirstOrDefault(u => u.Type == CommandUnit.UnitType.Archer || u.Type == CommandUnit.UnitType.Soldier);
                var ship = Units.FirstOrDefault(u => u.Type == CommandUnit.UnitType.TroopTransportShip);
                if (army != null)
                {
                    GameUI.inst.generalLargeUI.SetSelectedArmy(army.Army);
                    GameUI.inst.generalLargeUI.gameObject.SetActive(true);
                }
                if (ship != null)
                {
                    GameUI.inst.shipUI.SetSelectedShip(ship.Ship);
                    GameUI.inst.shipUI.gameObject.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                Debugging.Log("CommandGroup", $"Failed to select armies: {ex.Message}");
                Debugging.Log("CommandGroup", ex.StackTrace);
            }
        }
        public void MoveCamera()
        {
            if (!HasUnits) return;
            var positions = Positions.ToArray();
            var positionsArr = new ArrayExt<Vector3>(positions.Length);
            foreach (var pos in positions) positionsArr.Add(pos);
            Cam.inst.BringIntoView(positions[0], positionsArr);
        }
    }
}
