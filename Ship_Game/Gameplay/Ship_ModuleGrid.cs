﻿using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {
        public ShipModule[] ModuleSlotList;
        private ShipModule[] SparseModuleGrid;   // single dimensional grid, for performance reasons
        private ShipModule[] ExternalModuleGrid; // only contains external modules
        public int NumExternalSlots { get; private set; }
        private int GridWidth;
        private int GridHeight;
        private Vector2 GridOrigin; // local origin, eg -32, -48

        private void CreateModuleGrid()
        {
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

            int numShields = 0;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                Vector2 topLeft = module.Position;
                var botRight = new Vector2(topLeft.X + module.XSIZE * 16.0f,
                                           topLeft.Y + module.YSIZE * 16.0f);
                if (topLeft.X  < minX) minX = topLeft.X;
                if (topLeft.Y  < minY) minY = topLeft.Y;
                if (botRight.X > maxX) maxX = botRight.X;
                if (botRight.Y > maxY) maxY = botRight.Y;

                if (module.shield_power_max > 0f)
                    ++numShields;
            }

            GridOrigin = new Vector2(minX, minY);
            GridWidth  = (int)(maxX - minX) / 16;
            GridHeight = (int)(maxY - minY) / 16;
            SparseModuleGrid   = new ShipModule[GridWidth * GridHeight];
            ExternalModuleGrid = new ShipModule[GridWidth * GridHeight];

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                UpdateGridSlot(SparseModuleGrid, ModuleSlotList[i], becameActive: true);
            }

            // build active shields list
            Shields = new ShipModule[numShields];
            numShields = 0;
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (module.shield_power_max > 0f)
                    Shields[numShields++] = module;
            }
        }

        private void AddExternalModule(ShipModule module, int x, int y, int quadrant)
        {
            if (module.isExternal)
                return;
            ++NumExternalSlots;
            module.isExternal = true;
            module.quadrant   = quadrant;
            UpdateGridSlot(ExternalModuleGrid, x, y, module, becameActive: true);
        }

        private void RemoveExternalModule(ShipModule module, int x, int y)
        {
            if (!module.isExternal)
                return;
            --NumExternalSlots;
            module.isExternal = false;
            module.quadrant   = 0;
            UpdateGridSlot(ExternalModuleGrid, x, y, module, becameActive: false);
        }

        private bool IsModuleInactiveAt(int x, int y)
        {
            ShipModule module = (uint)x < GridWidth && (uint)y < GridHeight ? SparseModuleGrid[x + y*GridWidth] : null;
            return module == null || !module.Active;
        }

        private int GetQuadrantEstimate(int x, int y)
        {
            Vector2 dir = new Vector2(x - (GridWidth / 2), y - (GridHeight / 2)).Normalized();
            if (dir.X <= 0f && dir.Y <= 0f)
                return 0;
            if (Math.Abs(dir.Y) <= 0.5f)
                return dir.X <= 0f ? 1 /*top*/ : 3 /*bottom*/;
            return dir.X <= 0f ? 4 /*left*/ : 2 /*right*/;
        }

        private bool CheckIfShouldBeExternal(int x, int y)
        {
            if (!GetModuleAt(SparseModuleGrid, x, y, out ShipModule module))
                return false;

            SlotPointAt(module.Position, out x, out y); // now get the true topleft root coordinates of module
            bool shouldBeExternal = module.Active &&
                                    IsModuleInactiveAt(x, y - 1) ||
                                    IsModuleInactiveAt(x - 1, y) ||
                                    IsModuleInactiveAt(x + module.XSIZE, y) ||
                                    IsModuleInactiveAt(x, y + module.YSIZE);
            if (shouldBeExternal)
            {
                if (!module.isExternal)
                    AddExternalModule(module, x, y, GetQuadrantEstimate(x, y));
                return true;
            }
            if (module.isExternal)
                RemoveExternalModule(module, x, y);
            return false;
        }

        private void InitExternalSlots()
        {
            NumExternalSlots = 0;
            for (int y = 0; y < GridHeight; ++y)
            {
                for (int x = 0; x < GridWidth; ++x)
                {
                    int idx = x + y*GridWidth;
                    if (ExternalModuleGrid[idx] != null || !CheckIfShouldBeExternal(x, y))
                        continue;
                    // ReSharper disable once PossibleNullReferenceException
                    x += ExternalModuleGrid[idx].XSIZE - 1; // skip slots that span this module
                }
            }
        }

        // updates the isExternal status of a module, depending on whether it died or resurrected
        public void UpdateExternalSlots(ShipModule module, bool becameActive)
        {
            SlotPointAt(module.Position, out int x, out int y);

            if (becameActive) // we resurrected, so add us to external module grid and update all surrounding slots
                AddExternalModule(module, x, y, GetQuadrantEstimate(x, y));
            else // we turned inactive, so clear self from external module grid and 
                RemoveExternalModule(module, x, y);

            CheckIfShouldBeExternal(x, y - 1);
            CheckIfShouldBeExternal(x - 1, y);
            CheckIfShouldBeExternal(x + module.XSIZE, y);
            CheckIfShouldBeExternal(x, y + module.YSIZE);
        }

        private void UpdateGridSlot(ShipModule[] sparseGrid, int gridX, int gridY, ShipModule module, bool becameActive)
        {
            int endX = gridX + module.XSIZE, endY = gridY + module.YSIZE;
            for (int y = gridY; y < endY; ++y)
                for (int x = gridX; x < endX; ++x)
                    sparseGrid[x + y * GridWidth] = becameActive ? module : null;
        }
        private void UpdateGridSlot(ShipModule[] sparseGrid, ShipModule module, bool becameActive)
        {
            SlotPointAt(module.Position, out int x, out int y);
            UpdateGridSlot(sparseGrid, x, y, module, becameActive);
        }

        private void SlotPointAt(Vector2 moduleLocalPos, out int x, out int y)
        {
            Vector2 offset = moduleLocalPos - GridOrigin;
            x = (int)(offset.X / 16f);
            y = (int)(offset.Y / 16f);
        }

        public bool TryGetModule(Vector2 pos, out ShipModule module)
        {
            SlotPointAt(pos, out int x, out int y);
            return GetModuleAt(SparseModuleGrid, x, y, out module);
        }

        // safe and fast module lookup by x,y where coordinates (0,1) (2,1) etc
        private bool GetModuleAt(ShipModule[] sparseGrid, int x, int y, out ShipModule module)
        {
            module = (uint)x < GridWidth && (uint)y < GridHeight ? sparseGrid[x + y * GridWidth] : null;
            return module != null;
        }





        // The simplest form of collision against shields. This is handled in all other HitTest functions
        private ShipModule HitTestShields(Vector2 worldHitPos, float hitRadius)
        {
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldPower > 0f && shield.HitTestShield(worldHitPos, hitRadius))
                    return shield;
            }
            return null;
        }

        // Slightly more complicated ray-collision against shields
        private ShipModule RayHitTestShields(Vector2 worldStartPos, Vector2 worldEndPos, float rayRadius)
        {
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule shield = Shields[i];
                if (shield.ShieldPower > 0f && shield.RayHitTestShield(worldStartPos, worldEndPos, rayRadius))
                    return shield;
            }
            return null;
        }




        
        // Converts a world position to a grid local position (such as [16f,32f])
        public Vector2 WorldToGridLocal(Vector2 worldPoint)
        {
            Vector2 offset = worldPoint - Center;
            Vector2 rotated = offset.RotateAroundPoint(Vector2.Zero, -Rotation);
            return rotated - GridOrigin;
        }

        public Point WorldToGridLocalPoint(Vector2 worldPoint)
        {
            Vector2 local = WorldToGridLocal(worldPoint);
            return new Point((int)(local.X / 16f), (int)(local.Y / 16f));
        }

        public Point WorldToGridLocalPointClipped(Vector2 worldPoint)
        {
            Point pt = WorldToGridLocalPoint(worldPoint);
            if (pt.X < 0) pt.X = 0; else if (pt.X >= GridWidth)  pt.X = GridWidth  - 1;
            if (pt.Y < 0) pt.Y = 0; else if (pt.Y >= GridHeight) pt.Y = GridHeight - 1;
            return pt;
        }

        public Vector2 GridLocalToWorld(Vector2 localPoint)
        {
            Vector2 centerLocal = GridOrigin + localPoint;
            return centerLocal.RotateAroundPoint(Vector2.Zero, Rotation) + Center;
        }

        public Vector2 GridLocalPointToWorld(Point gridLocalPoint)
        {
            return GridLocalToWorld(new Vector2(gridLocalPoint.X * 16f, gridLocalPoint.Y * 16f));
        }





        // @note Only Active (alive) modules are in ExternalSlots. This is because ExternalSlots get
        //       updated every time a module dies. The code for that is in ShipModule.cs
        // @note This method is optimized for fast instant lookup, with a semi-optimal fallback floodfill search
        // @note Ignores shields !
        public ShipModule FindClosestUnshieldedModule(Vector2 worldPoint)
        {
            if (NumExternalSlots == 0) return null;

            ShipModule[] grid = ExternalModuleGrid;
            Point pt = WorldToGridLocalPointClipped(worldPoint);
            int width = GridWidth;
            int lastX = width - 1, lastY = GridHeight - 1;
            int minX = pt.X, minY = pt.Y, maxX = pt.X, maxY = pt.Y;
            ShipModule m;
            if ((m = grid[minX + minY*width]) != null && m.Active) return m;

            for (;;)
            {
                bool didExpand = false;
                if (minX > 0f) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y) if ((m = grid[minX + y*width]) != null && m.Active) return m;
                }
                if (maxX < lastX) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y) if ((m = grid[maxX + y*width]) != null && m.Active) return m;
                }
                if (minY > 0f) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x) if ((m = grid[x + minY*width]) != null && m.Active) return m;
                }
                if (maxY < lastY) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x) if ((m = grid[x + maxY*width]) != null && m.Active) return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }


        // find the first module that falls under the hit radius at given position
        public ShipModule HitTestSingle(Vector2 worldHitPos, float hitRadius, bool ignoreShields = false)
        {
            if (NumExternalSlots == 0) return null;
            if (!ignoreShields)
            {
                ShipModule shield = HitTestShields(worldHitPos, hitRadius);
                if (shield != null) return shield;
            }

            Point first = WorldToGridLocalPointClipped(worldHitPos - new Vector2(hitRadius));
            Point last  = WorldToGridLocalPointClipped(worldHitPos + new Vector2(hitRadius));
            ShipModule[] grid = SparseModuleGrid;
            int minX = (first.X + last.X) / 2;
            int minY = (first.Y + last.Y) / 2;
            int maxX = minX, maxY = minY;
            int width = GridWidth;
            ShipModule m;
            if ((m = grid[minX + minY*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, hitRadius)) return m;

            for (;;)
            {
                bool didExpand = false;
                if (minX > first.X) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[minX + y*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, hitRadius)) return m;
                }
                if (maxX < last.X) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[maxX + y*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, hitRadius)) return m;
                }
                if (minY > first.Y) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + minY*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, hitRadius)) return m;
                }
                if (maxY < last.Y) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + maxY*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, hitRadius)) return m;
                }
                if (!didExpand) return null; // aargh, looks like we didn't find any!
            }
        }


        // Code gods, I pray for thy forgiveness for this copy-paste -T_T-
        // This was done solely for performance reasons. This method gets called
        // every time an exploding projectile hits a ship. So it gets called for every missile impact
        public void DamageModulesInRange(GameplayObject damageSource, float damageAmount, 
                                         Vector2 worldHitPos, float damageRadius, bool ignoreShields)
        {
            float damageTracker = damageAmount;
            if (!ignoreShields)
            {
                for (int i = 0; i < Shields.Length; ++i)
                {
                    ShipModule module = Shields[i];
                    if (module.ShieldPower > 0f && module.HitTestShield(worldHitPos, damageRadius) && 
                        module.ApplyRadialDamage(damageSource, worldHitPos, damageRadius, ref damageTracker))
                            return; // no more damage to dish, exit early
                }
            }

            Point first = WorldToGridLocalPointClipped(worldHitPos - new Vector2(damageRadius));
            Point last  = WorldToGridLocalPointClipped(worldHitPos + new Vector2(damageRadius));
            ShipModule[] grid = SparseModuleGrid;
            int minX = (first.X + last.X) / 2;
            int minY = (first.Y + last.Y) / 2;
            int maxX = minX, maxY = minY;
            int width = GridWidth;
            ShipModule m;
            if ((m = grid[minX + minY*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, damageRadius)
                && m.ApplyRadialDamage(damageSource, worldHitPos, damageRadius, ref damageTracker)) return;

            for (;;)
            {
                bool didExpand = false;
                if (minX > first.X) { // test all modules to the left
                    --minX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[minX + y*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, damageRadius)
                            && m.ApplyRadialDamage(damageSource, worldHitPos, damageRadius, ref damageTracker)) return;
                }
                if (maxX < last.X) { // test all modules to the right
                    ++maxX; didExpand = true;
                    for (int y = minY; y <= maxY; ++y)
                        if ((m = grid[maxX + y*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, damageRadius)
                            && m.ApplyRadialDamage(damageSource, worldHitPos, damageRadius, ref damageTracker)) return;
                }
                if (minY > first.Y) { // test all top modules
                    --minY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + minY*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, damageRadius)
                            && m.ApplyRadialDamage(damageSource, worldHitPos, damageRadius, ref damageTracker)) return;
                }
                if (maxY < last.Y) { // test all bottom modules
                    ++maxY; didExpand = true;
                    for (int x = minX; x <= maxX; ++x)
                        if ((m = grid[x + maxY*width]) != null && m.Active && m.HitTestNoShields(worldHitPos, damageRadius)
                            && m.ApplyRadialDamage(damageSource, worldHitPos, damageRadius, ref damageTracker)) return;
                }
                if (!didExpand) return; // wellll, looks like we're done here!
            }
        }




        // perform a raytrace from point a to point b, visiting all grid points between them!
        private static ShipModule RayTrace(Point a, Point b, ShipModule[] grid, int gridWidth, int gridHeight)
        {
            int dx = Math.Abs(b.X - a.X);
            int dy = Math.Abs(b.Y - a.Y);
            int x = a.X;
            int y = a.Y;
            int n = 1 + dx + dy;
            int kx = (b.X > a.X) ? 1 : -1;
            int ky = (b.Y > a.Y) ? 1 : -1;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;
            for (; n > 0; --n)
            {
                ShipModule module = grid[x + y*gridWidth];
                if (module != null) return module;
                if (error > 0)
                {
                    if (0 < x && x < gridWidth) x += kx;
                    error -= dy;
                }
                else
                {
                    if (0 < y && y < gridHeight) y += ky;
                    error += dx;
                }
            }
            return null;
        }

        public ShipModule RayHitTestSingle(Vector2 startPos, Vector2 endPos, float rayRadius, bool ignoreShields = false)
        {
            if (!ignoreShields)
            {
                ShipModule shield = RayHitTestShields(startPos, endPos, rayRadius);
                if (shield != null) return shield;
            }

            Point a = WorldToGridLocalPoint(startPos);
            Point b = WorldToGridLocalPoint(endPos);
            if (MathExt.ClipLineWithBounds(GridWidth, GridHeight, a, b, ref a, ref b))
            {
                #if DEBUG
                    if (Empire.Universe.Debug)
                    {
                        Vector2 localA = WorldToGridLocal(startPos);
                        Vector2 localB = WorldToGridLocal(endPos);
                        AddGridLocalDebugLine(5f, localA, localB);
                    }
                #endif
                // @todo Make use of rayRadius to improve raytrace precision
                return RayTrace(a, b, SparseModuleGrid, GridWidth, GridHeight);
            }
            return null;
        }


        // find ShipModules that collide with a this wide RAY
        // direction must be normalized!!
        // results are sorted by distance
        // @note Don't bother optimizing this. It's only used during armour piercing, which is super rare.
        public Array<ShipModule> RayHitTestModules(
            Vector2 startPos, Vector2 direction, float distance, float rayRadius)
        {
            Vector2 endPos = startPos + direction * distance;

            var modules = new Array<ShipModule>();
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (module.Health > 0f && module.RayHitTestNoShield(startPos, endPos, rayRadius))
                    modules.Add(module);
            }
            modules.Sort(module => startPos.SqDist(module.Position));
            return modules;
        }




        // Refactor by RedFox: Picks a random internal module in search range (squared) of the projectile
        // -- Higher crew level means the missile will pick the most optimal target module ;) --
        private ShipModule TargetRandomInternalModule(Vector2 projPos, int level, float sqSearchRange)
        {
            ShipModule[] modules = ModuleSlotList.FilterBy(m => m.Health > 0f && projPos.SqDist(m.Center) < sqSearchRange);
            if (modules.Length == 0)
                return null;

            if (level > 1)
            {
                // Sort Descending (-), so first element is the module with greatest TargettingValue
                modules.Sort(m => -m.ModuleTargettingValue);
            }

            // higher levels lower the limit, which causes a better random pick
            int limit = modules.Length / (level + 1);
            return modules[RandomMath.InRange(limit)];
        }

        // This is called for guided weapons to pick a new target
        public ShipModule GetRandomInternalModule(Weapon source)
        {
            Vector2 center    = source.Owner?.Center ?? source.Center;
            int level         = source.Owner?.Level  ?? 0;
            float searchRange = source.Range + 100;
            return TargetRandomInternalModule(center, level, searchRange*searchRange);
        }

        // This is called for initial missile guidance ChooseTarget(), so range is not that important
        public ShipModule GetRandomInternalModule(Projectile source)
        {
            Vector2 projPos = source.Owner?.Center ?? source.Center;
            int level       = source.Owner?.Level  ?? 0;
            float searchRange = projPos.SqDist(Center) + 48*48; // only pick modules that are "visible" to the projectile
            return TargetRandomInternalModule(projPos, level, searchRange);
        }
    }
}
