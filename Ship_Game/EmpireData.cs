using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EmpireData
	{
		public SerializableDictionary<string, WeaponTagModifier> WeaponTags = new SerializableDictionary<string, WeaponTagModifier>();

		public string WarpStart;

		public string WarpEnd;

		public Difficulty difficulty;

		public bool ModRace;

		public string CurrentAutoFreighter = "";

		public string CurrentAutoColony = "";

		public string CurrentAutoScout = "";

		public string DiplomacyDialogPath;

		public DTrait DiplomaticPersonality;

		public ETrait EconomicPersonality;

		public float TaxRate = 0.25f;

		public List<string> ExcludedDTraits = new List<string>();

		public List<string> ExcludedETraits = new List<string>();

		public BatchRemovalCollection<Agent> AgentList = new BatchRemovalCollection<Agent>();

		public string AbsorbedBy;

		public string StartingShip;

		public string StartingScout;

		public string PrototypeShip;

		public string DefaultColonyShip;

		public string DefaultSmallTransport;

        public string DefaultTroopShip;

        public string DefaultShipyard = "Shipyard";

		public bool Defeated;

		public bool HasSecretTech;

		public bool RebellionLaunched;

		public float MilitaryScoreTotal;

		public int ScoreAverage;

		public string MusicCue;

		public List<string> ResearchQueue = new List<string>();

		public BatchRemovalCollection<Mole> MoleList = new BatchRemovalCollection<Mole>();

		public float CounterIntelligenceBudget;

		public string PortraitName;

		public string RebelSing;

		public string RebelPlur;

		public int TroopNameIndex;

		public int TroopDescriptionIndex;

		public string RebelName;

		public bool IsRebelFaction;

		public RacialTrait Traits;

		public byte Faction;

        public bool MinorRace;

		public short TurnsBelowZero;

		public bool Privatization;

		public float FuelCellModifier;

		public float FlatMoneyBonus;

		public float EmpireWideProductionPercentageModifier = 1f;

		public float FTLModifier = 35f;

		public float MassModifier = 1f;

		public float SubLightModifier = 1f;

		public float EmpireFertilityBonus;

		public float SensorModifier = 1f;

		public float OrdnanceEffectivenessBonus;

		public int ArmorPiercingBonus;

        public float SpoolTimeModifier = 1.0f;

		public float ExplosiveRadiusReduction;

		public float ShieldPenBonusChance;

		public float SpyModifier;

		public float DefensiveSpyBonus;

		public float OffensiveSpyBonus;

		public float FTLPowerDrainModifier = 2f;

		public List<Artifact> OwnedArtifacts = new List<Artifact>();

		public int BonusFighterLevels;

		public float MissileDodgeChance;

		public float MissileHPModifier = 1f;

		public bool Inhibitors;

		public float BaseReproductiveRate = 0.01f;

        //Added by McShooterz: power bonus
        public float PowerFlowMod = 0f;
        public float ShieldPowerMod = 0f;
        public float ExperienceMod = 0f;

        //unlock at start
        public List<string> unlockBuilding = new List<string>();
        public List<string> unlockShips = new List<string>();


		public EmpireData()
		{
			this.WeaponTags.Add("Kinetic", new WeaponTagModifier());
			this.WeaponTags.Add("Energy", new WeaponTagModifier());
			this.WeaponTags.Add("Beam", new WeaponTagModifier());
			this.WeaponTags.Add("Hybrid", new WeaponTagModifier());
			this.WeaponTags.Add("Railgun", new WeaponTagModifier());
			this.WeaponTags.Add("Missile", new WeaponTagModifier());
			this.WeaponTags.Add("Explosive", new WeaponTagModifier());
			this.WeaponTags.Add("Guided", new WeaponTagModifier());
			this.WeaponTags.Add("Intercept", new WeaponTagModifier());
			this.WeaponTags.Add("PD", new WeaponTagModifier());
			this.WeaponTags.Add("Spacebomb", new WeaponTagModifier());
			this.WeaponTags.Add("BioWeapon", new WeaponTagModifier());
			this.WeaponTags.Add("Drone", new WeaponTagModifier());
			this.WeaponTags.Add("Torpedo", new WeaponTagModifier());
			this.WeaponTags.Add("Subspace", new WeaponTagModifier());
			this.WeaponTags.Add("Warp", new WeaponTagModifier());
            //added by McShooterz: added missing tags
            this.WeaponTags.Add("Cannon", new WeaponTagModifier());
            this.WeaponTags.Add("Bomb", new WeaponTagModifier());
            //added by The Doctor: New tags
            this.WeaponTags.Add("Array", new WeaponTagModifier());
            this.WeaponTags.Add("Flak", new WeaponTagModifier());
            this.WeaponTags.Add("Tractor", new WeaponTagModifier());

		}

		public EmpireData GetClone()
		{
			return (EmpireData)this.MemberwiseClone();
		}
	}
}