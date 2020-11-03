using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.AI.ShipMovement;
using System;
using static Ship_Game.AI.ShipAI;
using static Ship_Game.AI.ShipAI.TargetParameterTotals;

namespace Ship_Game.AI
{
    public sealed class CombatAI
    {
        public float VultureWeight = 0.5f;
        public float SelfDefenseWeight = 0.5f;
        public float SmallAttackWeight;
        public float MediumAttackWeight;
        public float LargeAttackWeight;
        public float PreferredEngagementDistance;
        public float PirateWeight;
        private float AssistWeight = 0.5f;
        public Ship Owner;
        ShipAIPlan CombatTactic;
        CombatState CurrentCombatStance;

        public CombatAI()
        {
        }

        public CombatAI(Ship ship)
        {
            Owner = ship;
            UpdateTargetPriorities();
            CurrentCombatStance = ship.AI.CombatState;
            SetCombatTactics(ship.AI.CombatState);
        }

        public void ClearTargets()
        {
            Owner.AI.Target = null;
            Owner.AI.PotentialTargets.Clear();
        }

        public void UpdateTargetPriorities()
        {
            var ship = Owner;
            if (ship.SurfaceArea <= 0)
                return;

            float pd    = 0;
            float mains = 0;
            float other = 0;

            for (int i = 0; i < ship.Weapons.Count; i++)
            {
                Weapon w = ship.Weapons[i];
                if (w.Module.XSIZE * w.Module.YSIZE > 4)
                {
                    mains += w.Module.XSIZE * w.Module.YSIZE;
                }
                else if (w.SalvoCount > 2 || w.Tag_PD || w.Tag_Flak || w.Module.XSIZE + w.Module.YSIZE < 3)
                {
                    pd += w.Module.XSIZE * w.Module.YSIZE;
                }
                else if (w.DamageAmount > 0)
                    other += w.Module.XSIZE * w.Module.YSIZE;
            }

            other += Owner.Carrier.AllFighterHangars.Length;

            float totalSizeWeight = mains + pd + other;

            LargeAttackWeight  = mains / totalSizeWeight; ;
            SmallAttackWeight  = (pd - mains) / totalSizeWeight;
            MediumAttackWeight = (other - mains) / totalSizeWeight;

            if (ship.loyalty.isFaction || ship.VelocityMaximum > 500)
                VultureWeight  = 1;
            if (ship.loyalty.WeArePirates)
                PirateWeight   = 1;
            AssistWeight = Owner.Mothership != null ? 1 : 0;
            PreferredEngagementDistance = Owner.WeaponsMaxRange > 0 ? Owner.WeaponsMaxRange : Owner.AI.GetSensorRadius().LowerBound(1);
        }

        public float ApplyWeight(Ship nearbyShip, TargetParameterTotals nearbyTotals)
        {
            if (Owner == null) 
                return 0;
            float weight = 0;
            if (nearbyShip == null) 
                return weight;

            if (Owner.AI.Target == nearbyShip)
                weight += 1;

            weight += VultureWeight * (1 - nearbyShip.HealthPercent);
            weight += RangeWeight(nearbyShip);

            if (nearbyShip.AI.Target == Owner)
                weight += SelfDefenseWeight;

            if (nearbyShip.AI.Target?.GetLoyalty() == Owner.loyalty) 
                weight += AssistWeight;

            return weight / 5;
        }

        public float RangeWeight(Ship nearbyShip)
        {
            float rangeToTarget = Owner.Center.Distance(nearbyShip.Center);
            float weight = 0;
            if (Owner.Mothership != null)            
                rangeToTarget = Owner.Mothership.Center.Distance(nearbyShip.Center).UpperBound(Owner.Mothership.AI.CombatAI.PreferredEngagementDistance);
            
            if (Owner.AI.EscortTarget != null)
            {
                rangeToTarget = Owner.AI.EscortTarget.Center.Distance(nearbyShip.Center).UpperBound(PreferredEngagementDistance);
            }
            weight += 1 - (int)Math.Ceiling(rangeToTarget / PreferredEngagementDistance);


            return weight.Clamped(-1f, 1);
        }

