using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Zat.Commander
{
    public class CommandUnit
    {
        public enum UnitType { None = 0, Soldier = 1, Archer = 2, TroopTransportShip = 4 };

        public UnitSystem.Army Army { get; private set; }
        public ShipBase Ship { get; private set; }
        public TroopTransportShip TransportShip { get; private set; }

        public CommandUnit(UnitSystem.Army army)
        {
            this.Army = army;
        }
        public CommandUnit(ShipBase ship)
        {
            this.Ship = ship;
            TransportShip = ship?.GetComponent<TroopTransportShip>();
        }

        public UnitType Type {
            get {
                if (Army != null) return (Army.armyType == UnitSystem.ArmyType.Default ? UnitType.Soldier : UnitType.Archer);
                if (Ship != null && TransportShip != null) return UnitType.TroopTransportShip;
                return UnitType.None;
            }
        }

        public bool Valid
        {
            get
            {
                switch (Type)
                {
                    case UnitType.Archer:
                    case UnitType.Soldier:
                        return UnitSystem.inst.IsAlive(Army);
                    case UnitType.TroopTransportShip:
                        return Ship.state != ShipBase.State.Sinking && Ship.life > 0;
                    default:
                        return false;
                }
            }
        }

        public IMoveableUnit MoveableUnit
        {
            get
            {
                switch (Type)
                {
                    case UnitType.Archer:
                    case UnitType.Soldier:
                        return Army;
                    case UnitType.TroopTransportShip:
                        return TransportShip;
                    default:
                        return null;
                }
            }
        }
        public ISelectable Selectable
        {
            get
            {
                switch (Type)
                {
                    case UnitType.Archer:
                    case UnitType.Soldier:
                        return Army;
                    case UnitType.TroopTransportShip:
                        return TransportShip;
                    default:
                        return null;
                }
            }
        }

        public Guid Guid { get { return MoveableUnit.GetGuid(); } }
        public float Health { get { return MoveableUnit.CurrHealth(); } }
        public float MaxHealth { get { return MoveableUnit.MaxHealth(); } }
        public int TeamID { get { return MoveableUnit.TeamID(); } }
        public Vector3 Position { get { return MoveableUnit.GetPos(); } }
    }
}
