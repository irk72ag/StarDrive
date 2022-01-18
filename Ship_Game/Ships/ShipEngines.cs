﻿using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public enum WarpStatus
    {
        // This ship is not able to warp because of damage, inhibition, EMP damage, ...
        UnableToWarp,

        // This ship is waiting for other ships in the formation, or is recalling fighters
        WaitingOrRecalling,

        // Ship is completely ready to warp
        ReadyToWarp,

        // DO NOT ADD ANY MORE STATES HERE, IT WILL BREAK ALL WARP STATUS LOGIC 100%
    }

    public enum EngineStatus
    {
        // All engines on this ship have been destroyed or disabled by EMP
        Disabled,

        // Engines are up and running
        Active,
    }

    public class ShipEngines
    {
        Ship Owner;
        ShipAI AI => Owner.AI;

        public ShipModule[] Engines { get; private set; }
        public ShipModule[] ActiveEngines => Engines.Filter(e=> e.Active);

        public EngineStatus EngineStatus { get; private set; }
        public WarpStatus ReadyForWarp { get; private set; }
        public WarpStatus ReadyForFormationWarp { get; private set; }

        public override string ToString() => $"Status:{EngineStatus} Warp:{ReadyForWarp} FWarp:{ReadyForFormationWarp}";

        public ShipEngines(Ship owner, ShipModule[] slots)
        {
            Owner   = owner;
            Engines = slots.Filter(module => module.Is(ShipModuleType.Engine));
        }

        public void Dispose()
        {
            Owner = null;
            Engines = null;
        }

        public void Update()
        {
            // These need to be done in order
            EngineStatus = GetEngineStatus();
            ReadyForWarp = GetWarpReadyStatus();
            ReadyForFormationWarp = GetFormationWarpReadyStatus();
        }

        EngineStatus GetEngineStatus()
        {
            if (Owner.EnginesKnockedOut || Owner.Inhibited || Owner.EMPDisabled ||
                Owner.Dying || !Owner.HasCommand)
                return EngineStatus.Disabled;

            var engines = Engines;
            for (int i = 0; i < engines.Length; ++i)
            {
                ShipModule e = engines[i];
                if (e.Active && e.Powered)
                    return EngineStatus.Active;
            }

            // no engines are powered
            return EngineStatus.Disabled;
        }

        WarpStatus GetFormationWarpReadyStatus()
        {
            if (Owner.Fleet == null || Owner.AI.State != AIState.FormationWarp) 
                return ReadyForWarp;

            if (!Owner.CanTakeFleetMoveOrders())
                return ReadyForWarp;

            if (Owner.engineState == Ship.MoveState.Warp && ReadyForWarp < WarpStatus.ReadyToWarp)
                return ReadyForWarp;

            if (Owner.AI.State != AIState.AwaitingOrders &&
                !Owner.Fleet.IsShipAtFinalPosition(Owner, 2000))
            {
                Vector2 movePosition;
                if (AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.MovePosition != Vector2.Zero)
                    movePosition = goal.MovePosition;
                else
                    movePosition = Owner.Fleet.GetFinalPos(Owner);

                float facingFleetDirection = Owner.AngleDifferenceToPosition(movePosition);
                // WARNING: BE EXTREMELY CAREFUL WITH THIS ANGLE HERE,
                //          IF YOU MAKE IT TOO SMALL, FORMATION WARP WILL NOT WORK!
                if (facingFleetDirection > 0.05f)
                    return WarpStatus.WaitingOrRecalling;
            }
            return ReadyForWarp;
        }

        WarpStatus GetWarpReadyStatus()
        {
            if (EngineStatus == EngineStatus.Disabled || !Owner.Active || Owner.MaxFTLSpeed < 1)
                return WarpStatus.UnableToWarp;

            if (Owner.engineState == Ship.MoveState.Warp)
                return WarpStatus.ReadyToWarp;

            if (Owner.Carrier.RecallingFighters())
                return WarpStatus.WaitingOrRecalling;

            if (!Owner.IsWarpRangeGood(10000f))
                return WarpStatus.UnableToWarp;

            return WarpStatus.ReadyToWarp;
        }
    }
}
