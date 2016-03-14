﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;

namespace Ship_Game
{
    public sealed class SetupSave
    {
        public string Name = "";
        public string Date = "";
        public string ModName = "";
        public string ModPath = "";
        public string Version;
        public UniverseData.GameDifficulty GameDifficulty;
        public RaceDesignScreen.StarNum StarEnum;
        public RaceDesignScreen.GalSize Galaxysize;
        public int Pacing;
        public RaceDesignScreen.ExtraRemnantPresence ExtraRemnant;
        public float FTLModifier;
        public float EnemyFTLModifier;
        public float OptionIncreaseShipMaintenance;
        public float MinimumWarpRange;
        public float MemoryLimiter;
        public byte TurnTimer;
        public bool preventFederations;
        public float GravityWellRange;
        public RaceDesignScreen.GameMode mode;
        public int numOpponents;
        public int ExtraPlanets;
        public float StartingPlanetRichness;
        public bool PlanetaryGravityWells;
        public bool WarpInSystem;

        public SetupSave()
        { }

        public SetupSave(UniverseData.GameDifficulty gameDifficulty, RaceDesignScreen.StarNum StarEnum, RaceDesignScreen.GalSize Galaxysize, int Pacing, RaceDesignScreen.ExtraRemnantPresence ExtraRemnant, int numOpponents, RaceDesignScreen.GameMode mode)
        {
            if (GlobalStats.ActiveMod != null)
            {
                this.ModName = GlobalStats.ActiveMod.mi.ModName;
                this.ModPath = GlobalStats.ActiveMod.ModPath;
            }
            this.Version = ConfigurationManager.AppSettings["ExtendedVersion"];
            this.GameDifficulty = gameDifficulty;
            this.StarEnum = StarEnum;
            this.Galaxysize = Galaxysize;
            this.Pacing = Pacing;
            this.ExtraRemnant = ExtraRemnant;
            this.FTLModifier = GlobalStats.FTLInSystemModifier;
            this.EnemyFTLModifier = GlobalStats.EnemyFTLInSystemModifier;
            this.OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            this.MinimumWarpRange = GlobalStats.MinimumWarpRange;
            this.MemoryLimiter = GlobalStats.MemoryLimiter;
            this.TurnTimer = GlobalStats.TurnTimer;
            this.preventFederations = GlobalStats.preventFederations;
            this.GravityWellRange = GlobalStats.GravityWellRange;
            this.mode = mode;
            this.numOpponents = numOpponents;
            this.ExtraPlanets = GlobalStats.ExtraPlanets;
            this.StartingPlanetRichness = GlobalStats.StartingPlanetRichness;
            this.PlanetaryGravityWells = GlobalStats.PlanetaryGravityWells;
            this.WarpInSystem = GlobalStats.WarpInSystem;

            string str = DateTime.Now.ToString("M/d/yyyy");
            DateTime now = DateTime.Now;
            this.Date = string.Concat(str, " ", now.ToString("t", CultureInfo.CreateSpecificCulture("en-US").DateTimeFormat));
        }
    }
}
