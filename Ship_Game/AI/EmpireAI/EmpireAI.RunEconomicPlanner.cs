// ReSharper disable once CheckNamespace

using Ship_Game.AI.Budget;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Components;
using Ship_Game.Gameplay;
using static Ship_Game.AI.Components.BudgetPriorities;
using Ship_Game.Data.Serialization;

namespace Ship_Game.AI
{
    /// <summary>
    /// Economic process in brief.
    /// set up treasury goal and tax rate.
    /// calculate threat to empire.
    /// set budgets for areas.
    /// some areas like civilian freighter budget is not restrictive. It is used to allow the empire to track the money spent in that area.
    /// most budget areas are not hard restricted. There will be some wiggle room or outright relying on stored money.
    /// </summary>
    /// 
    using static HelperFunctions;
    public sealed partial class EmpireAI
    {

        [StarData] public float SSPBudget;
        [StarData] public float SpyBudget;
        [StarData] public float FreightBudget;
        [StarData] public float ColonyBudget;
        [StarData] public float DefenseBudget;
        [StarData] public float TerraformBudget;
        /// <summary>
        /// value from 0 to 1+
        /// This represents the overall threat to the empire. It is calculated from the EmpireRiskAssessment class.
        /// it currently looks at expansion threat, border threat, and general threat from each each empire.
        /// </summary>
        public float ThreatLevel { get; private set; }    = 0;
        public float EconomicThreat { get; private set; } = 0;
        public float BorderThreat { get; private set; }   = 0;
        public float EnemyThreat { get; private set; }    = 0;
        /// <summary>
        /// This is a quick set check to see if we are financially able to rush production
        /// </summary>
        public bool SafeToRush => CreditRating > 0.8f;

        // Empire spaceDefensive Reserves high enough to support fractional build budgets
        public bool EmpireCanSupportSpcDefense => DefenseBudget > OwnerEmpire.TotalOrbitalMaintenance && CreditRating > 0.90f;

        /// <summary>
        /// This is the budgeted amount of money that will be available to empire looking over 20 years.
        /// </summary>
        public float ProjectedMoney { get; private set; } = 0;

        /// <summary>
        /// This a ratio of projectedMoney and the normalized money then multiplied by 2 then add 1 - taxrate
        /// then the whole thing divided by 3. This puts a large emphasis on money goal to money ratio
        /// but a high tax will greatly effect the result.
        /// </summary>
        public float CreditRating
        {
            get
            {
                float normalMoney = OwnerEmpire.Money;
                float goalRatio = normalMoney / ProjectedMoney;

                return (goalRatio.UpperBound(1) * 3 + (1 - OwnerEmpire.data.TaxRate)) / 4f;
            }
        }

        /// <summary>
        /// This is not very accurate. Its around 20% off in either direction.
        /// It is a fast estimate and close enough with some hacks. 
        /// </summary>
        private float FindTaxRateToReturnAmount(float amount)
        {
            if (amount == 0)
                return 0;

            for (int i = 1; i < 100; i++)
            {
                float taxRate = i / 100f;

                float amountMade = OwnerEmpire.MaximumIncome * taxRate;

                if (amountMade >= amount)
                {
                    return taxRate;
                }
            }
            return 1;
        }

