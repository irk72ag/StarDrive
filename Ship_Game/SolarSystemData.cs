using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SolarSystemData
	{
		public string Name;

		public string SunPath = "star_yellow";

		public List<SolarSystemData.Ring> RingList = new List<SolarSystemData.Ring>();

		public SolarSystemData()
		{
		}

		public struct BuildingData
		{
			public string XMLName;

			public int Level;
		}

		public struct Resource
		{
			public string Name;
		}

		public struct Ring
		{
			public string Planet;

			public string SpecialDescription;

			public int WhichPlanet;

			public string Asteroids;

			public string HasRings;

			public bool HomePlanet;

			public string Owner;

			public string Station;

			public List<SolarSystemData.BuildingData> BuildingDataList;

			public List<SolarSystemData.Resource> ResourceList;

			public int Population;
		}
	}
}