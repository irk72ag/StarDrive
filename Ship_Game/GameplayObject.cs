using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    [Flags]
    public enum GameObjectType
    {
        None       = 0,
        Ship       = 1,
        ShipModule = 2,
        Projectile = 4,
        Beam       = 8,
        Asteroid   = 16,
        Moon       = 32,
    }

    public abstract class GameplayObject
    {
        public static GraphicsDevice device;

        /**
         *  @note Careful! Any property/variable that doesn't have [XmlIgnore][JsonIgnore]
         *        will be accidentally serialized! 
         */

        [XmlIgnore][JsonIgnore] public bool Active = true;
        [XmlIgnore][JsonIgnore] protected AudioHandle DeathSfx;
        [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }

        [Serialize(0)] public Vector2 Position;
        [Serialize(1)] public Vector2 Center;
        [Serialize(2)] public Vector2 Velocity;
        [Serialize(3)] public float Rotation;

        [Serialize(4)] public Vector2 Dimensions;
        [Serialize(5)] public float Radius = 1f;
        [Serialize(6)] public float Mass = 1f;
        [Serialize(7)] public float Health;

        [Serialize(8)] public GameObjectType Type;

        [XmlIgnore][JsonIgnore] public GameplayObject LastDamagedBy;
        [XmlIgnore][JsonIgnore] public bool CollidedThisFrame;

        [XmlIgnore][JsonIgnore] public int SpatialIndex = -1;
        [XmlIgnore][JsonIgnore] public bool InDeepSpace => System == null;


        private static int GameObjIds;
        [XmlIgnore][JsonIgnore] public int Id = ++GameObjIds;

        protected GameplayObject(GameObjectType typeFlags)
        {
            Type = typeFlags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(GameObjectType flags) => (Type & flags) != 0;

        public virtual bool Damage(GameplayObject source, float damageAmount)
        {
            return false;
        }

        public virtual void Initialize()
        {
            if (SpatialIndex == -1) // not assigned to a SpatialManager yet?
                ActiveSpatialManager.Add(this);
        }

        public virtual void Die(GameplayObject source, bool cleanupOnly)
        {
            Active = false;
            if (SpatialIndex != -1)
                ActiveSpatialManager.Remove(this);
        }

        [XmlIgnore][JsonIgnore] 
        public string SystemName => System?.Name ?? "Deep Space";

        [XmlIgnore][JsonIgnore] 
        public SpatialManager ActiveSpatialManager => UniverseScreen.DeepSpaceManager;

        public T[] GetNearby<T>() where T : GameplayObject => ActiveSpatialManager.GetNearby<T>(Position, Radius);        

        public void SetSystem(SolarSystem system)
        {
            if (System == system)
                return;

            if (this is Ship ship)
            {
                System?.ShipList.RemoveSwapLast(ship);
                system?.ShipList.AddUnique(ship);
            }
            System = system;
        }


        public virtual bool Touch(GameplayObject target)
        {
            return false; // by default, objects can't be touched
        }

        public virtual void Update(float elapsedTime)
        {
            CollidedThisFrame = false;
        }

        public override string ToString() => $"GameObj Id={Id} Pos={Position}";
    }
}