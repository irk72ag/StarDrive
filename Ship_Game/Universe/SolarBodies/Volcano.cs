﻿using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    public class Volcano // Created by Fat Bastard, Mar 2021
    {
        public bool Active { get; private set; }
        public bool Erupting { get; private set; }
        public float ActivationChance { get; private set; }
        public readonly PlanetGridSquare Tile;
        public readonly Planet P;

        public Volcano(PlanetGridSquare tile, Planet planet)
        {
            ActivationChance = InitActivationChance();
            Tile             = tile;
            P                = planet;
            CreateDormantVolcano();
        }

        // From save
        public Volcano(PlanetGridSquare tile, Planet planet, float activationChance, bool active, bool erupting)
        {
            ActivationChance = activationChance;
            Active           = active;
            Erupting         = erupting;
            Tile             = tile;
            P                = planet;
        }

        public Empire Player           => EmpireManager.Player;
        public bool Dormant            => !Active;
        float DeactivationChance       => ActivationChance * 3;
        float ActiveEruptionChance     => ActivationChance * 10;
        float CalmDownChance           => ActiveEruptionChance;
        float InitActivationChance()   => RandomMath.RandomBetween(0f, 1f);
        string ActiveVolcanoTexPath    => "Buildings/icon_Active_Volcano_64x64";
        string DormantVolcanoTexPath   => "Buildings/icon_Dormant_Volcano_64x64";
        string EruptingVolcanoTexPath  => "Buildings/icon_Erupting_Volcano_64x64";
        public bool ShouldNotifyPlayer => P.Owner == Player || P.AnyOfOurTroops(Player);

        void CreateLavaPool(PlanetGridSquare tile)
        {
            int bid    = RandomMath.RollDice(50) ? Building.Lava1Id : Building.Lava2Id;
            Building b = ResourceManager.CreateBuilding(bid);
            tile.PlaceBuilding(b, P);
            P.SetHasDynamicBuildings(true);
        }

        void CreateVolcanoBuilding(int bid)
        {
            Building b = ResourceManager.CreateBuilding(bid);
            Tile.PlaceBuilding(b, P);
            P.SetHasDynamicBuildings(true);
        }

        void CreateDormantVolcano()
        {
            P.DestroyTileWithVolcano(Tile);
            Active     = false;
            Erupting   = false;
            CreateVolcanoBuilding(Building.VolcanoId);
        }

        public void Evaluate()
        {
            if (Dormant)
            {
                TryActivate();
            }
            else if (Active)
            {
                if (TryDeactivate())
                    return;

                TryErupt();
            }
            else if (Erupting)
            {
                TryCalmDown();
            }
        }

        void TryActivate()
        {
            if (RandomMath.RollDice(ActivationChance))
            {
                P.DestroyTileWithVolcano(Tile);
                Active     = true;
                CreateVolcanoBuilding(Building.ActiveVolcanoId);
                if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                    Empire.Universe.NotificationManager.AddVolcanoRelated(P, new LocalizedText(4256).Text, ActiveVolcanoTexPath);
            }
        }

        void TryErupt()
        {
            if (RandomMath.RollDice(ActiveEruptionChance))
            {
                P.DestroyTileWithVolcano(Tile);
                string message = new LocalizedText(4260).Text;
                Erupting       = true;
                Erupt(out string eruptionSeverityText);
                message = $"{message}\n{eruptionSeverityText}";
                CreateVolcanoBuilding(Building.EruptingVolcanoId);
                if (RandomMath.RollDice(ActiveEruptionChance * 2))
                {
                    P.AddMaxBaseFertility(-0.1f);
                    message = $"{message}\n{new LocalizedText(6262).Text}";
                }
                else
                {
                    message = $"{message}\n{new LocalizedText(6261).Text}";
                }

                if (ShouldNotifyPlayer)
                    Empire.Universe.NotificationManager.AddVolcanoRelated(P, message, EruptingVolcanoTexPath);
            }
        }

        void TryCalmDown()
        {
            if (RandomMath.RollDice(CalmDownChance))
            {
                CreateDormantVolcano();
                string message   = new LocalizedText(4258).Text;
                ActivationChance = InitActivationChance();
                if (RandomMath.RollDice(ActiveEruptionChance * 2))
                {
                    float increaseBy   = RandomMath.RollDice(75) ? 0.1f : 0.2f;
                    message            = $"{message}\n{new LocalizedText(4259).Text} {increaseBy.String(1)}.";
                    P.MineralRichness += increaseBy;
                }

                if (ShouldNotifyPlayer)
                    Empire.Universe.NotificationManager.AddVolcanoRelated(P, message, DormantVolcanoTexPath);
            }
        }

        bool TryDeactivate()
        {
            if (RandomMath.RollDice(DeactivationChance))
            {
                Active   = false;
                Erupting = false;
                CreateDormantVolcano();
                if (!GlobalStats.DisableVolcanoWarning && ShouldNotifyPlayer)
                    Empire.Universe.NotificationManager.AddVolcanoRelated(P, new LocalizedText(4257).Text, DormantVolcanoTexPath);

                return true;
            }

            return false;
        }

        void Erupt(out string eruptionSeverityText)
        {
            var potentialTiles     = P.TilesList.Filter(t => !t.VolcanoHere & !t.LavaHere);
            int numLavaPoolsWanted = GetNumLavaPools(potentialTiles.Length.UpperBound(16));
            int actualLavaPools    = 0;
            var potentialLavaTiles = GetPotentialLavaTiles(Tile);

            for (int i = 0; i < numLavaPoolsWanted; i++)
            {
                if (potentialLavaTiles.Count == 0)
                    break;

                PlanetGridSquare tile = potentialLavaTiles.RandItem();
                CreateLavaPool(tile);
                actualLavaPools += 1;
                potentialLavaTiles.AddRange(GetPotentialLavaTiles(tile));
                potentialLavaTiles.Remove(tile);
            }

            eruptionSeverityText = GetEruptionText(actualLavaPools);
        }

        string GetEruptionText(int numLavaPoolsCreated)
        {
            string text;
            if (numLavaPoolsCreated == 0)     text = new LocalizedText(4263).Text;
            else if (numLavaPoolsCreated <=3) text = new LocalizedText(4264).Text;
            else                              text = new LocalizedText(4265).Text;

            return text;
        }

        Array<PlanetGridSquare> GetPotentialLavaTiles(PlanetGridSquare tile)
        {
            Array<PlanetGridSquare> tiles = new Array<PlanetGridSquare>();
            for (int i = 0; i < P.TilesList.Count; i++)
            {
                PlanetGridSquare t = P.TilesList[i];
                if (!t.VolcanoHere && !t.LavaHere && t.InRangeOf(tile, 1))
                    tiles.Add(t);
            }

            return tiles;
        }

        int GetNumLavaPools(int maxSeverity)
        {
            int numLavaPools;
            switch (RandomMath.RollDie(maxSeverity))
            {
                default: numLavaPools = 0; break;
                case 5:  numLavaPools = 1; break;
                case 6:
                case 7:  numLavaPools = 2; break;
                case 8:
                case 9:  numLavaPools = 3; break;
                case 10:
                case 11: numLavaPools = 4; break;
                case 12:
                case 13: numLavaPools = 5; break;
                case 14: numLavaPools = 6; break;
                case 15: numLavaPools = 7; break;
                case 16: numLavaPools = 8; break;
            }

            return numLavaPools;
        }

        public static void UpdateLava(PlanetGridSquare tile, Planet planet)
        {
            if (!RandomMath.RollDice(2))
                return; 

            // Remove the Lava Pool
            planet.DestroyTileWithVolcano(tile);
            if (RandomMath.RollDice(50))
            {
                planet.MakeTileHabitable(tile);
                if (planet.Owner == EmpireManager.Player)
                    Empire.Universe.NotificationManager.AddVolcanoRelated(planet, new LocalizedText(4266).Text);
            }
        }

        public static void RemoveVolcano(PlanetGridSquare tile, Planet planet) // todo After Terraforming
        {
            planet.DestroyTileWithVolcano(tile);
            tile.Volcano = null;
        }

        public string ActivationChanceText(out Color color)
        {
            color = Color.Green;
            if (Erupting)
                return "";

            string text;
            if (Dormant)
            {
                if      (ActivationChance < 0.1f)  text = new LocalizedText(4243).Text;
                else if (ActivationChance < 0.33f) text = new LocalizedText(4244).Text;
                else if (ActivationChance < 0.66f) text = new LocalizedText(4245).Text;
                else                               text = new LocalizedText(4246).Text;

                color = Color.Yellow;
                return $"{text} {new LocalizedText(4239)}";
            }

            if (Active)
            {
                if (ActiveEruptionChance < 1f)        text = new LocalizedText(4245).Text;
                else if (ActiveEruptionChance < 3.3f) text = new LocalizedText(4246).Text;
                else if (ActiveEruptionChance < 6.6f) text = new LocalizedText(4247).Text;
                else                                  text = new LocalizedText(4248).Text;

                color = Color.Red;
                return $"{text} {new LocalizedText(4242)}";
            }

            return "";
        }
    }
}