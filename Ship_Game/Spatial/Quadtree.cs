﻿using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public struct SpatialObj
    {
        public GameplayObject Obj;
        public float X, Y, LastX, LastY;
        public int Loyalty; // if loyalty == 0, then this is a STATIC world object !!!
        public int LastCollided;

        public Vector2 Center;
        public float Radius;
        public GameObjectType Type;

        public bool OverlapsQuads; // does it overlap multiple quads?

        public bool Is(GameObjectType flags) => (Type & flags) != 0;

        public SpatialObj(GameplayObject go)
        {
            Obj  = go;
            Type = go.Type;
            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)go;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
                Center = default(Vector2);
                Radius = 0f;
            }
            else
            {
                Center = Obj.Center;
                Radius   = Obj.Radius;
                X        = Center.X - Radius;
                Y        = Center.Y - Radius;
                LastX    = Center.X + Radius;
                LastY    = Center.Y + Radius;
            }
            Loyalty       = go.GetLoyaltyId();
            LastCollided  = 0;
            OverlapsQuads = false;
        }

        public SpatialObj(GameplayObject go, float radius)
        {
            Obj           = go;
            Type          = go.Type;
            Center        = go.Center;
            Radius        = radius;
            X             = Center.X - radius;
            Y             = Center.Y - radius;
            LastX         = Center.X + radius;
            LastY         = Center.Y + radius;
            Loyalty       = go.GetLoyaltyId();
            LastCollided  = 0;
            OverlapsQuads = false;
        }

        public void UpdateBounds() // Update SpatialObj bounding box
        {
            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)Obj;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
            }
            else
            {
                Center   = Obj.Center;
                Radius   = Obj.Radius;
                X        = Center.X - Radius;
                Y        = Center.Y - Radius;
                LastX    = Center.X + Radius;
                LastY    = Center.Y + Radius;
            }
        }

        public float HitTestBeam(ref SpatialObj target, out ShipModule hitModule)
        {
            var beam = (Beam)Obj;
            //if (obj.Owner == target.Obj) // actually we dont need this here because of Loyalty check
            //    return 0f; // ignore our parent ship
            ++GlobalStats.BeamTests;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;

            if (target.Is(GameObjectType.Ship)) // beam-ship is special collision
            {
                var ship = (Ship)target.Obj;
                hitModule = ship.RayHitTestSingle(beamStart, beamEnd, 8f, beam.IgnoresShields);
                if (hitModule == null)
                    return 0f;
                return hitModule.Center.FindClosestPointOnLine(beamStart, beamEnd).Distance(beamStart);
                //float hitDist = hitModule.Center.RayCircleIntersect(hitModule.Radius, beamStart, beamEnd);
                //if (hitDist < 0f) // beam barely glanced the module
                //    return hitModule.Center.FindClosestPointOnLine(beamStart, beamEnd).Distance(beamStart);
                //return hitDist;
            }
            hitModule = null;
            return target.Center.RayCircleIntersect(target.Radius, beamStart, beamEnd);
        }

        // assumes THIS is a projectile
        public bool HitTestProj(ref SpatialObj target, out ShipModule hitModule)
        {
            hitModule = null;
            float dx = Center.X - target.Center.X;
            float dy = Center.Y - target.Center.Y;
            float ra = Radius, rb = target.Radius;
            if ((dx*dx + dy*dy) >= (ra*ra + rb*rb))
                return false;
            if ((target.Type & GameObjectType.Ship) == 0) // target not a ship, collision success
                return true;

            // ship collision, target modules instead
            var proj = (Projectile)Obj;
            var ship = (Ship)target.Obj;

            // give a lot of leeway here; if we fall short, collisions wont work right
            float maxDistPerFrame = proj.Velocity.Length() / 30.0f; // this actually depends on the framerate...
            if (maxDistPerFrame > 15.0f) // ray collision
            {
                Vector2 dir     = proj.Velocity.Normalized();
                Vector2 prevPos = Center - (dir*maxDistPerFrame);
                hitModule = ship.RayHitTestSingle(prevPos, Center, Radius, proj.IgnoresShields);
            }
            else
            {
                hitModule = ship.HitTestSingle(proj.Center, proj.Radius, proj.IgnoresShields);
            }
            return hitModule != null;
        }

        public bool HitTestNearby(ref SpatialObj b)
        {
            float dx = Center.X - b.Center.X;
            float dy = Center.Y - b.Center.Y;
            float ra = Radius, rb = b.Radius;
            return (dx*dx + dy*dy) < (ra*ra + rb*rb);
        }
    }


    ///////////////////////////////////////////////////////////////////////////////////////////


    public struct SpatialCollision // two spatial objects that collided
    {
        public GameplayObject Obj1, Obj2;
    }

    public sealed class Quadtree : IDisposable
    {
        private static readonly SpatialObj[] NoObjects = new SpatialObj[0];

        public readonly int   Levels;
        public readonly float FullSize;

        public const int CellThreshold = 4;
        private Node Root;
        private int FrameId;

        ///////////////////////////////////////////////////////////////////////////////////////////
        private class Node
        {
            public readonly float X, Y, LastX, LastY;
            public Node NW, NE, SE, SW;
            public int Count;
            public SpatialObj[] Items;
            public Node(float x, float y, float lastX, float lastY)
            {
                X = x; Y = y;
                LastX = lastX; LastY = lastY;
                NW = null; NE = null; SE = null; SW = null;
                Count = 0;
                Items = NoObjects;
            }
            public void Add(ref SpatialObj obj)
            {
                if (Items.Length == Count)
                {
                    if (Count == 0) Items = new SpatialObj[CellThreshold];
                    else
                    {
                        //Array.Resize(ref Items, Count * 2);
                        var newItems = new SpatialObj[Count * 2];
                        for (int i = 0; i < Count; ++i)
                            newItems[i] = Items[i];
                        Items = newItems;
                    }
                }
                Items[Count++] = obj;
            }
            public void RemoveAtSwapLast(int index)
            {
                ref SpatialObj last = ref Items[--Count];
                Items[index] = last;
                last.Obj = null;
                if (Count == 0) Items = NoObjects;
            }
            public bool Overlaps(ref Vector2 topleft, ref Vector2 topright)
            {
                return X <= topright.X && LastX > topleft.X
                    && Y <= topright.Y && LastY > topleft.Y;
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////////////

        // Create a quadtree to fit the universe
        public Quadtree(float universeSize, float smallestCell = 512f)
        {
            Levels       = 1;
            FullSize     = smallestCell;
            while (FullSize < universeSize)
            {
                ++Levels;
                FullSize *= 2;
            }
            Reset();
        }
        ~Quadtree() { Dispose(); }

        public void Dispose()
        {
            Root = null;
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = FullSize / 2;
            Root = new Node(-half, -half, +half, +half);
        }

        private static void SplitNode(Node node, int level)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            node.NW = new Node(node.X, node.Y, midX,       midY);
            node.NE = new Node(midX,   node.Y, node.LastX, midY);
            node.SE = new Node(midX,   midY,   node.LastX, node.LastY);
            node.SW = new Node(node.X, midY,   midX,       node.LastY);

            int count = node.Count;
            SpatialObj[] arr = node.Items;
            node.Items = NoObjects;
            node.Count = 0;

            // reinsert all items:
            for (int i = 0; i < count; ++i)
                InsertAt(node, level, ref arr[i]);
        }

        private static Node PickSubQuadrant(Node node, ref SpatialObj obj)
        {
            float midX = (node.X + node.LastX) / 2;
            float midY = (node.Y + node.LastY) / 2;

            obj.OverlapsQuads = false;
            if (obj.X < midX && obj.LastX < midX) // left
            {
                if (obj.Y <  midY && obj.LastY < midY) return node.NW; // top left
                if (obj.Y >= midY)                     return node.SW; // bot left
            }
            else if (obj.X >= midX) // right
            {
                if (obj.Y <  midY && obj.LastY < midY) return node.NE; // top right
                if (obj.Y >= midY)                     return node.SE; // bot right
            }
            obj.OverlapsQuads = true;
            return null; // obj does not perfectly fit inside a quadrant
        }

        private static void InsertAt(Node node, int level, ref SpatialObj obj)
        {
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                {
                    node.Add(ref obj);
                    return;
                }

                if (node.NW != null)
                {
                    Node quad = PickSubQuadrant(node, ref obj);
                    if (quad != null)
                    {
                        node = quad; // go deeper!
                        --level;
                        continue;
                    }
                }

                // item belongs to this node
                node.Add(ref obj);

                // actually, are we maybe over Threshold and should Divide ?
                if (node.NW == null && node.Count >= CellThreshold)
                    SplitNode(node, level);
                return;
            }
        }

        public void Insert(GameplayObject go)
        {
            var obj = new SpatialObj(go);
            InsertAt(Root, Levels, ref obj);
        }

        private static bool RemoveAt(Node node, GameplayObject go)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                if (node.Items[i].Obj != go) continue;
                node.RemoveAtSwapLast(i);
                return true;
            }
            return node.NW != null
                && (RemoveAt(node.NW, go) || RemoveAt(node.NE, go)
                ||  RemoveAt(node.SE, go) || RemoveAt(node.SW, go));
        }

        public void Remove(GameplayObject go) => RemoveAt(Root, go);

        private void UpdateNode(Node node, int level)
        {
            if (node.Count > 0)
            {
                float nx = node.X, ny = node.Y; // L1 cache warm node bounds
                float nlastX = node.LastX, nlastY = node.LastY;

                for (int i = 0; i < node.Count; ++i)
                {
                    ref SpatialObj obj = ref node.Items[i]; // .Items may be modified by InsertAt and RemoveAtSwapLast
                    if (obj.Loyalty == 0)
                        continue; // seems to be a static world object, so don't bother updating

                    if (obj.Obj.Active == false)
                    {
                        node.RemoveAtSwapLast(i--);
                        continue;
                    }

                    obj.UpdateBounds();

                    //bool isFullyOutsideBounds = obj.LastX < nx || obj.LastY < ny ||
                    //                            obj.X > nlastX || obj.Y > nlastY;
                    //bool insideBoundsRect = nx <= obj.X && ny <= obj.Y && 
                    //                        obj.LastX < nlastX && obj.LastY < nlastY;
                    //if (!insideBoundsRect)
                    if (obj.X < nx || obj.Y < ny || obj.LastX > nlastX || obj.LastY > nlastY) // out of Node bounds??
                    {
                        SpatialObj reinsert = obj;
                        node.RemoveAtSwapLast(i--);
                        InsertAt(Root, Levels, ref reinsert);
                    }
                    // we previously overlapped the boundary, so insertion was at parent node;
                    // ... so now check if we're completely inside a subquadrant and reinsert into it
                    else if (obj.OverlapsQuads)
                    {
                        Node quad = PickSubQuadrant(node, ref obj);
                        if (quad != null)
                        {
                            SpatialObj reinsert = obj;
                            node.RemoveAtSwapLast(i--);
                            InsertAt(quad, level-1, ref reinsert);
                        }
                    }
                }
            }
            if (node.NW != null)
            {
                int sublevel = level - 1;
                UpdateNode(node.NW, sublevel);
                UpdateNode(node.NE, sublevel);
                UpdateNode(node.SE, sublevel);
                UpdateNode(node.SW, sublevel);
            }
        }

        private static int RemoveEmptyChildNodes(Node node)
        {
            if (node.NW == null)
                return node.Count;

            int subItems = 0;
            subItems += RemoveEmptyChildNodes(node.NW);
            subItems += RemoveEmptyChildNodes(node.NE);
            subItems += RemoveEmptyChildNodes(node.SE);
            subItems += RemoveEmptyChildNodes(node.SW);
            if (subItems == 0) // discard these empty quads:
            {
                node.NW = node.NE = node.SE = node.SW = null;
            }
            return node.Count + subItems;
        }

        public void UpdateAll()
        {
            ++FrameId;
            UpdateNode(Root, Levels);
            RemoveEmptyChildNodes(Root);
        }


        // finds the node that fully encloses this spatial object
        private Node FindEnclosingNode(ref SpatialObj obj)
        {
            int level = Levels;
            Node node = Root;
            for (;;)
            {
                if (level <= 1) // no more subdivisions possible
                    break;
                Node quad = PickSubQuadrant(node, ref obj);
                if (quad == null)
                    break;
                node = quad; // go deeper!
                --level;
            }
            return node;
        }


        // regular collision; it doesn't matter which SpatialObj collides, just return the first
        private static bool CollideShipAtNode(Node node, int frameId, ref SpatialObj ship)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj proj = ref node.Items[i]; // potential projectile ?
                if (frameId      != proj.LastCollided && // already collided this frame
                    proj.Loyalty != ship.Loyalty      && // friendlies don't collide
                    (proj.Type & GameObjectType.Proj) != 0 && // only collide with projectiles
                    (proj.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(ref ship, out ShipModule hitModule))
                {
                    proj.LastCollided = frameId;
                    ship.LastCollided = frameId;
                    HandleProjCollision(proj.Obj as Projectile, hitModule ?? ship.Obj);
                    return true;
                }
            }
            if (node.NW == null)
                return false;
            return CollideShipAtNode(node.NW, frameId, ref ship)
                || CollideShipAtNode(node.NE, frameId, ref ship)
                || CollideShipAtNode(node.SE, frameId, ref ship)
                || CollideShipAtNode(node.SW, frameId, ref ship);
        }

        // regular collision; it doesn't matter which SpatialObj collides, just return the first
        private static bool CollideProjAtNode(Node node, int frameId, ref SpatialObj proj)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (frameId      != item.LastCollided && // already collided this frame
                    item.Loyalty != proj.Loyalty      && // friendlies don't collide
                    (item.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(ref item, out ShipModule hitModule))
                {
                    item.LastCollided = frameId;
                    proj.LastCollided = frameId;
                    HandleProjCollision(proj.Obj as Projectile, hitModule ?? item.Obj);
                    return true;
                }
            }
            if (node.NW == null)
                return false;
            return CollideProjAtNode(node.NW, frameId, ref proj)
                || CollideProjAtNode(node.NE, frameId, ref proj)
                || CollideProjAtNode(node.SE, frameId, ref proj)
                || CollideProjAtNode(node.SW, frameId, ref proj);
        }

        private struct BeamHitResult
        {
            public GameplayObject Collided;
            public float Distance;
            public Node At;
            public int Idx;
        }

        // for beams it's important to only collide the CLOSEST object, so the lookup is exhaustive
        private static void CollideBeamAtNode(Node node, int frameId, ref SpatialObj beam, ref BeamHitResult result)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (frameId      != item.LastCollided && // already collided this frame
                    item.Loyalty != beam.Loyalty      && // friendlies don't collide
                    (item.Type & GameObjectType.Beam) == 0) // forbid beam-beam collision            
                {
                    float dist = beam.HitTestBeam(ref item, out ShipModule hitModule);
                    if (0f < dist && dist < result.Distance) // check if closest match
                    {
                        result.Distance = dist;
                        result.Collided = hitModule ?? item.Obj;
                        result.At  = node;
                        result.Idx = i;
                    }
                }
            }
            if (node.NW != null)
            {
                CollideBeamAtNode(node.NW, frameId, ref beam, ref result);
                CollideBeamAtNode(node.NW, frameId, ref beam, ref result);
                CollideBeamAtNode(node.SE, frameId, ref beam, ref result);
                CollideBeamAtNode(node.SW, frameId, ref beam, ref result);
            }
        }

        private static void CollideBeamAtNode(Node node, int frameId, ref SpatialObj beam)
        {
            // this whole thing is quite ugly... but we need some way to set .LastCollided
            BeamHitResult hit = default(BeamHitResult);
            hit.Distance = 9999999f;
            CollideBeamAtNode(node, frameId, ref beam, ref hit);
            if (hit.Collided == null)
                return;
            hit.At.Items[hit.Idx].LastCollided = frameId;
            beam.LastCollided                  = frameId;
            HandleBeamCollision(beam.Obj as Beam, hit.Collided, hit.Distance);
        }

        public void CollideAll() => CollideAllAt(Root, FrameId);

        private static void CollideAllAt(Node node, int frameId)
        {
            if (node.NW != null) // depth first approach, to early filter LastCollided
            {
                CollideAllAt(node.NW, frameId);
                CollideAllAt(node.NE, frameId);
                CollideAllAt(node.SE, frameId);
                CollideAllAt(node.SW, frameId);
            }

            int count = node.Count;
            if (count <= 1) // can't collide with self :)
                return;
            SpatialObj[] items = node.Items;
            for (int i = 0; i < count; ++i)
            {
                ref SpatialObj so = ref items[i];
                if (frameId == so.LastCollided || so.Obj.Active == false)
                    continue; // already collided inside this loop

                // each collision instigator type has a very specific recursive handler
                if      ((so.Type & GameObjectType.Beam) != 0) CollideBeamAtNode(node, frameId, ref so);
                else if ((so.Type & GameObjectType.Proj) != 0) CollideProjAtNode(node, frameId, ref so);
                else if ((so.Type & GameObjectType.Ship) != 0) CollideShipAtNode(node, frameId, ref so);
            }
        }


        private static void HandleBeamCollision(Beam beam, GameplayObject victim, float hitDistance)
        {
            if (!beam.Touch(victim)) // dish out damage if we can
                return; // we couldn't do a Touch for some reason :(

            if (victim is ShipModule module) // for ships we get the actual ShipModule that was hit, not the ship itself
                module.GetParent().MoveModulesTimer = 2f;
            
            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;
            Vector2 hitPos;
            if (hitDistance > 0f)
                hitPos = beamStart + (beamEnd - beamStart).Normalized()*hitDistance;
            else // the beam probably glanced the module from side, so just get the closest point:
                hitPos = victim.Center.FindClosestPointOnLine(beamStart, beamEnd);

            beam.CollidedThisFrame = true;
            beam.ActualHitDestination = hitPos;
        }

        private static void HandleProjCollision(Projectile projectile, GameplayObject victim)
        {
            if (!projectile.Touch(victim))
                return;

            if (victim is ShipModule module) // for ships we get the actual ShipModule that was hit, not the ship itself
                module.GetParent().MoveModulesTimer = 2f;
        }

        private static void FindNearbyAtNode(Node node, ref SpatialObj nearbyDummy, GameObjectType filter, 
                                             ref int numNearby, ref GameplayObject[] nearby)
        {
            int count = node.Count;
            SpatialObj[] items = node.Items;
            for (int i = 0; i < count; ++i)
            {
                ref SpatialObj so = ref items[i];
                if (filter != GameObjectType.None && (so.Obj.Type & filter) == 0)
                    continue; // no filter match

                // ignore self  and  ensure in radius
                if (so.Obj == nearbyDummy.Obj || !nearbyDummy.HitTestNearby(ref so))
                    continue;

                if (numNearby == nearby.Length) // "clever" resize
                    Array.Resize(ref nearby, numNearby + count);

                nearby[numNearby++] = so.Obj;
            }
            if (node.NW != null)
            {
                FindNearbyAtNode(node.NW, ref nearbyDummy, filter, ref numNearby, ref nearby);
                FindNearbyAtNode(node.NE, ref nearbyDummy, filter, ref numNearby, ref nearby);
                FindNearbyAtNode(node.SE, ref nearbyDummy, filter, ref numNearby, ref nearby);
                FindNearbyAtNode(node.SW, ref nearbyDummy, filter, ref numNearby, ref nearby);
            }
        }

        public GameplayObject[] FindNearby(GameplayObject obj, float radius, GameObjectType filter = GameObjectType.None)
        {
            // assume most results will be either empty, or only from a single quadrant
            // this means using a dynamic Array will be way more wasteful
            int numNearby = 0;
            GameplayObject[] nearby = Empty<GameplayObject>.Array;

            var nearbyDummy = new SpatialObj(obj, radius); // dummy object to simplify our search interface

            // find the deepest enclosing node
            Node node = FindEnclosingNode(ref nearbyDummy);
            if (node != null)
                FindNearbyAtNode(node, ref nearbyDummy, filter, ref numNearby, ref nearby);

            if (numNearby != nearby.Length)
                Array.Resize(ref nearby, numNearby);

            if (ShouldStoreDebugInfo) AddNearbyDebug(obj, radius, nearby);
            else                      DebugFindNearby.Clear();
            return nearby;
        }

        private static bool ShouldStoreDebugInfo => Empire.Universe.Debug && Empire.Universe.showdebugwindow;
        private static void AddNearbyDebug(GameplayObject obj, float radius, GameplayObject[] nearby)
        {
            var debug = new FindNearbyDebug { Obj = obj, Radius = radius, Nearby = nearby, Timer = 2f };
            for (int i = 0; i < DebugFindNearby.Count; ++i)
                if (DebugFindNearby[i].Obj == obj) {
                    DebugFindNearby[i] = debug;
                    return;           
                }
            DebugFindNearby.Add(debug);
        }

        private struct FindNearbyDebug
        {
            public GameplayObject Obj;
            public float Radius;
            public GameplayObject[] Nearby;
            public float Timer;
        }

        private static readonly Array<FindNearbyDebug> DebugFindNearby = new Array<FindNearbyDebug>();
        private static SpatialObj[] DebugDrawBuffer = NoObjects;

        private static readonly Color Brown  = new Color(Color.SaddleBrown, 150);
        private static readonly Color Violet = new Color(Color.MediumVioletRed, 100);
        private static readonly Color Golden = new Color(Color.Gold, 100);
        // "Allies are Blue, Enemies are Red, what should I do, with our Quadtree?" - RedFox
        private static readonly Color Blue   = new Color(Color.CadetBlue, 100);
        private static readonly Color Red    = new Color(Color.OrangeRed, 100);
        private static readonly Color Yellow = new Color(Color.Yellow, 100);

        private static void DebugVisualize(UniverseScreen screen, ref Vector2 topleft, ref Vector2 botright, Node node)
        {
            var center = new Vector2((node.X + node.LastX) / 2, (node.Y + node.LastY) / 2);
            var size   = new Vector2(node.LastX - node.X, node.LastY - node.Y);
            screen.DrawRectangleProjected(center, size, 0f, Brown);

            // @todo This is a hack to reduce concurrency related bugs.
            //       once the main drawing and simulation loops are stable enough, this copying can be removed
            //       In most cases it doesn't matter, because this is only used during DEBUG display...
            int count = node.Count;
            if (DebugDrawBuffer.Length < count) DebugDrawBuffer = new SpatialObj[count];
            Array.Copy(node.Items, DebugDrawBuffer, count);

            for (int i = 0; i < count; ++i)
            {
                ref SpatialObj so = ref DebugDrawBuffer[i];
                var ocenter = new Vector2((so.X + so.LastX) / 2, (so.Y + so.LastY) / 2);
                var osize   = new Vector2(so.LastX - so.X, so.LastY - so.Y);
                screen.DrawRectangleProjected(ocenter, osize, 0f, Violet);
                screen.DrawLineProjected(center, ocenter, Violet);
            }
            if (node.NW != null)
            {
                if (node.NW.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.NW);
                if (node.NE.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.NE);
                if (node.SE.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.SE);
                if (node.SW.Overlaps(ref topleft, ref botright)) DebugVisualize(screen, ref topleft, ref botright, node.SW);
            }
        }

        private static Color GetRelationColor(GameplayObject a, GameplayObject b)
        {
            Empire e1 = EmpireManager.GetEmpireById(a.GetLoyaltyId());
            Empire e2 = EmpireManager.GetEmpireById(b.GetLoyaltyId());
            if (e1 != null && e2 != null)
            {
                if (e1 == e2)
                    return Blue;
                if (e1.IsEmpireAttackable(e2)) // hostile?
                    return Red;
                if (e1.TryGetRelations(e2, out Relationship relations) && relations.Treaty_Alliance)
                    return Blue;
            }
            return Yellow; // neutral relation
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            var screenSize = new Vector2(screen.Viewport.Width, screen.Viewport.Height);
            Vector2 topleft  = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
            Vector2 botright = screen.UnprojectToWorldPosition(screenSize);
            DebugVisualize(screen, ref topleft, ref botright, Root);

            Array.Clear(DebugDrawBuffer, 0, DebugDrawBuffer.Length); // prevent zombie objects

            for (int i = 0; i < DebugFindNearby.Count; ++i)
            {
                FindNearbyDebug debug = DebugFindNearby[i];
                screen.DrawCircleProjected(debug.Obj.Center, debug.Radius, 36, Golden);
                for (int j = 0; j < debug.Nearby.Length; ++j)
                {
                    GameplayObject nearby = debug.Nearby[j];
                    screen.DrawLineProjected(debug.Obj.Center, nearby.Center, GetRelationColor(debug.Obj, nearby));
                }

                debug.Timer -= UniverseScreen.DeltaTime;
                if (debug.Timer > 0f)
                    DebugFindNearby[i] = debug;
                else
                    DebugFindNearby.RemoveAtSwapLast(i--);
            }
        }
    }
}
