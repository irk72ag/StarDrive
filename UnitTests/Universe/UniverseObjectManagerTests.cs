﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe
{
    [TestClass]
    public class UniverseObjectManagerTests : StarDriveTest
    {
        public UniverseObjectManagerTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void SpawnedShipIsAddedToEmpireAndUniverse()
        {
            var spawnedShip = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(Player.OwnedShips.Count, 1, "Ship was not added to Player's Empire OwnedShips");
            Assert.IsTrue(UState.Objects.ContainsShip(spawnedShip.Id), "Ship was not added to UniverseObjectManager");
        }

        [TestMethod]
        public void DesignShipIsNotAddedToEmpireAndUniverse()
        {
            IShipDesign design = ResourceManager.Ships.GetDesign("Vulcan Scout");
            var mustNotBeAddedToEmpireOrUniverse = new DesignShip(UState, design as ShipDesign);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(0, Player.OwnedShips.Count, "DesignShip should not be added to Player's Empire OwnedShips");
            Assert.AreEqual(0, UState.Objects.NumShips, "DesignShip should not be added to UniverseObjectManager");
        }

        [TestMethod]
        public void ShipsWithNoModulesShouldNotBeAddedToEmpire()
        {
            ShipDesign design = ResourceManager.Ships.GetDesign("Vulcan Scout").GetClone(null);
            design.SetDesignSlots(Empty<DesignSlot>.Array);

            // somehow we manage to create one
            var emptyTemplate = new DesignShip(UState, design);
            var mustNotBeAddedToEmpireOrUniverse = Ship.CreateShipAtPoint(UState, emptyTemplate, Player, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(0, Player.OwnedShips.Count, "Ship with empty modules should not be added to Player's Empire OwnedShips");
            Assert.AreEqual(0, UState.Objects.NumShips, "Ship with empty modules should not be added to UniverseObjectManager");
        }
    }
}
