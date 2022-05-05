﻿using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe;
using SDGraphics;

namespace Ship_Game.Commands.Goals
{
    public class FleetRequisition : Goal
    {
        public const string ID = "FleetRequisition";
        public override string UID => ID;
        private bool Rush; // no need for saving this as it is used immediately

        public FleetRequisition(int id, UniverseState us)
            : base(GoalType.FleetRequisition, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetForFleetRequisition,
                DummyStepTryAgain,
                AddShipToFleetAndMoveToPosition
            };
        }

        public FleetRequisition(string shipName, Empire owner, bool rush)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire      = owner;
            ToBuildUID  = shipName;
            ShipToBuild = ResourceManager.Ships.GetDesign(shipName);
            Rush        = rush;

            Evaluate();
        }

        GoalStep FindPlanetForFleetRequisition()
        {
            if (PlanetBuildingAt == null || !PlanetBuildingAt.HasSpacePort)
            {
                if (!empire.FindPlanetToBuildShipAt(empire.SpacePorts, ShipToBuild, out PlanetBuildingAt))
                    return GoalStep.TryAgain;
            }

            PlanetBuildingAt.Construction.Enqueue(ShipToBuild, this, notifyOnEmpty: false);
            if (Rush)
                PlanetBuildingAt.Construction.MoveToAndContinuousRushFirstItem();

            return GoalStep.GoToNextStep;
        }

        GoalStep AddShipToFleetAndMoveToPosition()
        {
            if (Fleet == null)
            {
                Log.Error($"FleetRequisition {ToBuildUID} complete but Fleet is null!");
                return GoalStep.GoalComplete;
            }
            if (FinishedShip == null)
            {
                Log.Error($"FleetRequisition {ToBuildUID} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }

            foreach (FleetDataNode node in Fleet.DataNodes)
            {
                if (node.GoalId != Id)
                    continue;

                Ship ship = FinishedShip;
                node.Ship = ship;
                node.GoalId = 0;

                if (Fleet.Ships.Count == 0)
                    Fleet.FinalPosition = ship.Position + RandomMath.Vector2D(3000f);
                if (Fleet.FinalPosition == Vector2.Zero)
                    Fleet.FinalPosition = empire.FindNearestRallyPoint(ship.Position).Center;

                Fleet.AddExistingShip(ship,node);
                ship.AI.ResetPriorityOrder(false);
                ship.AI.OrderMoveTo(Fleet.GetFinalPos(ship), ship.Fleet.FinalDirection);

                return GoalStep.GoalComplete;
            } 
            return GoalStep.GoalComplete;
        }
    }
}