        public ShipWeight ShipCommandTargeting(ShipWeight weight, TargetParameterTotals targetPrefs)
        {
            // standard ship targeting:
            // within max weapons range
            // within desired range
            // pirate scavenging
            // Size desire / hit chance
            // speed / turnrate difference
            // damaged by
            // 
            Ship target            = weight.Ship;
            float theirDPS         = target.TotalDps;
            float distanceToTarget = Owner.Center.Distance(weight.Ship.Center);
            float errorRatio       = (Owner.MaxWeaponError) / target.Radius;
            float danger           = (theirDPS - targetPrefs.DPS) / theirDPS.LowerBound(1);
            float targetDanger     = (danger * errorRatio).Clamped(-1,1);

            // closer is higher
            float weaponsRange = Owner.WeaponsMaxRange * 2;
            float inWeaponsRange = (weaponsRange - distanceToTarget) / weaponsRange;
            float inDesiredCombatRange = (Owner.DesiredCombatRange * 2 - distanceToTarget ) / (Owner.DesiredCombatRange * 2);

            float targetingUs          = weight.Ship.AI.Target == Owner ? targetDanger : 0;
            float damagingUs           = weight.Ship == Owner.LastDamagedBy ? targetDanger : 0;

            // more agile than us the less they are valued. 
            float turnRatio = 0;
            if (target.RotationRadiansPerSecond > 0 && Owner.RotationRadiansPerSecond >0)
                turnRatio = (Owner.RotationRadiansPerSecond  - target.RotationRadiansPerSecond) / Owner.RotationRadiansPerSecond;

            float stlRatio = 0;
            if (target.MaxSTLSpeed > 0 )
                stlRatio = (Owner.MaxSTLSpeed - target.MaxSTLSpeed) / target.MaxSTLSpeed;

            float pirate = Owner.loyalty.WeArePirates && target.shipData.ShipCategory == ShipData.Category.Civilian ? 1 : 0;


            float weightTotal = inDesiredCombatRange + targetingUs + damagingUs + turnRatio + stlRatio + pirate;
            float normalizer = 0;
            //normalizer += errorRatio != 0 ? 1 : 0;
            //normalizer += targetDanger != 0 ? 1 : 0;
            normalizer += inDesiredCombatRange != 0 ? 1 : 0;
            normalizer += targetingUs != 0 ? 1 : 0;
            normalizer += damagingUs != 0 ? 1 : 0;
            normalizer += Owner.AI.Target != weight.Ship && Owner.AI.Target != null ? 1 : 0;
            normalizer += turnRatio != 0 ? 1 : 0;
            normalizer += stlRatio != 0 ? 1 : 0;

            weightTotal /= normalizer;

            weight.SetWeight(weightTotal);
            return weight;

        }

        public float SizeAttackWeight(Ship target, TargetParameterTotals nearbyAverages)
        {
            float avgNearBySize = nearbyAverages.Size;
            int surfaceArea     = target.SurfaceArea;
            float priority      = MediumAttackWeight;

            if (surfaceArea < avgNearBySize * 0.75f)
                priority = SmallAttackWeight;
            else if (surfaceArea > avgNearBySize * 1.25f)
                priority = LargeAttackWeight;

            return priority;
        }

        public void SetCombatTactics(CombatState combatState)
        {
            if (CurrentCombatStance != combatState)
            {
                CurrentCombatStance = combatState;
                CombatTactic = null;
                Owner.shipStatusChanged = true; // FIX: force DesiredCombatRange update
            }

            if (CombatTactic == null)
            {
                switch (combatState)
                {
                    case CombatState.Artillery:
                        CombatTactic = new CombatTactics.Artillery(Owner.AI);
                        break;
                    case CombatState.BroadsideLeft:
                        CombatTactic = new CombatTactics.BroadSides(Owner.AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.BroadsideRight:
                        CombatTactic = new CombatTactics.BroadSides(Owner.AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.OrbitLeft:
                        CombatTactic = new CombatTactics.OrbitTarget(Owner.AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.OrbitRight:
                        CombatTactic = new CombatTactics.OrbitTarget(Owner.AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.AttackRuns:
                        CombatTactic = new CombatTactics.AttackRun(Owner.AI);
                        break;
                    case CombatState.HoldPosition:
                        CombatTactic = new CombatTactics.HoldPosition(Owner.AI);
                        break;
                    case CombatState.Evade:
                        CombatTactic = new CombatTactics.Evade(Owner.AI);
                        break;
                    case CombatState.AssaultShip:
                        CombatTactic = new CombatTactics.AssaultShipCombat(Owner.AI);
                        break;
                    case CombatState.OrbitalDefense:
                        break;
                    case CombatState.ShortRange:
                        CombatTactic = new CombatTactics.Artillery(Owner.AI);
                        break;
                }

            }
        }

        public void ExecuteCombatTactic(FixedSimTime timeStep) => CombatTactic?.Execute(timeStep, null);

    }
}