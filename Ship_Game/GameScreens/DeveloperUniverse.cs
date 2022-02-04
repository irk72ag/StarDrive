﻿using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Universe;

namespace Ship_Game
{
    public class DeveloperUniverse : UniverseScreen
    {
        public DeveloperUniverse(float universeSize) : base(universeSize)
        {
            UState.NoEliminationVictory = true; // SandBox mode doesn't have elimination victory
            UState.Paused = false; // start simulating right away for Sandbox
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // nice zoom in effect, we set the cam height to super high
            CamPos.Z *= 10000.0;
            CamDestination.Z = 100000.0; // and set a lower destination

            // DISABLED these since they are not that useful anymore

            //Empire playerEmpire = SandBox.EmpireList.First;
            //Planet homePlanet   = playerEmpire.GetPlanets()[0];

            //Vector2 debugDir  = playerEmpire.GetOwnedSystems()[0].Position.DirectionToTarget(homePlanet.Center);
            //string platformId = "Kinetic Platform";
            //string platformId = "Beam Platform L4";

            // @note Auto-added to Universe
            //var debugPlatform = new PredictionDebugPlatform(platformId, EmpireManager.Remnants, homePlanet.Center + debugDir * 5000f);

            Log.Write(ConsoleColor.DarkMagenta, "DeveloperUniverse.LoadContent");
        }
        
        public static DeveloperUniverse Create(string playerPreference = "United",
                                               int numOpponents = 1)
        {
            var s = Stopwatch.StartNew();
            EmpireManager.Clear();

            var universe = new DeveloperUniverse(1_000_000f);
            UniverseState us = universe.UState;
            us.FTLModifier      = GlobalStats.FTLInSystemModifier;
            us.EnemyFTLModifier = GlobalStats.EnemyFTLInSystemModifier;
            us.GravityWells        = GlobalStats.PlanetaryGravityWells;
            us.FTLInNeutralSystems = GlobalStats.WarpInSystem;
            CurrentGame.StartNew(us, pace:1f, 1, 0, 1);

            IEmpireData[] candidates = ResourceManager.MajorRaces.Filter(d => PlayerFilter(d, playerPreference));
            IEmpireData player = candidates[0];
            IEmpireData[] opponents = ResourceManager.MajorRaces.Filter(data => data.ArchetypeName != player.ArchetypeName);

            var races = new Array<IEmpireData>(opponents);
            races.Shuffle();
            races.Resize(Math.Min(races.Count, numOpponents)); // truncate
            races.Insert(0, player);

            foreach (IEmpireData data in races)
            {
                Empire e = us.CreateEmpire(data, isPlayer: (data == player));
                e.data.CurrentAutoScout     = e.data.ScoutShip;
                e.data.CurrentAutoColony    = e.data.ColonyShip;
                e.data.CurrentAutoFreighter = e.data.FreighterShip;
                e.data.CurrentConstructor   = e.data.ConstructorShip;

                // Now, generate system for our empire:
                var system = new SolarSystem(us);
                system.Position = GenerateRandomSysPos(10000, us);
                system.GenerateStartingSystem(us, e.data.Traits.HomeSystemName, 1f, e);
                system.OwnerList.Add(e);

                us.AddSolarSystem(system);
            }

            foreach (IEmpireData data in ResourceManager.MinorRaces) // init minor races
            {
                us.CreateEmpire(data, isPlayer: false);
            }

            Empire.InitializeRelationships(EmpireManager.Empires, GameDifficulty.Hard);

            foreach (SolarSystem system in universe.UState.Systems)
            {
                system.FiveClosestSystems = universe.UState.GetFiveClosestSystems(system);
            }

            ShipDesignUtils.MarkDesignsUnlockable();
            Log.Info($"CreateSandboxUniverse elapsed:{s.Elapsed.TotalMilliseconds}");
            return universe;
        }
        
        static bool PlayerFilter(IEmpireData d, string playerPreference)
        {
            if (playerPreference.NotEmpty())
            {
                return d.ArchetypeName.Contains(playerPreference)
                    || d.Name.Contains(playerPreference);
            }
            return true;
        }

        static Vector2 GenerateRandomSysPos(float spacing, UniverseState us)
        {
            Vector2 sysPos = Vector2.Zero;
            for (int i = 0; i < 20; ++i) // max 20 tries
            {
                sysPos = RandomMath.Vector2D(us.Size - 100000f);
                if (us.FindSolarSystemAt(sysPos) == null)
                    return sysPos; // we got it!
            }
            return sysPos;
        }
    }
}