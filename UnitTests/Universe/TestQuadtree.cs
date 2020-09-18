﻿using System;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestQuadTree : TestQuadtreeCommon
    {
        [TestMethod]
        public void BasicInsert()
        {
            TestBasicInsert(new Quadtree(100_000));
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            TestFindNearbySingle(new Quadtree(10_000));
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            TestFindNearbyMulti(new Quadtree(10_000));
        }

        [TestMethod]
        public void FindNearbyTypeFilter()
        {
            TestFindNearbyTypeFilter(new Quadtree(10_000));
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            TestTreeUpdatePerformance(new Quadtree(1_000_000));
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            TestTreeSearchPerformance(new Quadtree(500_000));
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            TestConcurrentUpdateAndSearch(new Quadtree(500_000));
        }

        [TestMethod]
        public void TreeCollisionPerformance()
        {
            TestTreeCollisionPerformance(new Quadtree(50_000));
        }
    }
}