        public void RunEconomicPlanner(bool fromSave = false)
        {
            float money = OwnerEmpire.Money;
            float treasuryGoal = TreasuryGoal(money);
            ProjectedMoney = treasuryGoal;
            AutoSetTaxes(ProjectedMoney, money);

            // gamestate attempts to increase the budget if there are wars or lack of some resources.
            // its primarily geared at ship building.
            float riskLimit = (CreditRating * 2).Clamped(0.1f, 2);
            ThreatLevel = GetRisk(riskLimit);

            // the values below are now weights to adjust the budget areas.
            float defense = BudgetSettings.GetBudgetFor(BudgetAreas.Defense);
            float SSP     = BudgetSettings.GetBudgetFor(BudgetAreas.SSP);
            float build   = BudgetSettings.GetBudgetFor(BudgetAreas.Build);
            float spy     = BudgetSettings.GetBudgetFor(BudgetAreas.Spy);
            float colony  = BudgetSettings.GetBudgetFor(BudgetAreas.Colony);
            float terraform = BudgetSettings.GetBudgetFor(BudgetAreas.Terraform);
            float savings   = BudgetSettings.GetBudgetFor(BudgetAreas.Savings);

            // for the player they don't use some budgets. so distribute them to areas they do
            // spy budget is a special case currently and is not distributed.
            if (OwnerEmpire.isPlayer)
            {
                float budgetBalance = (build + spy) / 2f;
                defense            += budgetBalance;
                colony             += budgetBalance;
                SSP                += budgetBalance;
            }

            float moneyStrategy = treasuryGoal.LowerBound(money);

            DefenseBudget   = ExponentialMovingAverage(DefenseBudget, DetermineDefenseBudget(moneyStrategy, defense, ThreatLevel));
            SSPBudget       = ExponentialMovingAverage(SSPBudget, DetermineSSPBudget(moneyStrategy, SSP));
            BuildCapacity   = ExponentialMovingAverage(BuildCapacity, DetermineBuildCapacity(moneyStrategy, ThreatLevel, build));
            SpyBudget       = ExponentialMovingAverage(SpyBudget, DetermineSpyBudget(moneyStrategy, spy));
            ColonyBudget    = ExponentialMovingAverage(ColonyBudget, DetermineColonyBudget(moneyStrategy, colony));
            TerraformBudget = ExponentialMovingAverage(TerraformBudget, DetermineColonyBudget(moneyStrategy, terraform));

            PlanetBudgetDebugInfo();
            float allianceBudget = 0;
            foreach (var ally in OwnerEmpire.Universe.GetAllies(OwnerEmpire)) allianceBudget += ally.AI.BuildCapacity;
            AllianceBuildCapacity = BuildCapacity + allianceBudget;
        }

        float DetermineDefenseBudget(float treasuryGoal, float percentOfMoney, float risk)
        {
            float budget = SetBudgetForArea(percentOfMoney, treasuryGoal, risk);
            return budget;
        }

        float DetermineSSPBudget(float treasuryGoal, float percentOfMoney)
        {
            var strat  = OwnerEmpire.Research.Strategy;
            float risk = (1 + (strat.IndustryRatio + strat.ExpansionRatio)) * 0.5f;
            return SetBudgetForArea(percentOfMoney, treasuryGoal, risk);
        }

        float DetermineBuildCapacity(float treasuryGoal, float risk, float percentOfMoney)
        {
            float buildBudget = SetBudgetForArea(percentOfMoney, treasuryGoal, risk);
            return buildBudget;
        }

        float DetermineColonyBudget(float treasuryGoal, float percentOfMoney)
        {
            var budget = SetBudgetForArea(percentOfMoney, treasuryGoal);
            return budget;
        }

        float DetermineSpyBudget(float treasuryGoal, float percentOfMoney)
        {
            if (OwnerEmpire.isPlayer)
                return 0;

            bool notKnown = !OwnerEmpire.AllRelations.Any(r => r.Known && !r.Them.IsFaction);
            if (notKnown) return 0;

            float trustworthiness = (OwnerEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 100) * 0.01f;
            float militaryRatio   = OwnerEmpire.Research.Strategy.MilitaryRatio;

            // it is possible that the number of agents can exceed the agent limit. That needs a whole other pr. So this hack to make things work.
            float agentRatio      =  OwnerEmpire.data.AgentList.Count.UpperBound(EmpireSpyLimit) / (float)EmpireSpyLimit;

            // here we want to make sure that even if they arent trust worthy that the value they put on war machines will
            // get more money.
            float treasuryToSave  = ((0.5f + agentRatio + trustworthiness + militaryRatio) * 0.6f);
            float numAgents       = OwnerEmpire.data.AgentList.Count;
            float spyNeeds        = 1 + EmpireSpyLimit - numAgents.UpperBound(EmpireSpyLimit);
            spyNeeds              = spyNeeds.LowerBound(1);
            float overSpend       = OverSpendRatio(treasuryGoal, treasuryToSave, spyNeeds);
            float budget          = treasuryGoal * percentOfMoney * overSpend;

            return budget;
        }

        private void PlanetBudgetDebugInfo()
        {
            if (!OwnerEmpire.Universe.Debug)
                return;

            var pBudgets = new Array<PlanetBudget>();
            foreach (var planet in OwnerEmpire.GetPlanets())
            {
                if (planet.Budget != null)
                    pBudgets.Add(planet.Budget);
            }

            PlanetBudgets = pBudgets;
        }

