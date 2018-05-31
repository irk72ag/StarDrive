﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using Ship_Game;
using Ship_Game.Gameplay;

namespace SDUnitTests
{
    [TestFixture]
    public class TestArrayT
    {
        [Test]
        public void TestArrayAdd()
        {
            var arr = new Array<int>();
            arr.Add(1);
            Assert.AreEqual(arr.Count, 1, "Count should be 1");
            Assert.AreEqual(arr.Capacity, 4, "Capacity should be 4");
            arr.Add(2);
            arr.Add(3);
            arr.Add(4);
            arr.Add(5);
            Assert.AreEqual(5, arr.Count, "Count should be 5");
            Assert.AreEqual(8, arr.Capacity, "Capacity should grow aligned to 4, expected 8");
        }

        [Test]
        public void TestArrayContains()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };
            Assert.IsTrue(arr.Contains("c"), "Contains should work for existing items");
            Assert.IsFalse(arr.Contains("x"), "Contains should not give false positives");
            arr.Add(null);
            Assert.IsTrue(arr.Contains(null), "Contains must detect null properly");

            var obj = "c";
            var refs = new Array<string> { "a", "b", "c", "d" };
            refs.Add(obj);
            Assert.IsTrue(refs.ContainsRef(obj), "Contains should work for existing items");
            Assert.IsFalse(refs.ContainsRef("x"), "Contains should not give false positives");
            refs.Add(null);
            Assert.IsTrue(refs.ContainsRef(null), "Contains must detect null properly");
        }

        [Test]
        public void TestArrayRemoveAll()
        {
            var arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => true);
            Assert.AreEqual(0, arr.Count, "RemoveAll true should erase all elements");

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => x % 2 == 1);
            Assert.AreEqual(4, arr.Count, "RemoveAll odd should remove half the elements");
        }

        [Test]
        public void TestToArrayList()
        {
            var arr = new[] { "a", "b", "c" };
            Array<string> arr1 = new Array<string>();
            arr1.AddRange(arr);
            Assert.AreEqual(arr, arr1);
            Assert.Throws<InvalidOperationException>(() => arr1.ToArrayList());

            var arr2 = ((ICollection<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);
        }
        
        [Test]
        public void TestToArray()
        {
            var arr = new[] { "a", "b", "c" };
            var arr1 = new Array<string>();
            arr1.AddRange(arr);
            Assert.AreEqual(arr, arr1);

            string[] arr2 = ((ICollection<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);
        }
    }
}
