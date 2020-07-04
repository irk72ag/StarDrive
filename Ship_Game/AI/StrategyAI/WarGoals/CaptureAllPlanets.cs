﻿using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class CaptureAllPlanets : Campaign
    {
        SolarSystem CurrentTarget;

        /// <summary>
        /// Initializes from save a new instance of the <see cref="CaptureAllPlanets"/> class.
        /// </summary>
        public CaptureAllPlanets(Campaign campaign, War war) : base(campaign, war) => CreateSteps();

        public CaptureAllPlanets(CampaignType campaignType, War war) : base(campaignType, war)
        {
            CreateSteps();
        }

        void CreateSteps()
        {
            Steps = new Func<GoalStep>[] 
            {
                SetTargets,
                SetupRallyPoint,
                AttackSystems
            };
        }

        GoalStep SetTargets()
        {
            Vector2 empireCenter = Owner.GetWeightedCenter();
            AddTargetSystems(Them.GetOwnedSystems().Filter(s => s.IsExploredBy(Owner)));

            AddTargetSystems(OwnerWar.GetHistoricLostSystems().Filter(s => s.OwnerList.Contains(Them) && !s.OwnerList.Contains(Owner)));

            if (TargetSystems.IsEmpty) return GoalStep.GoalFailed;

            return GoalStep.GoToNextStep;
        }

        void UpdateTargetSystemList()
        {
            for (int x = 0; x < TargetSystems.Count; x++)
            {
                var s = TargetSystems[x];
                if (s.OwnerList.Contains(Them))
                    continue;
                TargetSystems.RemoveAt(x);
            }
        }

        GoalStep AttackSystems()
        {
            if (Owner.GetOwnedSystems().Count == 0) return GoalStep.GoalFailed;
            UpdateTargetSystemList();
            if (HaveConqueredTargets()) return GoalStep.GoalComplete;
            if (TargetSystems.IsEmpty) return GoalStep.TryAgain;
            var fleets        = Owner.AllFleetsReady();
            int priorityMod   = 0;
            float strength    = fleets.AccumulatedStrength;

            if (Owner.FindNearestOwnedSystemTo(TargetSystems, out SolarSystem nearestSystem))
                TargetSystems.Sort(s => s.Position.SqDist(nearestSystem.Position));

            var tasks = new WarTasks(Owner, Them);
            foreach(var system in TargetSystems)
            {
                if (!HaveConqueredTarget(system))
                {
                    float defense = Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(system.Position, Owner.GetProjectorRadius(), Owner);
                    strength -= defense *2;

                    float distanceToCenter = system.Position.SqDist(nearestSystem.Position);
                    tasks.StandardAssault(system, OwnerWar.Priority() + priorityMod, 2);
                }
                if (strength < 0) break;
                priorityMod++;
            }
            Owner.GetEmpireAI().AddPendingTasks(tasks.GetNewTasks());
            return GoalStep.RestartGoal;
        }

        bool HaveConqueredTargets()
        {
            foreach(var system in TargetSystems)
            {
                if (!HaveConqueredTarget(system)) 
                    return false;
            }
            return true;
        }

        bool HaveConqueredTarget(SolarSystem system) => !system.OwnerList.Contains(Them);
    }
}