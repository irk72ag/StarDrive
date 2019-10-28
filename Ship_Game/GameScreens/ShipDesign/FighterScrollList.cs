﻿
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class FighterScrollList : ScrollList<FighterListItem>
    {
        Submenu FighterListContainer;
        readonly GameScreen Screen;
        public ShipModule ActiveHangarModule;
        public ShipModule ActiveModule;
        public string HangarShipUIDLast = "";

        public FighterScrollList(Submenu fighterList, GameScreen shipDesignScreen) : base(fighterList, 40)
        {
            Screen = shipDesignScreen;
            FighterListContainer = fighterList;
        }

        public bool HitTest(InputState input)
        {
            return ActiveModule != null && Rect.HitTest(input.CursorPosition);
        }

        void Populate()
        {
            Reset();
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicLaunch.ToString()));
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicInterceptor.ToString()));
            AddShip(ResourceManager.GetShipTemplate(DynamicHangarOptions.DynamicAntiShip.ToString()));
            foreach (string shipId in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.GetShipTemplate(shipId, out Ship hangarShip))
                    continue;
                string role = ShipData.GetRole(hangarShip.shipData.HullRole);
                if (!ActiveModule.PermittedHangarRoles.Contains(role))
                    continue;
                if (hangarShip.SurfaceArea > ActiveModule.MaximumHangarShipSize)
                    continue;
                AddShip(ResourceManager.ShipsDict[shipId]);
            }
        }
        
        void AddShip(Ship ship)
        {
            AddItem(new FighterListItem(ship));
        }

        public override void OnItemHovered(FighterListItem item)
        {
            if (item == null) // we're not hovering the scroll list, just highlight the active ship
            {
                foreach (FighterListItem e in AllEntries)
                    if (ActiveModule.hangarShipUID == e.Ship.Name)
                        SelectionBox = new Selector(e.Rect);
            }
            base.OnItemHovered(item);
        }

        public override void OnItemClicked(FighterListItem item)
        {
            ActiveModule.hangarShipUID = item.Ship.Name;
            HangarShipUIDLast = item.Ship.Name;
            base.OnItemClicked(item);
        }

        public bool HandleInput(InputState input, ShipModule activeModule, ShipModule highlightedModule)
        {
            activeModule = activeModule ?? highlightedModule;
            if (activeModule?.ModuleType == ShipModuleType.Hangar &&
                !activeModule.IsTroopBay && !activeModule.IsSupplyBay)
            {
                ActiveModule = activeModule;
                SetActiveHangarModule(activeModule, ActiveHangarModule);
                
                base.HandleInput(input);
                return true;
            }
            
            base.HandleInput(input);
            ActiveHangarModule = null;
            ActiveModule = null;
            HangarShipUIDLast = "";
            return false;
        }

        bool HangarNotSelected(ShipModule activeModule, ShipModule activeHangarModule)
            => activeHangarModule == activeModule || activeModule.ModuleType != ShipModuleType.Hangar;

        public void SetActiveHangarModule(ShipModule activeModule, ShipModule activeHangarModule)
        {
            if (HangarNotSelected(activeModule, activeHangarModule))
                return;

            ActiveHangarModule = activeModule;
            Populate();

            Ship fighter = ResourceManager.GetShipTemplate(HangarShipUIDLast, false);
            if (HangarShipUIDLast != "" && activeModule.PermittedHangarRoles.Contains(fighter?.shipData.GetRole()) && activeModule.MaximumHangarShipSize >= fighter?.SurfaceArea)
            {
                activeModule.hangarShipUID = HangarShipUIDLast;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            if (ActiveHangarModule == null)
                return;

            Screen.DrawRectangle(Rect, Color.TransparentWhite, Color.Black);
            FighterListContainer.Draw(batch);
            base.Draw(batch);
        }
    }
}
