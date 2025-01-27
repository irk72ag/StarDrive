﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets
{
    [TestClass]
    public class FleetTests : StarDriveTest
    {
        Array<Ship> PlayerShips = new();
        Array<Ship> EnemyShips  = new();
        Array<Fleet> PlayerFleets = new();

        public FleetTests()
        {
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Heavy Carrier mk5-b", "Corsair", "Terran-Prototype");
            CreateUniverseAndPlayerEmpire();
            //Universe.UState.Objects.EnableParallelUpdate = false;
            UState.Events.Disabled = true;
        }

        void CreateWantedShipsAndAddThemToList(int numberWanted, string shipName, Array<Ship> shipList)
        {
            for (int i = 0; i < numberWanted; i++)
            {
                var ship = SpawnShip(shipName, Player, Vector2.Zero);
                shipList.Add(ship);
                if (ship.MaxFTLSpeed < Ship.LightSpeedConstant)
                    throw new InvalidOperationException($"Invalid FleetTest ship: '{ship.Name}' CANNOT WARP! MaxFTLSpeed: {ship.MaxFTLSpeed:0}");
            }
        }

        Fleet CreateTestFleet(Array<Ship> ships, Array<Fleet> fleets)
        {
            Fleet fleet = ships[0].Loyalty.CreateFleet(1, null);
            fleet.AddShips(ships);
            fleets.Add(fleet);
            return fleet;
        }

        [TestMethod]
        public void FleetAssemblesIntoFlanksAndSquads()
        {
            CreateWantedShipsAndAddThemToList(10, "Heavy Carrier mk5-b", PlayerShips);
            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);

            // verify fleet created and has the expected ships
            AssertEqual(10, fleet.CountShips, $"Expected 10 ships in fleet but got {fleet.CountShips}");

            fleet.AutoArrange();

            int flankCount = fleet.AllFlanks.Count;
            AssertEqual(5, flankCount, $"Expected 5 flanks got {flankCount}");

            Array<Array<Fleet.Squad>> flanks = fleet.AllFlanks;
            int squadCount = flanks.Sum(sq => sq.Count);
            AssertEqual(3, squadCount, $"Expected 3 squads got {squadCount}");

            int squadShipCount = flanks.Sum(sq => sq.Sum(s=> s.Ships.Count));
            AssertEqual(10, squadShipCount, $"Expected 10 ships in fleet got {squadShipCount}");
        }

        [TestMethod]
        public void FleetArrangesIntoNonZeroOffsets()
        {
            CreateWantedShipsAndAddThemToList(10, "Heavy Carrier mk5-b", PlayerShips);
            foreach (var ship in PlayerShips)
            {
                ship.AI.CombatState = CombatState.Artillery;
            }

            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.Update(FixedSimTime.Zero/*paused during init*/);
            fleet.AutoArrange();
            foreach (var ship in PlayerShips)
            {
                Assert.IsTrue(ship.RelativeFleetOffset != Vector2.Zero, $"Ship RelativeFleetOffset must not be Zero: {ship}");
                Assert.IsTrue(ship.FleetOffset != Vector2.Zero, $"Ship FleetOffset must not be Zero: {ship}");
            }
        }

        Fleet CreateMassivePlayerFleet(Vector2 initialDir)
        {
            CreateWantedShipsAndAddThemToList(5, "Terran-Prototype", PlayerShips);
            CreateWantedShipsAndAddThemToList(195, "Vulcan Scout", PlayerShips);
            foreach (Ship s in PlayerShips)
                s.Direction = initialDir;

            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.AutoArrange();
            return fleet;
        }

        Vector2 FleetMoveTo(Fleet fleet, Vector2 offset)
        {
            Vector2 target = fleet.FinalPosition + offset;
            Vector2 finalDir = offset.Normalized();
            Log.Write($"Fleet.MoveToNow({target.X},{target.Y})");
            fleet.MoveTo(target, finalDir, MoveOrder.Regular);
            return target;
        }

        Vector2 FleetQueueMoveOrder(Fleet fleet, Vector2 offset)
        {
            Vector2 target = fleet.FinalPosition + offset;
            Vector2 finalDir = offset.Normalized();
            Log.Write($"Fleet.QueueMoveOrder({target.X},{target.Y})");
            fleet.MoveTo(target, finalDir, MoveOrder.AddWayPoint);
            return target;
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveUp()
        {
            // move up
            var offset = new Vector2(0, -40_000f);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveLeft()
        {
            // move left
            var offset = new Vector2(-40_000f, 0f);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveDown()
        {
            // move down
            var offset = new Vector2(0, 40_000f);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        [TestMethod]
        public void FleetIsAbleToWarpMoveRight()
        {
            // move right
            var offset = new Vector2(40_000f, 0);
            Fleet fleet = CreateMassivePlayerFleet(offset.Normalized());
            Vector2 target = FleetMoveTo(fleet, offset);
            AssertAllShipsWarpedToTarget(fleet, target, simTimeout: 8.0);
        }

        Fleet CreateRandomizedPlayerFleet(int randomSeed, bool bigShips)
        {
            Log.Write($"RandomizedFleet seed={randomSeed}");
            if (bigShips)
                CreateWantedShipsAndAddThemToList(5, "Terran-Prototype", PlayerShips);
            CreateWantedShipsAndAddThemToList(195, "Vulcan Scout", PlayerShips);

            // scatter the ships
            var random = new ThreadSafeRandom();
            foreach (Ship s in PlayerShips)
            {
                s.Direction = random.Direction2D();
                s.Position = random.Vector2D(2000);
            }

            Fleet fleet = CreateTestFleet(PlayerShips, PlayerFleets);
            fleet.AutoArrange();
            return fleet;
        }

        [TestMethod]
        public void FleetCanAssembleAndFormationWarp()
        {
            Fleet fleet = CreateRandomizedPlayerFleet(12345, bigShips:true);
            fleet.AssembleFleet(new Vector2(0, 10_000), Vectors.Down, forceAssembly:true); // assemble the fleet in distance

            // order it to warp forward at an angle
            var finalTarget = FleetMoveTo(fleet, new Vector2(50_000, 50_000));
            AssertAllShipsWarpedToTarget(fleet, finalTarget, simTimeout: 30.0);
        }

        [TestMethod]
        public void FleetCanAssembleAndFormationWarpToMultipleWayPoints()
        {
            Fleet fleet = CreateRandomizedPlayerFleet(12345, bigShips:false);
            fleet.AssembleFleet(new Vector2(0, 10_000), Vectors.Down, forceAssembly:true); // assemble the fleet in distance

            // order it to warp forward at an angle
            FleetMoveTo(fleet, new Vector2(20_000, 20_000));
            // and then queue up another WayPoint to the fleet
            var finalTarget = FleetQueueMoveOrder(fleet, new Vector2(-20_000, 20_000));
            AssertAllShipsWarpedToTarget(fleet, finalTarget, simTimeout: 120.0);
        }

        void AssertAllShipsWarpedToTarget(Fleet fleet, Vector2 target, double simTimeout)
        {
            var shipsThatWereInWarp = new HashSet<Ship>();
            const float arrivalDist = 8000;

            // run while some ships are still not within arrivalDist
            RunSimWhile((simTimeout, fatal:false), () => fleet.Ships.Any(s => Dist(s) > arrivalDist), body:() =>
            {
                fleet.Update(TestSimStep);
                foreach (Ship s in fleet.Ships)
                    if (s.IsInWarp) shipsThatWereInWarp.Add(s);
            });

            float Dist(Ship s)
            {
                return s.Position.Distance(target + s.FleetOffset);
            }

            void Print(string wat, Ship s)
            {
                Log.Write($"{wat} dist:{Dist(s):0} V:{s.Velocity.Length():0} STLCap:{s.STLSpeedLimit:0} FTLCap:{s.FTLSpeedLimit:0} Vmax:{s.VelocityMax:0}");
                Log.Write($"\t\t\tstate:{s.engineState} Goal:{s.AI.OrderQueue.PeekFirst?.Plan} {s.ShipEngines}");
                Log.Write($"\t\t\t{s}");
            }

            var didWarp = shipsThatWereInWarp.ToArr().Sorted(s => s.Id).Sorted(s => (int)s.ShipEngines.ReadyForFormationWarp);
            if (didWarp.Length != fleet.Ships.Count)
            {
                var notInWarp = fleet.Ships.Except(didWarp);
                string error = $"{notInWarp.Length} fleet ships did not enter warp!";
                Log.Write(error);
                for (int i = 0; i < notInWarp.Length && i < 10; ++i)
                    Print("DID_NOT_WARP", notInWarp[i]);
                Assert.Fail(error);
            }

            var didNotArrive = didWarp.Filter(s => Dist(s) > 7500+500);
            if (didNotArrive.Length != 0)
            {
                string error = $"{didNotArrive.Length} fleet ships did not arrive at destination!";
                Log.Write(error);
                for (int i = 0; i < didNotArrive.Length && i < 10; ++i)
                    Print("DID_NOT_ARRIVE", didNotArrive[i]);
                Assert.Fail(error);
            }

            Log.Write("All ships arrived at destination");
        }
    }
}
