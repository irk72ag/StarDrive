﻿using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateDirectorPayment : Goal
    {
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        Pirates Pirates => Owner.Pirates;

        [StarDataConstructor]
        public PirateDirectorPayment(Empire owner) : base(GoalType.PirateDirectorPayment, owner)
        {
            Steps = new Func<GoalStep>[]
            {
               UpdatePaymentStatus,
               UpdatePirateActivity,
            };
        }

        public PirateDirectorPayment(Empire owner, Empire targetEmpire) : this(owner)
        {
            TargetEmpire = targetEmpire;
            if (Pirates.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Pirates: New {Owner.Name} Payment Director for {TargetEmpire.Name} ----");
        }

        GoalStep UpdatePaymentStatus()
        {
            if (Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed;

            int victimPlanets = TargetEmpire.GetPlanets().Count;

            if (victimPlanets > Pirates.MinimumColoniesForPayment
                || victimPlanets == Pirates.MinimumColoniesForPayment 
                && Owner.Random.RollDice(10))
            {
                return RequestPayment() ? GoalStep.GoToNextStep : GoalStep.TryAgain;
            }

            return GoalStep.TryAgain; // Too small for now
        }

        GoalStep UpdatePirateActivity()
        {
            if (Pirates.PaidBy(TargetEmpire))
            {
                // Ah, so they paid us,  we can use this money to expand our business 
                Pirates.TryLevelUp(TargetEmpire.Universe);
                Pirates.ResetThreatLevelFor(TargetEmpire);
                Pirates.Owner.SignTreatyWith(TargetEmpire, Gameplay.TreatyType.NonAggression);
            }
            else
            {
                // They did not pay! We will raid them
                Pirates.IncreaseThreatLevelFor(TargetEmpire);
                if (!Pirates.Owner.AI.HasGoal(g => g.Type == GoalType.PirateDirectorRaid && g.TargetEmpire == TargetEmpire))
                     Pirates.AddGoalDirectorRaid(TargetEmpire);
            }

            return GoalStep.RestartGoal;
        }

        bool RequestPayment()
        {
            if (GlobalStats.RestrictAIPlayerInteraction && TargetEmpire.isPlayer)
                return false;

            // If the timer is done, the pirates will demand new payment or immediately if the threat level is -1 (initial)
            if (Pirates.PaymentTimerFor(TargetEmpire) > 0 && Pirates.ThreatLevelFor(TargetEmpire) > -1)
            {
                Pirates.DecreasePaymentTimerFor(TargetEmpire);
                return false;
            }

            // If the player did not pay, don't ask for another payment, let them crawl to
            // us when they are ready to pay and increase out threat level to them
            if (!Pirates.PaidBy(TargetEmpire) && TargetEmpire.isPlayer && Pirates.ThreatLevelFor(TargetEmpire) > -1)
            {
                Pirates.IncreaseThreatLevelFor(TargetEmpire);
                Pirates.ResetPaymentTimerFor(TargetEmpire);
                return false;
            }

            // They Paid at least once  (or it's our first demand), so we can continue milking money fom them
            Log.Info(ConsoleColor.Green,$"Pirates: {Owner.Name} Payment Director - Demanding payment from {TargetEmpire.Name}");

            if (!Pirates.Owner.IsKnown(TargetEmpire))
            {
                Empire.SetRelationsAsKnown(Pirates.Owner, TargetEmpire);
            }

            if (TargetEmpire.isPlayer)
                Encounter.ShowEncounterPopUpFactionInitiated(Pirates.Owner, Owner.Universe.Screen);
            else
                DemandMoneyFromAI();

            // We demanded payment for the first time, let the game begin
            if (Pirates.ThreatLevelFor(TargetEmpire) == -1)
                Pirates.IncreaseThreatLevelFor(TargetEmpire);

            return true;
        }

        void DemandMoneyFromAI()
        {
            bool error = true;
            if (Encounter.GetEncounterForAI(Pirates.Owner, 0, out Encounter e))
            {
                if (e.PercentMoneyDemanded > 0)
                {
                    error             = false;
                    int moneyDemand   = Pirates.GetMoneyModifier(TargetEmpire, e.PercentMoneyDemanded);
                    float chanceToPay = 1 - moneyDemand/TargetEmpire.Money.LowerBound(1);
                    chanceToPay       = chanceToPay.LowerBound(0) * 100 / ((int)UState.P.Difficulty+1);
                        
                    if (TargetEmpire.data.TaxRate < 0.4f && Owner.Random.RollDice(chanceToPay)) // We can expand that with AI personality
                    {
                        TargetEmpire.AddMoney(-moneyDemand);
                        TargetEmpire.AI.EndWarFromEvent(Pirates.Owner);
                        Log.Info(ConsoleColor.Green, $"Pirates: {Owner.Name} Payment Director " +
                                                     $"Got - {moneyDemand} credits from {TargetEmpire.Name}");
                    }
                    else
                    {
                        TargetEmpire.AI.DeclareWarFromEvent(Pirates.Owner, WarType.SkirmishWar);
                        Log.Info(ConsoleColor.Green, $"Pirates: {Owner.Name} Payment Director " +
                                                     $"- {TargetEmpire.Name} refused to pay {moneyDemand} credits!");
                    }
                }
            }

            if (error)
                Log.Warning($"Could not find PercentMoneyDemanded in {Pirates.Owner.Name} encounters for {TargetEmpire.Name}. " +
                            $"Make sure there is a step 0 encounter for {Pirates.Owner.Name} in encounter dialogs and " +
                            "with <BaseMoneyRequested> xml tag");
        }
    }
}