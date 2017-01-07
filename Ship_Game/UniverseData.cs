using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class UniverseData
	{
		public string loadFogPath;

		public Array<SolarSystem> SolarSystemsList = new Array<SolarSystem>();

		public Vector2 Size;

		public GameDifficulty difficulty = GameDifficulty.Normal;

		public float FTLSpeedModifier = 1f;
        public float EnemyFTLSpeedModifier = 1f;
        public float FTLInSystemModifier = 1f;
        public bool FTLinNeutralSystem = true;

		public bool GravityWells;

		public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();

		public Ship playerShip;

		public Array<Empire> EmpireList = new Array<Empire>();
        public static float UniverseWidth;

		public UniverseData()
		{
            UniverseWidth = Size.X;
		}

		public enum GameDifficulty
		{
			Easy,
			Normal,
			Hard,
			Brutal
		}
    }
}