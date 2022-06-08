﻿using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateDirectorRaid : Goal
    {
        public const string ID = "PirateDirectorRaid";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateDirectorRaid(int id, UniverseState us)
            : base(GoalType.PirateDirectorRaid, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
               PrepareRaid
            };
        }
        public PirateDirectorRaid(Empire owner, Empire targetEmpire)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Raid Director vs. {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        GoalStep PrepareRaid()
        {
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
            {
                Log.Info(ConsoleColor.Green, $"---- Pirates: {empire.Name} Raid Director vs. {TargetEmpire.Name}, They paid, terminating ----");
                return GoalStep.GoalFailed; // We got paid or they are gone, Raid Director can go on vacation
            }

            float startChance = RaidStartChance();
            if (RandomMath.RollDice(startChance))
            {
                GoalType raid  = GetRaid();
                switch (raid)
                {
                    case GoalType.PirateRaidTransport:  Pirates.AddGoalRaidTransport(TargetEmpire);      break;
                    case GoalType.PirateRaidOrbital:    Pirates.AddGoalRaidOrbital(TargetEmpire);        break;
                    case GoalType.PirateRaidProjector:  Pirates.AddGoalRaidProjector(TargetEmpire);      break;
                    case GoalType.PirateRaidCombatShip: Pirates.AddGoalRaidCombatShip(TargetEmpire);     break;
                }
            }

            return GoalStep.TryAgain;
        }

        int RaidStartChance()
        {
            if (!Pirates.CanDoAnotherRaid(out int numRaids))
                return 0; // Limit maximum of concurrent raids

            int startChance = Pirates.Level.LowerBound((int)UState.Difficulty + 1);
            startChance     = (startChance / EmpireManager.PirateFactions.Length.LowerBound(1)).LowerBound(1);
            startChance    /= numRaids + 1;

            //return 100; // For testing
            return startChance.UpperBound(Pirates.ThreatLevelFor(TargetEmpire));
        }

        GoalType GetRaid()
        {
            int raid = RandomMath.RollDie(Pirates.Level.UpperBound(Pirates.ThreatLevelFor(TargetEmpire)));

            switch (raid)
            {
                default:
                case 1:
                case 2:  return GoalType.PirateRaidTransport;
                case 3:  return GoalType.PirateRaidProjector;
                case 4:  return GoalType.PirateRaidTransport;
                case 5:  return GoalType.PirateRaidTransport;
                case 6:  return GoalType.PirateRaidProjector;
                case 7:  return GoalType.PirateRaidOrbital;
                case 8:  return GoalType.PirateRaidCombatShip;
                case 9:  return GoalType.PirateRaidOrbital;
                case 10: return GoalType.PirateRaidProjector;
                case 11: return GoalType.PirateRaidCombatShip;
                case 12: return GoalType.PirateRaidOrbital;
                case 13: return GoalType.PirateRaidCombatShip;
                case 14: return GoalType.PirateRaidTransport;
                case 15: return GoalType.PirateRaidProjector;
                case 16: return GoalType.PirateRaidCombatShip;
                case 17: return GoalType.PirateRaidTransport;
                case 18: return GoalType.PirateRaidOrbital;
                case 19: return GoalType.PirateRaidCombatShip;
            }
        }
    }
}