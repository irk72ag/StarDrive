﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class BuildOrbital : Goal
    {
        public const string ID = "BuildOrbital";
        public override string UID => ID;

        public BuildOrbital() : base(GoalType.BuildOrbital)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildConstructor,
                WaitMainGoalCompletion,
                OrderDeployOrbital,
                WaitForDeployment
            };
        }

        public BuildOrbital(Planet planet, string toBuildName, Empire owner) : this()
        {
            ToBuildUID       = toBuildName;
            PlanetBuildingAt = planet;
            empire           = owner;

            Evaluate();
        }

        GoalStep BuildConstructor()
        {
            if (PlanetBuildingAt.Owner != empire)
                return GoalStep.GoalFailed;

            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship orbital))
            {
                Log.Error($"BuildOrbital: no orbital to build with uid={ToBuildUID ?? "null"}");
                return GoalStep.GoalFailed;
            }

            string constructorId = empire.data.ConstructorShip;
            if (!ResourceManager.GetShipTemplate(constructorId, out ShipToBuild))
            {
                Log.Error($"BuildOrbital: no construction ship with uid={constructorId}");
                return GoalStep.GoalFailed;
            }
            PlanetBuildingAt.Construction.AddPlatform(orbital, ShipToBuild, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeployOrbital()
        {
            BuildPosition              = FindNewOrbitalLocation();
            FinishedShip.isConstructor = true;
            FinishedShip.VanityName    = "Construction Ship";
            FinishedShip.AI.OrderDeepSpaceBuild(this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            // FB - must keep this goal until the ship deployed it's structure. 
            // If the goal is not kept, load game construction ships lose the empire goal and get stuck
            return FinishedShip == null ? GoalStep.GoalComplete : GoalStep.TryAgain;
        }

        Vector2 FindNewOrbitalLocation()
        {
            const int ringLimit = ShipBuilder.OrbitalsLimit / 9 + 1; // FB - limit on rings, based on Orbitals Limit
            for (int ring = 0; ring < ringLimit; ring++)
            {
                int degrees    = (int)RandomMath.RandomBetween(0f, 9f);
                float distance = 2000 + 1000 * ring * PlanetBuildingAt.Scale;
                Vector2 pos    = PlanetBuildingAt.Center + MathExt.PointOnCircle(degrees * 40, distance);
                if (!IsOrbitalAlreadyPresentAt(pos) && !IsOrbitalPlannedAt(pos))
                    return pos;

                for (int i = 0; i < 9; i++) // FB - 9 orbitals per ring
                {
                    pos = PlanetBuildingAt.Center + MathExt.PointOnCircle(i * 40, distance);
                    if (!IsOrbitalAlreadyPresentAt(pos))
                        return pos;
                }
            }

            return PlanetBuildingAt.Center; // There is a limit on orbitals number
        }

        bool IsOrbitalAlreadyPresentAt(Vector2 position)
        {
            foreach (Ship orbital in PlanetBuildingAt.OrbitalStations.Values)
            {
                Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                    orbital.Position, orbital.Radius, Color.LightCyan, 10.0f);
                if (position.InRadius(orbital.Position, orbital.Radius))
                    return true;
            }

            return false;
        }

        // Checks if a Construction Ship is due to deploy a structure at a point
        bool IsOrbitalPlannedAt(Vector2 position)
        {
            foreach (Ship ship in empire.GetShips())
            {
                ShipAI.ShipGoal g = ship.AI.OrderQueue.PeekFirst;
                if (g != null && g.Plan == ShipAI.Plan.DeployOrbital && g.Goal.PlanetBuildingAt == PlanetBuildingAt)
                {
                    if (position.InRadius(g.Goal.BuildPosition, 400))
                        return true;
                }
            }

            return false;
        }
    }
}