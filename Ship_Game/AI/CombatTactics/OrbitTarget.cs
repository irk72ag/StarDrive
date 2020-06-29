﻿using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;
using Ship_Game.AI.ShipMovement.CombatManeuvers;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class OrbitTarget : OrbitPlan
    {
        public OrbitTarget(ShipAI ai, OrbitDirection direction) : base(ai, direction)
        {
        }

        protected override void OverrideCombatValues(float elapsedTime)
        {
            DesiredCombatRange = AI.Owner.DesiredCombatRange * 0.8f - AI.Owner.Radius - AI.Target.Radius;
        }

        protected override CombatMoveState ExecuteAttack(float elapsedTime)
        {
            Vector2 predictedTarget = AI.Owner.PredictImpact(AI.Target);
            
            UpdateOrbitPos(predictedTarget, DesiredCombatRange, elapsedTime);
            AI.ThrustOrWarpToPos(OrbitPos, elapsedTime);
            return CombatMoveState.OrbitInjection;
        }
    }
}
