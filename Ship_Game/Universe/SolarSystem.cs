using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Lights;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public sealed class SolarSystem : ExplorableGameObject
    {
        public string Name = "Random System";
        public UniverseState Universe;

        //public Array<Empire> OwnerList = new Array<Empire>();
        public HashSet<Empire> OwnerList = new HashSet<Empire>();
        public Array<Ship> ShipList = new Array<Ship>();
        public bool PiratePresence { get; private set; }

        public Array<ILight> Lights = new Array<ILight>();

        // this is the minimum solar system radius
        // needs to be big enough to properly trigger system-radius related events
        const float MinRadius = 150000f;

        public Array<Planet> PlanetList = new Array<Planet>();
        public Array<Asteroid> AsteroidsList = new Array<Asteroid>();
        public Array<Moon> MoonList = new Array<Moon>();

        Empire[] FullyExplored = Empty<Empire>.Array;

        SunType TheSunType;
        public SunLayerState[] SunLayers;

        public SunType Sun
        {
            get
            {
                if (TheSunType.Disposed) // attempt to reload the sun data automatically
                {
                    Sun = SunType.FindSun(TheSunType.Id); // full reload
                }
                return TheSunType;
            }
            set
            {
                TheSunType = value;
                SunLayers = value.CreateLayers(ResourceManager.RootContent);
            }
        }

        public Array<Ring> RingList = new Array<Ring>();
        int NumberOfRings;
        public Array<SolarSystem> FiveClosestSystems = new Array<SolarSystem>();
        public Array<Anomaly> AnomaliesList = new Array<Anomaly>();
        public bool IsStartingSystem;
        [XmlIgnore][JsonIgnore] bool WasVisibleLastFrame;

        public SolarSystem(UniverseState us, int id)
            : base(id, GameObjectType.SolarSystem)
        {
            Universe = us;
            Radius = MinRadius;
            DisableSpatialCollision = true;
        }

        public SolarSystem(UniverseState us, Vector2 position) : this(us, us.CreateId())
        {
            Position = position;
        }

        public void Update(FixedSimTime timeStep, UniverseScreen universe)
        {
            var player = EmpireManager.Player;

            for (int i = 0; i < SunLayers.Length; i++)
            {
                SunLayerState layer = SunLayers[i];
                layer.Update(timeStep);
            }

            var solarStatus = Status.Values.ToArr();
            for (int i = 0; i < solarStatus.Length; i++)
            {
                var status = solarStatus[i];
                status.Update(timeStep);
            }

            InFrustum = universe.IsInFrustum(Position, Radius)
                    && (universe.IsSectorViewOrCloser)
                    && IsExploredBy(player);

            if (InFrustum && universe.IsSystemViewOrCloser)
            {
                WasVisibleLastFrame = true;
                for (int i = 0; i < AsteroidsList.Count; i++)
                {
                    AsteroidsList[i].UpdateVisibleAsteroid(Position, timeStep);
                }
                for (int i = 0; i < MoonList.Count; i++)
                {
                    MoonList[i].UpdateVisibleMoon(timeStep);
                }
            }
            else if (WasVisibleLastFrame)
            {
                WasVisibleLastFrame = false;
                for (int i = 0; i < AsteroidsList.Count; i++)
                {
                    AsteroidsList[i].DestroySceneObject();
                }

                for (int i = 0; i < MoonList.Count; i++)
                {
                    MoonList[i].DestroySceneObject();
                }
            }

            for (int i = 0; i < PlanetList.Count; i++)
            {
                Planet planet = PlanetList[i];
                planet.InFrustum = InFrustum && universe.IsInFrustum(planet.Position3D, planet.ObjectRadius);
                planet.Update(timeStep);
            }

            if (Sun.RadiationDamage > 0f)
                UpdateSolarRadiationDebug();

            bool radiation = ShouldApplyRadiationDamage(timeStep);
            if (radiation)
            {
                for (int i = 0; i < ShipList.Count; ++i)
                {
                    Ship ship = ShipList[i];
                    if (ship.Active)
                    {
                        ApplySolarRadiationDamage(ship);
                    }
                }
            }
        }

        public void SetPiratePresence(bool value)
        {
            PiratePresence = value;
        }

        /// <summary>
        /// Checks if the empire has planets owned in this system. It might be the only owner here as well.
        /// </summary>
        public bool HasPlanetsOwnedBy(Empire empire)
        {
            return OwnerList.Contains(empire);
        }

        public bool IsExclusivelyOwnedBy(Empire empire)
        {
            return HasPlanetsOwnedBy(empire) && OwnerList.Count == 1;
        }

        public void UpdateOwnerList()
        {
            OwnerList.Clear();
            foreach (Planet planet in PlanetList)
            {
                if (planet.Owner != null)
                    OwnerList.Add(planet.Owner);
            }
        }

        float RadiationTimer;
        const float RadiationInterval = 0.5f;

        bool ShouldApplyRadiationDamage(FixedSimTime timeStep)
        {
            if (Sun.RadiationDamage > 0f)
            {
                RadiationTimer += timeStep.FixedTime;
                if (RadiationTimer >= RadiationInterval)
                {
                    RadiationTimer -= RadiationInterval;
                    return true;
                }
            }
            return false;
        }

        void UpdateSolarRadiationDebug()
        {
            // some debugging for us developers
            if (Universe.DebugMode == Debug.DebugModes.Solar)
            {
                for (float r = 0.03f; r < 0.5f; r += 0.03f)
                {
                    float dist = Sun.RadiationRadius*r;
                    var color = new Color(Color.Red, Sun.DamageMultiplier(dist));
                    Universe.DebugWin?.DrawCircle(Debug.DebugModes.Solar,
                        Position, dist, color, 0f);
                }
                Universe.DebugWin?.DrawCircle(Debug.DebugModes.Solar,
                    Position, Sun.RadiationRadius, Color.Brown, 0f);
            }
        }

        void ApplySolarRadiationDamage(Ship ship)
        {
            if (!ship.IsGuardian && ShipWithinRadiationRadius(ship, out float distance))
            {
                float damage = SunLayers[0].Intensity * Sun.DamageMultiplier(distance)
                                                      * Sun.RadiationDamage;
                ship.CauseRadiationDamage(damage, this);
            }
        }

        bool ShipWithinRadiationRadius(Ship ship, out float distance)
        {
            distance = ship.Position.Distance(Position);
            return distance < Sun.RadiationRadius;
        }
        
        public bool InSafeDistanceFromRadiation(Vector2 center)
        {
            return Sun.RadiationDamage.AlmostZero() || center.Distance(Position) > Sun.RadiationRadius + 10000;
        }

        public bool InSafeDistanceFromRadiation(float distance)
        {
            return Sun.RadiationDamage.AlmostZero() || distance > Sun.RadiationRadius + 10000;
        }

        // overload for ship info UI or AI maybe
        public bool ShipWithinRadiationRadius(Ship ship)
        {
            float distance = ship.Position.Distance(Position);
            return distance < Sun.RadiationRadius;
        }

        public Planet IdentifyGravityWell(Ship ship)
        {
            if (Universe.GravityWells)
            {
                // @todo QuadTree. need to have planets in the quad tree.
                for (int i = 0; i < PlanetList.Count; i++)
                {
                    Planet planet                 = PlanetList[i];
                    float wellReduction           = 1 - ship.Loyalty.data.Traits.EnemyPlanetInhibitionPercentCounter;
                    bool inFriendlyProjectorRange = ship.IsInFriendlyProjectorRange;
                    bool planetInhibitsAtWar      = planet.Owner?.WillInhibit(ship.Loyalty) == true;
                    bool checkGravityWell         = !inFriendlyProjectorRange || planetInhibitsAtWar;
                    float wellRadius              = inFriendlyProjectorRange && planetInhibitsAtWar 
                                                    ? planet.GravityWellRadius * wellReduction
                                                    : planet.GravityWellRadius;

                    if (checkGravityWell && ship.Position.InRadius(planet.Position, wellRadius))
                        return planet;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks the priority of this system for defense tasks
        /// </summary>
        /// <param name="empire"></param>
        /// <returns>priority between 0 to 4 (0 is the highest)</returns>
        public int DefenseTaskPriority(Empire empire)
        {
            int priority = 3;
            var planetsToCheck = PlanetList.Filter(p => p.Owner == empire);
            if (planetsToCheck.Length == 0)
            {
                if (OwnerList.Any(empire.IsAtWarWith))
                    planetsToCheck = PlanetList.Filter(p => p.Owner != null);
            }

            if (planetsToCheck.Length > 0)
            {
                int totalLevels = 0;
                int totalWeights = 0;
                // Using weighted level here
                foreach (Planet p in planetsToCheck)
                {
                    totalLevels += p.Level;
                    totalWeights += p.Level*p.Level;
                }

                priority = 5 - totalWeights / totalLevels.LowerBound(1);
            }

            return priority;
        }

        public float PotentialValueFor(Empire e)
        {
            return PlanetList.Sum(p => p.ColonyPotentialValue(e));
        }

        public float WarValueTo(Empire empire)
        { 
            return PlanetList.Sum(p => p.ColonyWarValueTo(empire));
        }

        readonly Map<Empire, EmpireSolarSystemStatus> Status = new Map<Empire, EmpireSolarSystemStatus>();

        EmpireSolarSystemStatus GetStatus(Empire empire)
        {
            if (!Status.TryGetValue(empire, out EmpireSolarSystemStatus status))
            {
                status = new EmpireSolarSystemStatus(this, empire);
                Status.Add(empire, status);
            }
            return status;
        }

        /// <summary>
        /// Forces present can not cause damage to ships but can be destroyed. 
        /// </summary>
        public bool HostileForcesPresent(Empire empire)
        {
            if (empire == null)
                return false;
            return GetStatus(empire).HostileForcesPresent;
        }

        /// <summary>
        /// Forces present can destroy friendly ships. 
        /// </summary>
        public bool DangerousForcesPresent(Empire empire)
        {
            if (empire == null)
                return false;
            return GetStatus(empire).DangerousForcesPresent;
        }

        void SetFullyExplored(Empire empire) => FullyExplored.FlatMapSet(ref FullyExplored, empire);
        public bool IsFullyExploredBy(Empire empire) => FullyExplored.FlatMapIsSet(empire);
        public void UpdateFullyExploredBy(Empire empire)
        {
            if (IsExploredBy(empire)
                && !IsFullyExploredBy(empire)
                && !PlanetList.Any(p => !p.IsExploredBy(empire)))
            {
                SetFullyExplored(empire);
                //Log.Info($"The {empire.Name} have fully explored {Name}");
            }
        }

        public Planet FindPlanet(int planetId)
        {
            if (planetId != 0)
            {
                foreach (Planet p in PlanetList)
                    if (p.Id == planetId)
                        return p;
            }
            return null;
        }

        public void GenerateRandomSystem(UniverseState us, RandomBase random, string name, Empire owner)
        {
            // Changed by RedFox: 3% chance to get a tri-sun "star_binary"
            Sun = random.RollDice(percent:3)
                ? SunType.FindSun("star_binary")
                : SunType.RandomHabitableSun(s => s.Id != "star_binary");

            Name = name;
            int starRadius = random.Int(250, 500);
            float sysMaxRingRadius = starRadius * 300;
            float firstRingRadius = sysMaxRingRadius * 0.1f;
            int minR = random.AvgInt(GlobalStats.ExtraPlanets, 3, iterations: 2);
            int maxR = random.Int(minR, 7 + minR);
            NumberOfRings = random.Int(minR, maxR);

            // when generating homeworld systems, we want at least 5 rings
            if (owner != null)
            {
                IsStartingSystem = true;
                NumberOfRings = NumberOfRings.LowerBound(5);
            }

            RingList.Capacity = NumberOfRings;
            float ringSpace   = sysMaxRingRadius / NumberOfRings;

            MarkovNameGenerator markovNameGenerator = null;
            if (owner != null)
                markovNameGenerator = ResourceManager.GetRandomNames(owner);

            float NextRingRadius(int ringNum) => firstRingRadius + random.Float(0, ringSpace / (1 + NumberOfRings - ringNum));

            float GeneratePlanet(int ringNum)
            {
                float ringRadius = NextRingRadius(ringNum);
                float randomAngle = random.Float(0f, 360f);
                string planetName = markovNameGenerator?.NextName ?? Name + " " + RomanNumerals.ToRoman(ringNum);
                var p = new Planet(us.CreateId(), random, this, randomAngle, ringRadius, planetName,
                                   sysMaxRingRadius, owner, null);
                PlanetList.Add(p);
                var ring = new Ring
                {
                    OrbitalDistance = p.OrbitalRadius,
                    Asteroids = false,
                    planet    = p
                };
                RingList.Add(ring);
                return p.OrbitalRadius;
            }

            int ringNumber = 1;
            for (; ringNumber < NumberOfRings + 1; ringNumber++)
            {
                firstRingRadius += 5000;
                if (!GlobalStats.DisableAsteroids && random.RollDice(10))
                {
                    float ringRadius = NextRingRadius(ringNumber);
                    float spread = ringRadius - firstRingRadius;
                    GenerateAsteroidRing(random, ringRadius + spread * 0.25f, spread: spread * 0.5f);
                    firstRingRadius = ringRadius + spread / 2;
                }
                else
                {
                    firstRingRadius = GeneratePlanet(ringNumber);
                }
            }

            // for homeworld systems, force generate a planet if none was generated
            if (owner != null && PlanetList.Count == 0)
            {
                GeneratePlanet(ringNumber + 1);
            }

            // now, if number of planets is <= 2 and they are barren,
            // then 33% chance to have neutron star:
            if (PlanetList.Count <= 2 + GlobalStats.ExtraPlanets && PlanetList.All(p => p.IsBarrenGasOrVolcanic)
                && random.RollDice(percent:15))
            {
                Sun = SunType.RandomBarrenSun();
            }

            FinalizeGeneratedSystem();
        }

        public void GenerateFromData(UniverseState us, RandomBase random, SolarSystemData data, Empire owner)
        {
            Name = data.Name;
            Sun = SunType.FindSun(data.SunPath);

            int numberOfRings = data.RingList.Count;
            int fixedSpacing = random.Int(50, 500);
            int nextDistance = 10000 + GetRingWidth(0);
            float sysMaxRingRadius = data.RingList.Last.OrbitalDistance;

            int GetRingWidth(int orbitalWidth)
            {
                return orbitalWidth > 0 ? orbitalWidth : fixedSpacing + random.Int(10500, 12000);
            }

            if (owner != null)
                IsStartingSystem = true;

            for (int i = 0; i < numberOfRings; i++)
            {
                SolarSystemData.Ring ringData = data.RingList[i];

                int orbitalDist = ringData.OrbitalDistance > 0 ? ringData.OrbitalDistance : nextDistance;
                nextDistance = orbitalDist + GetRingWidth(ringData.OrbitalWidth);

                if (ringData.Asteroids != null)
                {
                    GenerateAsteroidRing(random, orbitalDist, spread: 3000f, scaleMin: 1.2f, scaleMax: 4.6f);
                    continue;
                }

                float randomAngle = random.Float(0f, 360f);
                var p = new Planet(us.CreateId(), random, this, randomAngle, orbitalDist, ringData.Planet,
                                   sysMaxRingRadius, owner, ringData);
                PlanetList.Add(p);
                RingList.Add(new Ring
                {
                    OrbitalDistance = orbitalDist,
                    Asteroids = false,
                    planet = p
                });
            }

            FinalizeGeneratedSystem();
        }

        void FinalizeGeneratedSystem()
        {
            Radius = MinRadius;
            if (!RingList.IsEmpty)
            {
                int enclosingRadius = ((int)RingList.Last.OrbitalDistance + 10000).RoundUpToMultipleOf(10000);
                Radius = Math.Max(MinRadius, enclosingRadius);
            }
        }

        public void AddSystemExploreSuccessMessage(Empire empire)
        {
            if (!empire.isPlayer)
                return; // Message only the player

            //added by gremlin  add shamatts notification here
            var message = new StringBuilder(Name); //@todo create global string builder
            message.Append(" system explored.");

            if (Sun.RadiationDamage > 0)
                message.Append("\nThis Star emits radiation which will damage your ship's\nexternal modules or shields if they get close to it.");

            var planetsTypesNumber = new Map<string, int>();
            if (PlanetList.Count > 0)
            {
                foreach (Planet planet in PlanetList)
                    planetsTypesNumber.AddToValue(planet.CategoryName, 1);

                foreach (var pair in planetsTypesNumber)
                    message.Append('\n').Append(pair.Value).Append(' ').Append(pair.Key);
            }

            foreach (Planet planet in PlanetList)
            {
                Building tile = planet.BuildingList.Find(t => t.IsCommodity);
                if (tile != null)
                    message.Append('\n').Append(tile.Name).Append(" on ").Append(planet.Name);
            }

            if (DangerousForcesPresent(empire))
                message.Append("\nCombat in system!!!");

            if (OwnerList.Count > 0 && !OwnerList.Contains(empire))
                message.Append("\nContested system!!!");

            Universe.Notifications.AddNotification(new Notification
            {
                Pause           = false,
                Message         = message.ToString(),
                ReferencedItem1 = this,
                Icon            = Sun.Icon,
                Action          = "SnapToExpandSystem"
            }, "sd_ui_notification_warning");
        }

        public float GetActualStrengthPresent(Empire e)
        {
            float strength = 0f;
            for (int i = 0; i < ShipList.Count; i++)
            {
                Ship ship = ShipList[i];
                if (ship?.Active != true) continue;
                if (ship.Loyalty != e)
                    continue;
                strength += ship.GetStrength();
            }

            return strength;
        }

        public float GetKnownStrengthHostileTo(Empire e)
        {
            float strength = 0f;
            for (int i = 0; i < ShipList.Count; i++)
            {
                Ship ship = ShipList[i];
                if (ship?.Active != true || !ship.KnownByEmpires.KnownBy(e)) continue;
                if (!ship.Loyalty.IsAtWarWith(e))
                    continue;
                strength += ship.GetStrength();
            }

            return strength;
        }

        public bool IsAnomalyOnAnyKnownPlanets(Empire player)
        {
            foreach (Planet planet in PlanetList)
            {
                if (planet.IsExploredBy(player))
                {
                    for (int i = 0; i < planet.BuildingList.Count; ++i)
                    {
                        if (planet.BuildingList[i].EventHere)
                            return true;
                    }
                }
            }
            return false;
        }

        public Array<Empire> GetKnownOwners(Empire player)
        {
            var owners = new Array<Empire>();

            foreach (Empire e in OwnerList)
            {
                player.GetRelations(e, out Relationship ssRel);
                bool wellKnown = Universe.Debug || e.isPlayer || ssRel.Treaty_Alliance;
                if (wellKnown)
                    return OwnerList.ToArrayList();

                if (ssRel.Known)
                    owners.Add(e);
            }
            return owners;
        }

        bool NoAsteroidProximity(Vector2 pos)
        {
            for (int i = 0; i < AsteroidsList.Count; i++)
                if (pos.InRadius(AsteroidsList[i].Position, 200.0f))
                    return false;
            return true;
        }

        Vector2 GenerateAsteroidPos(RandomBase random, float ringRadius, float spread)
        {
            for (int i = 0; i < 100; ++i) // while (true) would be unsafe, so give up after 100 turns
            {
                Vector2 pos = Vector2.Zero.GenerateRandomPointOnCircle(ringRadius + random.Float(-spread, spread));
                if (NoAsteroidProximity(pos))
                    return pos;
            }
            return Vector2.Zero.GenerateRandomPointOnCircle(ringRadius + random.Float(-spread, spread));
        }

        void GenerateAsteroidRing(RandomBase random, float orbitalDistance, float spread, float scaleMin=0.75f, float scaleMax=1.6f)
        {
            int numberOfAsteroids = random.Int(150, 250);
            AsteroidsList.Capacity += numberOfAsteroids;
            for (int i = 0; i < numberOfAsteroids; ++i)
            {
                var pos = GenerateAsteroidPos(random, orbitalDistance, spread);
                AsteroidsList.Add(new Asteroid(Universe.CreateId(), random, scaleMin, scaleMax, pos));
            }
            RingList.Add(new Ring
            {
                OrbitalDistance = orbitalDistance,
                Asteroids = true
            });
        }

        public struct FleetAndPos
        {
            public string FleetName;
            public Vector2 Pos;
        }

        public struct Ring
        {
            public float OrbitalDistance;
            public bool Asteroids;
            public Planet planet;

            public SavedGame.RingSave Serialize()
            {
                var ringSave = new SavedGame.RingSave
                {
                    Asteroids = Asteroids,
                    OrbitalDistance = OrbitalDistance
                };

                if (planet == null)
                    return ringSave;

                var pdata = new SavedGame.PlanetSaveData
                {
                    Id = planet.Id,
                    TurnsCrippled        = planet.CrippledTurns,
                    FoodState            = planet.FS,
                    ProdState            = planet.PS,
                    FoodLock             = planet.Food.PercentLock,
                    ProdLock             = planet.Prod.PercentLock,
                    ResLock              = planet.Res.PercentLock,
                    Name                 = planet.Name,
                    Scale                = planet.Scale,
                    ShieldStrength       = planet.ShieldStrengthCurrent,
                    Population           = planet.Population,
                    BasePopPerTile       = planet.BasePopPerTile,
                    Fertility            = planet.BaseFertility,
                    MaxFertility         = planet.BaseMaxFertility,
                    Richness             = planet.MineralRichness,
                    Owner                = planet.Owner?.data.Traits.Name ?? "",
                    WhichPlanet          = planet.PType.Id,
                    OrbitalAngle         = planet.OrbitalAngle,
                    OrbitalDistance      = planet.OrbitalRadius,
                    HasRings             = planet.HasRings,
                    Radius               = planet.ObjectRadius,
                    FarmerPercentage     = planet.Food.Percent,
                    WorkerPercentage     = planet.Prod.Percent,
                    ResearcherPercentage = planet.Res.Percent,
                    FoodHere             = planet.FoodHere,
                    TerraformPoints      = planet.TerraformPoints,
                    ProdHere             = planet.ProdHere,
                    ColonyType           = planet.colonyType,
                    GovOrbitals          = planet.GovOrbitals,
                    GovGroundDefense     = planet.GovGroundDefense,
                    GovMilitia           = planet.AutoBuildTroops,
                    GarrisonSize         = planet.GarrisonSize,
                    Quarantine           = planet.Quarantine,
                    ManualOrbitals       = planet.ManualOrbitals,
                    WantedPlatforms      = planet.WantedPlatforms,
                    WantedShipyards      = planet.WantedShipyards,
                    WantedStations       = planet.WantedStations,
                    ManualCivilianBudget = planet.ManualCivilianBudget,
                    ManualGrdDefBudget   = planet.ManualGrdDefBudget,
                    ManualSpcDefBudget   = planet.ManualSpcDefBudget,
                    DontScrapBuildings   = planet.DontScrapBuildings,
                    NumShipyards         = planet.NumShipyards,
                    SpecialDescription   = planet.SpecialDescription,
                    IncomingFreighters   = planet.IncomingFreighterIds,
                    OutgoingFreighters   = planet.OutgoingFreighterIds,
                    StationsList         = planet.OrbitalStations.Where(s => s.Active).Select(s => s.Id).ToArr(),
                    ExploredBy           = planet.ExploredByEmpires.Select(e => e.data.Traits.Name),
                    BaseFertilityTerraformRatio  = planet.BaseFertilityTerraformRatio,
                    HasLimitedResourcesBuildings = planet.HasLimitedResourceBuilding,
                    ManualFoodImportSlots     = planet.ManualFoodImportSlots,
                    ManualProdImportSlots     = planet.ManualProdImportSlots,
                    ManualColoImportSlots     = planet.ManualColoImportSlots,
                    ManualFoodExportSlots     = planet.ManualFoodExportSlots,
                    ManualProdExportSlots     = planet.ManualProdExportSlots,
                    ManualColoExportSlots     = planet.ManualColoExportSlots,
                    AverageFoodImportTurns    = planet.AverageFoodImportTurns,
                    AverageProdImportTurns    = planet.AverageProdImportTurns,
                    AverageFoodExportTurns    = planet.AverageFoodExportTurns,
                    AverageProdExportTurns    = planet.AverageProdExportTurns,
                    IsHomeworld               = planet.IsHomeworld,
                    BombingIntensity          = planet.BombingIntensity
                };

                if (planet.Owner != null)
                {
                    pdata.QISaveList = planet.ConstructionQueue.Select(item => item.Serialize());
                }

                pdata.PGSList = planet.TilesList.Select(tile => tile.Serialize());

                ringSave.Planet = pdata;
                return ringSave;
            }
        }

        public override string ToString() => $"System '{Name}' Pos={Position} Rings={NumberOfRings}";
    }
}