        /// <summary>
        /// set a target budget of 20 years of growth.
        /// </summary>
        public float TreasuryGoal(float normalizedMoney)
        {
            // calculate income using income at a 100% tax rate - untracked expenditures.
            float gross = OwnerEmpire.MaximumStableIncome - OwnerEmpire.TotalCivShipMaintenance -
                          OwnerEmpire.TroopCostOnPlanets - OwnerEmpire.TotalTroopShipMaintenance;

            float treasuryGoal = gross * OwnerEmpire.data.treasuryGoal / GoalEqualizer;
            float timeSpan     = 200;
            treasuryGoal      *= timeSpan;
            return treasuryGoal.LowerBound(0);
        }


        // As the empire grows, it wants more and more money in the bank, and it is getting out of proportion
        // resulting high taxes. This will reduce the goal to maintain low tax by the AI
        public float GoalEqualizer => OwnerEmpire.isPlayer || OwnerEmpire.Money  < 2000 ? 1 : (1 + OwnerEmpire.Money * 0.00004f).UpperBound(4);

        /// <summary>
        /// Creates a ratio between cash on hand above what we want on hand and treasury goal
        /// eg. treasury goal is 100, cash on hand is 100, and percent to save is .5
        /// then the result would (100 + 50) / 100 = 1.5
        /// m+m-t*p / t. will always return at least 0.
        ///
        /// </summary>
        public float OverSpendRatio(float treasuryGoal, float percentageOfTreasuryToSave, float maxRatio)
        {
            float money    = OwnerEmpire.Money;
            float treasury = treasuryGoal.LowerBound(1);
            float minMoney = money - treasury * percentageOfTreasuryToSave;
            float ratio    = (money + minMoney) / treasury.LowerBound(1);
            return ratio.Clamped(0f, maxRatio);
        }

        private void AutoSetTaxes(float treasuryGoal, float money)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoTaxes)
                return;

            if (money <= 0)
            {
                OwnerEmpire.data.TaxRate = 1;
                return;
            }

            float treasuryGap = treasuryGoal - money;

            if (treasuryGap < 0)
            {
                OwnerEmpire.data.TaxRate = 0;
                return;
            }

            float treasuryDeficit = treasuryGap > 0 ? treasuryGap : 0;

            // try to meet goal in 20 years.
            // currently logic hits goal in about 10 years.
            float timeSpan = 200;

            //figure how much is needed to fulfill treasury in timespan and cover current costs
            float neededPerTurnForeTreasury = Math.Max(treasuryDeficit / timeSpan, 0);
            float closeToGoalCompensator = 1;
            if (treasuryGoal > 1000f)
            {
                // If about 50% to treasury start reducing amount wanted (or 90% for the player)
                float reducer          = treasuryGoal / (OwnerEmpire.isPlayer ? 9f : 2f);
                closeToGoalCompensator = Math.Min(treasuryGap  / reducer, 1);
            }
            float amount = OwnerEmpire.AllSpending + neededPerTurnForeTreasury;

            OwnerEmpire.data.TaxRate = FindTaxRateToReturnAmount(amount * closeToGoalCompensator);
        }

        public Array<PlanetBudget> PlanetBudgets;

        float SetBudgetForArea(float percentOfIncome, float treasuryGoal, float risk = 1)
        {
            float budget = treasuryGoal * percentOfIncome * (OwnerEmpire.isPlayer ? 1 : risk);
            return budget.LowerBound(1);
        }

        public float GetRisk(float riskLimit)
        {
            float maxRisk    = 0;
            float econRisk   = 0;
            float borderRisk = 0;
            float enemyRisk  = 0;

            foreach (Relationship rel in OwnerEmpire.AllRelations
                         .Filter(rel => !rel.Them.IsDefeated && rel.Known && rel.Risk.Risk > 0))
            {
                maxRisk    += rel.Risk.Risk;
                econRisk   = Math.Max(econRisk, rel.Risk.Expansion);
                borderRisk = Math.Max(borderRisk, rel.Risk.Border);
                enemyRisk  = Math.Max(enemyRisk, rel.Risk.KnownThreat);
            }

            ThreatLevel    = maxRisk;
            EconomicThreat = econRisk;
            BorderThreat   = borderRisk;
            EnemyThreat    = enemyRisk;

            return maxRisk.Clamped(0.25f, riskLimit);
        }
    }
}