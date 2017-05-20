using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game {
    public sealed partial class ShipDesignScreen
    {
        public void ChangeHull(ShipData hull) //Mer
        {
#if SHIPYARD
            TotalI = TotalO = TotalE = TotalIO = TotalIE = TotalOE = TotalIOE = 0;
        #endif
            Reset = true;
            DesignStack.Clear();
            LastDesignActionPos = Vector2.Zero;
            LastActiveUID = "";

            lock (GlobalStats.ObjectManagerLocker)
            {
                if (shipSO != null)
                {
                    ScreenManager.Remove(shipSO);
                }
            }
            ActiveHull = new ShipData
            {
                Animated = hull.Animated,
                CombatState = hull.CombatState,
                Hull = hull.Hull,
                IconPath = hull.IconPath,
                ModelPath = hull.ModelPath,
                Name = hull.Name,
                Role = hull.Role,
                ShipStyle = hull.ShipStyle,
                ThrusterList = hull.ThrusterList,
                ShipCategory = hull.ShipCategory,
                CarrierShip = hull.CarrierShip
            };
            Techs.Clear();
            AddToTechList(ActiveHull.HullData.techsNeeded);
            CarrierOnly = hull.CarrierShip;
            LoadCategory = hull.ShipCategory;
            Fml = true;
            Fmlevenmore = true;

            ActiveHull.ModuleSlots = new ModuleSlotData[hull.ModuleSlots.Length];
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData hullSlot = hull.ModuleSlots[i];
                ModuleSlotData data = new ModuleSlotData
                {
                    Position = hullSlot.Position,
                    Restrictions = hullSlot.Restrictions,
                    Facing = hullSlot.Facing,
                    InstalledModuleUID = hullSlot.InstalledModuleUID
                };
                ActiveHull.ModuleSlots[i] = data;
#if SHIPYARD
                if (data.Restrictions == Restrictions.I) TotalI++;
                if (data.Restrictions == Restrictions.O) TotalO++;
                if (data.Restrictions == Restrictions.E) TotalE++;
                if (data.Restrictions == Restrictions.IO) TotalIO++;
                if (data.Restrictions == Restrictions.IE) TotalIE++;
                if (data.Restrictions == Restrictions.OE) TotalOE++;
                if (data.Restrictions == Restrictions.IOE) TotalIOE++;
            #endif
            }
            CombatState = hull.CombatState;
            if (!hull.Animated)
            {
                ActiveModel = ResourceManager.GetModel(ActiveHull.ModelPath);
                ModelMesh mesh = ActiveModel.Meshes[0];
                shipSO = new SceneObject(mesh)
                {
                    ObjectType = ObjectType.Dynamic,
                    World = WorldMatrix
                };
                lock (GlobalStats.ObjectManagerLocker)
                {
                    ScreenManager.Submit(this.shipSO);
                }
            }
            else
            {
                SkinnedModel sm = ResourceManager.GetSkinnedModel(ActiveHull.ModelPath);
                shipSO = new SceneObject(sm.Model)
                {
                    ObjectType = ObjectType.Dynamic,
                    World = WorldMatrix
                };
                lock (GlobalStats.ObjectManagerLocker)
                {
                    ScreenManager.Submit(this.shipSO);
                }
            }
            foreach (ToggleButton button in CombatStatusButtons)
            {
                switch (button.Action)
                {
                    default: continue;
                    case "attack":
                        button.Active = CombatState == CombatState.AttackRuns;
                        break;
                    case "arty":
                        button.Active = CombatState == CombatState.Artillery;
                        break;
                    case "hold":
                        button.Active = CombatState == CombatState.HoldPosition;
                        break;
                    case "orbit_left":
                        button.Active = CombatState == CombatState.OrbitLeft;
                        break;
                    case "broadside_left":
                        button.Active = CombatState == CombatState.BroadsideLeft;
                        break;
                    case "broadside_right":
                        button.Active = CombatState == CombatState.BroadsideRight;
                        break;
                    case "short":
                        button.Active = CombatState == CombatState.ShortRange;
                        break;
                    case "evade":
                        button.Active = CombatState == CombatState.Evade;
                        break;
                    case "orbit_right":
                        button.Active = CombatState == CombatState.OrbitRight;
                        break;
                }
            }
            SetupSlots();
        }

        private bool CheckDesign()
        {
            bool emptySlots = true;
            bool hasBridge = false;
            foreach (SlotStruct slot in Slots)
            {
                if (slot.ModuleUID == null)
                    emptySlots = false;
                if (slot.ModuleUID == null || !slot.Module.IsCommandModule)
                    continue;
                hasBridge = true;
            }
            if (!hasBridge && ActiveHull.Role != ShipData.RoleName.platform &&
                ActiveHull.Role != ShipData.RoleName.station || !emptySlots)
                return false;
            return true;
        }

        public void CreateShipModuleSelectionWindow()
        {
            UpArrow = new Rectangle(ModuleSelectionArea.X + ModuleSelectionArea.Width - 22,
                ModuleSelectionArea.Y, 22, 30);
            DownArrow = new Rectangle(ModuleSelectionArea.X + ModuleSelectionArea.Width - 22,
                ModuleSelectionArea.Y + ModuleSelectionArea.Height - 32, 20, 30);
            var categories = new Array<string>();
            Dictionary<string, Array<ShipModule>> moduleDict = new Map<string, Array<ShipModule>>();
            foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
            {
                if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                {
                    continue;
                }
                string cat = module.Value.ModuleType.ToString();
                if (!categories.Contains(cat))
                {
                    categories.Add(cat);
                }
                if (moduleDict.ContainsKey(cat))
                {
                    moduleDict[cat].Add(module.Value);
                }
                else
                {
                    moduleDict.Add(cat, new Array<ShipModule>());
                    moduleDict[cat].Add(module.Value);
                }
                ModuleButton mb = new ModuleButton
                {
                    moduleRect = new Rectangle(0, 0, 128, 128),
                    ModuleUID = module.Key
                };
                ModuleButtons.Add(mb);
            }
            categories.Sort();
            int i = 0;
            foreach (string cat in categories)
            {
                var moduleCatButton = new ModuleCatButton
                {
                    mRect = new Rectangle(ModuleSelectionArea.X + 10, ModuleSelectionArea.Y + 10 + i * 25, 45,
                        25),
                    Category = cat
                };
                ModuleCatButtons.Add(moduleCatButton);
                i++;
            }
            int x = 0;
            int y = 0;
            foreach (ModuleButton mb in ModuleButtons)
            {
                mb.moduleRect.X = ModuleSelectionArea.X + 20 + x * 128;
                mb.moduleRect.Y = ModuleSelectionArea.Y + 10 + y * 128;
                x++;
                if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
                {
                    if (x <= 1) continue;
                    y++;
                    x = 0;
                }
                else
                {
                    if (x <= 2) continue;
                    y++;
                    x = 0;
                }
            }
        }

        private void CreateSOFromHull()
        {
            lock (GlobalStats.ObjectManagerLocker)
            {
                if (shipSO != null)
                {
                    ScreenManager.Remove(this.shipSO);
                }
                ModelMesh mesh = ActiveModel.Meshes[0];
                shipSO = new SceneObject(mesh)
                {
                    ObjectType = ObjectType.Dynamic,
                    World = WorldMatrix
                };
                ScreenManager.Submit(this.shipSO);
                this.SetupSlots();
            }
        }

        private void DoExit(object sender, EventArgs e)
        {
            ReallyExit();
        }


        public override void ExitScreen()
        {
            if (!ShipSaved && !CheckDesign())
            {
                MessageBoxScreen message = new MessageBoxScreen(this, Localizer.Token(2121), "Save", "Exit");
                message.Cancelled += DoExit;
                message.Accepted += SaveWIP;
                ScreenManager.AddScreen(message);
                return;
            }
            if (ShipSaved || !CheckDesign())
            {
                ReallyExit();
                return;
            }
            MessageBoxScreen message0 = new MessageBoxScreen(this, Localizer.Token(2137), "Save", "Exit");
            message0.Cancelled += DoExit;
            message0.Accepted += SaveChanges;
            ScreenManager.AddScreen(message0);
        }

        public void ExitToMenu(string launches)
        {
            ScreenToLaunch = launches;
            MessageBoxScreen message;
            if (ShipSaved && CheckDesign())
            {
                LaunchScreen(null, null);
                ReallyExit();
                return;
            }
            if (!ShipSaved && CheckDesign())
            {
                message = new MessageBoxScreen(this, Localizer.Token(2137), "Save", "Exit");
                message.Cancelled += LaunchScreen;
                message.Accepted += SaveChanges;
                ScreenManager.AddScreen(message);
                return;
            }
            message = new MessageBoxScreen(this, Localizer.Token(2121), "Save", "Exit");
            message.Cancelled += LaunchScreen;
            message.Accepted += SaveWIPThenLaunchScreen;
            ScreenManager.AddScreen(message);
        }

   



        public override void HandleInput(InputState input)
        {
            CategoryList.HandleInput(input);
            if (DropdownRect.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
            {
                switch (CategoryList.Options[CategoryList.ActiveIndex].value)
                {
                    case 1:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 75%", ScreenManager);
                        break;
                    }
                    case 2:
                    {
                        ToolTip.CreateTooltip(
                            "Can be used as Freighter.\nEvade when enemy.\nRepair when damaged at 15%",
                            ScreenManager);
                        break;
                    }
                    case 3:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 35%", ScreenManager);
                        break;
                    }
                    case 4:
                    case 5:
                    case 6:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 55%", ScreenManager);
                        break;
                    }
                    case 7:
                    {
                        ToolTip.CreateTooltip("Never Repair!", ScreenManager);
                        break;
                    }
                    default:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 75%", ScreenManager);
                        break;
                    }
                }
            }
            CarrierOnlyBox.HandleInput(input);

            if (ActiveModule != null && (ActiveModule.InstalledWeapon != null
                                              && ActiveModule.ModuleType != ShipModuleType.Turret ||
                                              ActiveModule.XSIZE != ActiveModule.YSIZE))
            {
                if (input.Left)
                    ChangeModuleState(ActiveModuleState.Left);
                if (input.Right)
                    ChangeModuleState(ActiveModuleState.Right);
                if (input.Down)
                    ChangeModuleState(ActiveModuleState.Rear);
                if (input.Up)
                    ChangeModuleState(ActiveModuleState.Normal);
            }
            if (input.ShipDesignExit && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
            }
            if (Close.HandleInput(input))
                ExitScreen();
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && input.LastKeyboardState.IsKeyUp(Keys.Z)
                     && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (DesignStack.Count <= 0)
                    return;
                LastActiveUID = "";
                ShipModule shipModule = ActiveModule;
                DesignAction designAction = DesignStack.Pop();
                SlotStruct slot1 = new SlotStruct();
                foreach (SlotStruct slot2 in Slots)
                {
                    if (slot2.PQ == designAction.clickedSS.PQ)
                    {
                        ClearSlotNoStack(slot2);
                        slot1 = slot2;
                        slot1.Facing = designAction.clickedSS.Facing;
                    }
                    foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                    {
                        if (slot2.PQ == slotStruct.PQ)
                        {
                            ClearSlotNoStack(slot2);
                            break;
                        }
                    }
                }
                if (designAction.clickedSS.ModuleUID != null)
                {
                    ActiveModule = ShipModule.CreateNoParent(designAction.clickedSS.ModuleUID);
                    ResetModuleState();
                    InstallModuleNoStack(slot1);
                }
                foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                {
                    foreach (SlotStruct slot2 in Slots)
                    {
                        if (slot2.PQ == slotStruct.PQ && slotStruct.ModuleUID != null)
                        {
                            ActiveModule = ShipModule.CreateNoParent(slotStruct.ModuleUID);
                            ResetModuleState();
                            InstallModuleNoStack(slot2);
                            slot2.Facing = slotStruct.Facing;
                            slot2.ModuleUID = slotStruct.ModuleUID;
                        }
                    }
                }
                ActiveModule = shipModule;
                ResetModuleState();
            }
            else
            {
                if (!ModuleSelectionMenu.Menu.HitTest(input.CursorPosition)
                    && !HullSelectionRect.HitTest(input.CursorPosition)
                    && !ChooseFighterSub.Menu.HitTest(input.CursorPosition))
                {
                    if (input.ScrollOut)
                    {
                        TransitionZoom -= 0.1f;
                        if (TransitionZoom < 0.300000011920929)
                            TransitionZoom = 0.3f;
                        if (TransitionZoom > 2.65000009536743)
                            TransitionZoom = 2.65f;
                    }
                    if (input.ScrollIn)
                    {
                        TransitionZoom += 0.1f;
                        if (TransitionZoom < 0.300000011920929)
                            TransitionZoom = 0.3f;
                        if (TransitionZoom > 2.65000009536743)
                            TransitionZoom = 2.65f;
                    }
                }

                if (Debug)
                {
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter) && input.LastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        foreach (ModuleSlotData moduleSlotData in ActiveHull.ModuleSlots)
                            moduleSlotData.InstalledModuleUID = null;
                        new XmlSerializer(typeof(ShipData)).Serialize(
                            new StreamWriter("Content/Hulls/" + ActiveHull.ShipStyle + "/" + ActiveHull.Name + ".xml"),
                            ActiveHull);
                    }
                    if (input.Right)
                        ++Operation;
                    if (Operation > SlotModOperation.Normal)
                        Operation = SlotModOperation.Delete;
                }

                HoveredModule = null;
                MouseStateCurrent = Mouse.GetState();
                Vector2 vector2 = new Vector2(MouseStateCurrent.X, MouseStateCurrent.Y);
                selector = null;
                EmpireUI.HandleInput(input, this);
                ActiveModSubMenu.HandleInputNoReset(this);
                HullSL.HandleInput(input);
                for (int index = HullSL.indexAtTop;
                    index < HullSL.Copied.Count && index < HullSL.indexAtTop + HullSL.entriesToDisplay;
                    ++index)
                {
                    ScrollList.Entry e = HullSL.Copied[index];
                    if (e.item is ModuleHeader)
                    {
                        if ((e.item as ModuleHeader).HandleInput(input, e))
                            return;
                    }
                    else if (e.clickRect.HitTest(vector2))
                    {
                        selector = new Selector(ScreenManager, e.clickRect);
                        e.clickRectHover = 1;
                        selector = new Selector(ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            if (!ShipSaved && !CheckDesign())
                            {
                                var messageBoxScreen        = new MessageBoxScreen(this, 2121, "Save", "No");
                                messageBoxScreen.Accepted  += SaveWIPThenChangeHull;
                                messageBoxScreen.Cancelled += JustChangeHull;
                                Changeto                    = e.item as ShipData;
                                ScreenManager.AddScreen(messageBoxScreen);
                                return;
                            }
                            ChangeHull(e.item as ShipData);
                            return;
                        }
                    }
                    else
                        e.clickRectHover = 0;
                }
                ModSel.HandleInput(this);
                if (ActiveModule != null)
                {
                    if (ActiveModule.ModuleType == ShipModuleType.Hangar && !ActiveModule.IsTroopBay
                        && !ActiveModule.IsSupplyBay)
                    {
                        UpdateHangarOptions(ActiveModule);
                        ChooseFighterSL.HandleInput(input);
                        for (int index = ChooseFighterSL.indexAtTop;
                            index < ChooseFighterSL.Copied.Count
                            && index < ChooseFighterSL.indexAtTop + ChooseFighterSL.entriesToDisplay;
                            ++index)
                        {
                            ScrollList.Entry entry = ChooseFighterSL.Copied[index];
                            if (entry.clickRect.HitTest(vector2))
                            {
                                selector = new Selector(ScreenManager, entry.clickRect);
                                entry.clickRectHover = 1;
                                selector = new Selector(ScreenManager, entry.clickRect);
                                if (input.InGameSelect)
                                {
                                    ActiveModule.hangarShipUID = (entry.item as Ship).Name;
                                    HangarShipUIDLast = (entry.item as Ship).Name;
                                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                    return;
                                }
                            }
                        }
                    }
                }
                else if (HighlightedModule != null && HighlightedModule.ModuleType == ShipModuleType.Hangar
                         && (!HighlightedModule.IsTroopBay && !HighlightedModule.IsSupplyBay))
                {
                    ChooseFighterSL.HandleInput(input);
                    for (int index = ChooseFighterSL.indexAtTop;
                        index < ChooseFighterSL.Copied.Count
                        && index < ChooseFighterSL.indexAtTop + ChooseFighterSL.entriesToDisplay;
                        ++index)
                    {
                        ScrollList.Entry entry = ChooseFighterSL.Copied[index];
                        if (!entry.clickRect.HitTest(vector2)) continue;
                        selector = new Selector(ScreenManager, entry.clickRect);
                        entry.clickRectHover = 1;
                        selector = new Selector(ScreenManager, entry.clickRect);
                        if (!input.InGameSelect) continue;
                        HighlightedModule.hangarShipUID = (entry.item as Ship).Name;
                        HangarShipUIDLast = (entry.item as Ship).Name;
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        return;
                    }
                }
                for (int index = WeaponSl.indexAtTop;
                    index < WeaponSl.Copied.Count
                    && index < WeaponSl.indexAtTop + WeaponSl.entriesToDisplay;
                    ++index)
                {
                    ScrollList.Entry e = WeaponSl.Copied[index];
                    if (e.item is ModuleHeader)
                    {
                        if ((e.item as ModuleHeader).HandleInput(input, e))
                            return;
                    }
                    else if (e.clickRect.HitTest(vector2))
                    {
                        selector = new Selector(ScreenManager, e.clickRect);
                        e.clickRectHover = 1;
                        selector = new Selector(ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            SetActiveModule(ShipModule.CreateNoParent((e.item as ShipModule).UID));
                            ResetModuleState();
                            return;
                        }
                    }
                    else
                        e.clickRectHover = 0;
                }
                WeaponSl.HandleInput(input);
                if (HullSelectionRect.HitTest(input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed
                    || ModSel.Menu.HitTest(input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed
                    || ActiveModSubMenu.Menu.HitTest(input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                    return;
                if (ModSel.Menu.HitTest(vector2))
                {
                    if (MouseStateCurrent.ScrollWheelValue > MouseStatePrevious.ScrollWheelValue
                        && WeaponSl.indexAtTop > 0)
                        --WeaponSl.indexAtTop;
                    if (MouseStateCurrent.ScrollWheelValue < MouseStatePrevious.ScrollWheelValue
                        && WeaponSl.indexAtTop + WeaponSl.entriesToDisplay < WeaponSl.Entries.Count)
                        ++WeaponSl.indexAtTop;
                }
                if (ArcsButton.R.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(134, ScreenManager);
                if (ArcsButton.HandleInput(input))
                {
                    ArcsButton.ToggleOn = !ArcsButton.ToggleOn;
                    ShowAllArcs = ArcsButton.ToggleOn;
                }
                if (input.Tab)
                {
                    ShowAllArcs = !ShowAllArcs;
                    ArcsButton.ToggleOn = ShowAllArcs;
                }
                if (input.RightMouseClick)
                {
                    StartDragPos = input.CursorPosition;
                    CameraVelocity.X = 0.0f;
                }
                if (input.RightMouseHeld())
                {
                    float num1 = input.CursorPosition.X - StartDragPos.X;
                    float num2 = input.CursorPosition.Y - StartDragPos.Y;
                    Camera._pos += new Vector2(-num1, -num2);
                    StartDragPos = input.CursorPosition;
                    cameraPosition.X += -num1;
                    cameraPosition.Y += -num2;
                }
                else
                {
                    CameraVelocity.X = 0.0f;
                    CameraVelocity.Y = 0.0f;
                }
                CameraVelocity.X = MathHelper.Clamp(CameraVelocity.X, -10f, 10f);
                CameraVelocity.Y = MathHelper.Clamp(CameraVelocity.Y, -10f, 10f);
                if (input.Escaped)
                    ExitScreen();
                if (ToggleOverlay)
                {
                    foreach (SlotStruct slotStruct in Slots)
                    {
                        Vector2 spaceFromWorldSpace =
                            Camera.GetScreenSpaceFromWorldSpace(new Vector2(slotStruct.PQ.enclosingRect.X,
                                slotStruct.PQ.enclosingRect.Y));
                        if (new Rectangle((int) spaceFromWorldSpace.X, (int) spaceFromWorldSpace.Y,
                                (int) (16.0 * Camera.Zoom), (int) (16.0 * Camera.Zoom))
                            .HitTest(vector2))
                        {
                            if (slotStruct.Module != null)
                                HoveredModule = slotStruct.Module;
                            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed &&
                                input.LastMouseState.LeftButton == ButtonState.Released)
                            {
                                GameAudio.PlaySfxAsync("simple_beep");
                                if (Debug)
                                {
                                    DebugAlterSlot(slotStruct.SlotReference.Position, Operation);
                                    return;
                                }
                                if (slotStruct.Module != null)
                                    HighlightedModule = slotStruct.Module;
                            }
                        }
                    }
                }
                if (UpArrow.HitTest(vector2) && MouseStateCurrent.LeftButton == ButtonState.Released &&
                    (MouseStatePrevious.LeftButton == ButtonState.Pressed && ScrollPosition > 0))
                {
                    --ScrollPosition;
                    GameAudio.PlaySfxAsync("blip_click");
                    foreach (ModuleButton moduleButton in ModuleButtons)
                        moduleButton.moduleRect.Y += 128;
                }
                if (DownArrow.HitTest(vector2) && input.LeftMouseClick)
                {
                    ++ScrollPosition;
                    GameAudio.PlaySfxAsync("blip_click");
                    foreach (ModuleButton moduleButton in ModuleButtons)
                        moduleButton.moduleRect.Y -= 128;
                }
                if (ModuleSelectionArea.HitTest(vector2))
                {
                    if (input.ScrollIn && ScrollPosition > 0)
                    {
                        --ScrollPosition;
                        GameAudio.PlaySfxAsync("blip_click");
                        foreach (ModuleButton moduleButton in ModuleButtons)
                            moduleButton.moduleRect.Y += 128;
                    }
                    if (input.ScrollOut)
                    {
                        ++ScrollPosition;
                        GameAudio.PlaySfxAsync("blip_click");
                        foreach (ModuleButton moduleButton in ModuleButtons)
                            moduleButton.moduleRect.Y -= 128;
                    }
                }
                if (input.RightMouseClick)
                {
                    //this should actually clear slots
                    ActiveModule = null;
                    foreach (SlotStruct slot in Slots)
                    {
                        slot.SetValidity();
                        Vector2 spaceFromWorldSpace = Camera.GetScreenSpaceFromWorldSpace(
                            new Vector2(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y));
                        var rect = new Rectangle((int) spaceFromWorldSpace.X, (int) spaceFromWorldSpace.Y
                            , (int) (16.0 * Camera.Zoom), (int) (16.0 * Camera.Zoom));
                        if (slot.Module == null || !rect.HitTest(vector2)) continue;
                        slot.SetValidity(slot.Module);
                        var designAction = new DesignAction
                        {
                            clickedSS = new SlotStruct
                            {
                                PQ = slot.PQ,
                                Restrictions = slot.Restrictions,
                                Facing = slot.Module != null ? slot.Module.Facing : 0.0f,
                                ModuleUID = slot.ModuleUID,
                                Module = slot.Module,
                                SlotReference = slot.SlotReference
                            }
                        };
                        DesignStack.Push(designAction);
                        GameAudio.PlaySfxAsync("sub_bass_whoosh");
                        ClearParentSlot(slot);
                        RecalculatePower();
                    }
                }
                foreach (ModuleButton moduleButton in ModuleButtons)
                {
                    if (ModuleSelectionArea.HitTest(new Vector2(moduleButton.moduleRect.X + 30,
                        moduleButton.moduleRect.Y + 30)))
                    {
                        if (moduleButton.moduleRect.HitTest(vector2))
                        {
                            if (input.InGameSelect)
                                SetActiveModule(ShipModule.CreateNoParent(moduleButton.ModuleUID));
                            moduleButton.isHighlighted = true;
                        }
                        else
                            moduleButton.isHighlighted = false;
                    }
                }
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && ActiveModule != null)
                {
                    foreach (SlotStruct slot in Slots)
                    {
                        Vector2 spaceFromWorldSpace = Camera.GetScreenSpaceFromWorldSpace(new Vector2(
                            slot.PQ.enclosingRect.X,
                            slot.PQ.enclosingRect.Y));
                        if (!new Rectangle((int) spaceFromWorldSpace.X, (int) spaceFromWorldSpace.Y
                                , (int) (16f * Camera.Zoom), (int) (16f * Camera.Zoom))
                            .HitTest(vector2)) continue;
                        GameAudio.PlaySfxAsync("sub_bass_mouseover");

                        if (slot.PQ.X != (int)LastDesignActionPos.X || slot.PQ.Y != (int)LastDesignActionPos.Y
                            || ActiveModule.UID != LastActiveUID)
                        {
                            InstallModule(
                                slot); //This will make the Ctrl+Z functionality in the shipyard a lot more responsive -Gretman
                            LastDesignActionPos.X = slot.PQ.X;
                            LastDesignActionPos.Y = slot.PQ.Y;
                            LastActiveUID = ActiveModule.UID;
                        }
                    }
                }
                foreach (SlotStruct slotStruct in Slots)
                {
                    if (slotStruct.ModuleUID != null && HighlightedModule != null &&
                        (slotStruct.Module == HighlightedModule &&
                         slotStruct.Module.FieldOfFire > 0f) &&
                        slotStruct.Module.ModuleType == ShipModuleType.Turret)
                    {                        
                        Vector2 spaceFromWorldSpace =
                            Camera.GetScreenSpaceFromWorldSpace(new Vector2(
                                slotStruct.PQ.enclosingRect.X + 16 * slotStruct.Module.XSIZE / 2,
                                slotStruct.PQ.enclosingRect.Y + 16 * slotStruct.Module.YSIZE / 2));
                        //I am not sure what the below was trying to do. It wasnt doing anything...
                        //float fieldOfFire = slotStruct.Module.FieldOfFire / 2f;
                        //float angleToTarget = spaceFromWorldSpace.AngleToTarget(vector2);
                        //float facing = HighlightedModule.Facing;
                        //float angle = Math.Abs(angleToTarget - facing);
                        //if (angle > fieldOfFire)
                        //{
                        //    if (angleToTarget > 180f)
                        //        angleToTarget = -1f * (360f - angleToTarget);
                        //    if (facing > 180f)
                        //        facing = -1f * (360f - facing);
                        //    angle = Math.Abs(angleToTarget - facing);
                        //}

                        if (input.ShipYardArcMove())                            
                                HighlightedModule.Facing = spaceFromWorldSpace.AngleToTarget(vector2);                                                                            
                    }
                }
                foreach (UIButton uiButton in Buttons)
                {
                    if (uiButton.Rect.HitTest(vector2))
                    {
                        uiButton.State = UIButton.PressState.Hover;
                        if (MouseStateCurrent.LeftButton == ButtonState.Pressed &&
                            MouseStatePrevious.LeftButton == ButtonState.Pressed)
                            uiButton.State = UIButton.PressState.Pressed;
                        if (MouseStateCurrent.LeftButton == ButtonState.Released &&
                            MouseStatePrevious.LeftButton == ButtonState.Pressed)
                        {
                            switch (uiButton.Launches)
                            {
                                case "Toggle Overlay":
                                    GameAudio.PlaySfxAsync("blip_click");
                                    ToggleOverlay = !ToggleOverlay;
                                    continue;
                                case "Save As...":
                                    if (CheckDesign())
                                    {
                                        ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
                                        continue;
                                    }
                                    else
                                    {
                                        GameAudio.PlaySfxAsync("UI_Misc20");
                                        ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(2049)));
                                        continue;
                                    }
                                case "Load":
                                    ScreenManager.AddScreen(new LoadDesigns(this));
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                    else
                        uiButton.State = UIButton.PressState.Default;
                }
                if (ActiveHull != null)
                {
                    foreach (ToggleButton toggleButton in CombatStatusButtons)
                    {
                        if (toggleButton.r.HitTest(input.CursorPosition))
                        {
                            if (toggleButton.HasToolTip)
                                ToolTip.CreateTooltip(toggleButton.WhichToolTip, ScreenManager);
                            if (input.InGameSelect)
                            {
                                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                switch (toggleButton.Action)
                                {
                                    case "attack":
                                        CombatState = CombatState.AttackRuns;
                                        break;
                                    case "arty":
                                        CombatState = CombatState.Artillery;
                                        break;
                                    case "hold":
                                        CombatState = CombatState.HoldPosition;
                                        break;
                                    case "orbit_left":
                                        CombatState = CombatState.OrbitLeft;
                                        break;
                                    case "broadside_left":
                                        CombatState = CombatState.BroadsideLeft;
                                        break;
                                    case "orbit_right":
                                        CombatState = CombatState.OrbitRight;
                                        break;
                                    case "broadside_right":
                                        CombatState = CombatState.BroadsideRight;
                                        break;
                                    case "evade":
                                        CombatState = CombatState.Evade;
                                        break;
                                    case "short":
                                        CombatState = CombatState.ShortRange;
                                        break;
                                }
                            }
                        }
                        else
                            toggleButton.Hover = false;
                        switch (toggleButton.Action)
                        {
                            case "attack":
                                toggleButton.Active = CombatState == CombatState.AttackRuns;
                                continue;
                            case "arty":
                                toggleButton.Active = CombatState == CombatState.Artillery;
                                continue;
                            case "hold":
                                toggleButton.Active = CombatState == CombatState.HoldPosition;
                                continue;
                            case "orbit_left":
                                toggleButton.Active = CombatState == CombatState.OrbitLeft;
                                continue;
                            case "broadside_left":
                                toggleButton.Active = CombatState == CombatState.BroadsideLeft;
                                continue;
                            case "orbit_right":
                                toggleButton.Active = CombatState == CombatState.OrbitRight;
                                continue;
                            case "broadside_right":
                                toggleButton.Active = CombatState == CombatState.BroadsideRight;
                                continue;
                            case "evade":
                                toggleButton.Active = CombatState == CombatState.Evade;
                                continue;
                            case "short":
                                toggleButton.Active = CombatState == CombatState.ShortRange;
                                continue;
                            default:
                                continue;
                        }
                    }
                }
                MouseStatePrevious = MouseStateCurrent;
                base.HandleInput(input);
            }
        }

        private void JustChangeHull(object sender, EventArgs e)
        {
            ShipSaved = true;
            ChangeHull(Changeto);
        }

        private void LaunchScreen(object sender, EventArgs e)
        {
            string str = ScreenToLaunch;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Research")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = ScreenToLaunch;
            string str3 = str2;
            if (str2 != null)
            {
                if (str3 == "Main Menu")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                }
                else if (str3 == "Shipyard")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Empire")
                {
                    ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, EmpireUI));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Diplomacy")
                {
                    ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "?")
                {
                    GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                    var wiki = new InGameWiki(this, new Rectangle(0, 0, 750, 600))
                    {
                        TitleText = "StarDrive Help",
                        MiddleText =
                            "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                    ScreenManager.AddScreen(wiki);
                }
            }
            ReallyExit();
        }

        public override void LoadContent()
        {
            LightRig rig = TransientContent.Load<LightRig>("example/ShipyardLightrig");
            rig.AssignTo(this);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280 || ScreenManager
                    .GraphicsDevice.PresentationParameters.BackBufferHeight <= 768)
            {
                LowRes = true;
            }
            Rectangle leftRect = new Rectangle(5, 45, 405,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45 -
                (int) (0.4f * ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight) + 10);
            ModuleSelectionMenu = new Menu1(ScreenManager, leftRect);
            Rectangle modSelR = new Rectangle(0, (LowRes ? 45 : 100), 305, (LowRes ? 350 : 400));
            ModSel = new Submenu(ScreenManager, modSelR, true);
            ModSel.AddTab("Wpn");
            ModSel.AddTab("Pwr");
            ModSel.AddTab("Def");
            ModSel.AddTab("Spc");
            WeaponSl = new ScrollList(ModSel);
            Rectangle active = new Rectangle(modSelR.X, modSelR.Y + modSelR.Height + 15, modSelR.Width, 300);
            activeModWindow = new Menu1(ScreenManager, active);
            Rectangle acsub = new Rectangle(active.X, modSelR.Y + modSelR.Height + 15, 305, 320);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 760)
            {
                acsub.Height = acsub.Height + 120;
            }
            ActiveModSubMenu = new Submenu(ScreenManager, acsub);
            ActiveModSubMenu.AddTab("Active Module");
            Choosefighterrect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y - 90, 240, 270);
            if (Choosefighterrect.Y + Choosefighterrect.Height >
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                int diff = Choosefighterrect.Y + Choosefighterrect.Height - ScreenManager.GraphicsDevice
                               .PresentationParameters.BackBufferHeight;
                Choosefighterrect.Height = Choosefighterrect.Height - (diff + 10);
            }
            Choosefighterrect.Height = acsub.Height;
            ChooseFighterSub = new Submenu(ScreenManager, Choosefighterrect);
            ChooseFighterSub.AddTab("Choose Fighter");
            ChooseFighterSL = new ScrollList(ChooseFighterSub, 40);
            foreach (KeyValuePair<string, bool> hull in EmpireManager.Player.GetHDict())
            {
                if (!hull.Value)
                {
                    continue;
                }
                AvailableHulls.Add(ResourceManager.HullsDict[hull.Key]);
            }
            PrimitiveQuad.graphicsDevice = ScreenManager.GraphicsDevice;
            float width = Viewport.Width;
            Viewport viewport = Viewport;
            float aspectRatio = width / viewport.Height;
            offset = new Vector2();
            Viewport viewport1 = Viewport;
            offset.X = viewport1.Width / 2 - 256;
            Viewport viewport2 = Viewport;
            offset.Y = viewport2.Height / 2 - 256;
            Camera = new Camera2d();
            Camera2d vector2 = Camera;
            Viewport viewport3 = Viewport;
            float single = viewport3.Width / 2f;
            Viewport viewport4 = Viewport;
            vector2.Pos = new Vector2(single, viewport4.Height / 2f);
            Vector3 camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
            View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                         Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                            new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
            ChangeHull(AvailableHulls[0]);
            lock (GlobalStats.ObjectManagerLocker)
            {
                if (!ActiveHull.Animated)
                {
                    ActiveModel = TransientContent.Load<Model>(ActiveHull.ModelPath);
                    CreateSOFromHull();
                }
                else
                {
                    ScreenManager.Remove(this.shipSO);
                    SkinnedModel sm = ResourceManager.GetSkinnedModel(this.ActiveHull.ModelPath);
                    this.shipSO = new SceneObject(sm.Model)
                    {
                        ObjectType = ObjectType.Dynamic,
                        World = WorldMatrix
                    };
                    ScreenManager.Submit(this.shipSO);
                    this.SetupSlots();
                }
            }
            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
            {
                if (slot.Position.X < LowestX)
                {
                    LowestX = slot.Position.X;
                }
                if (slot.Position.X <= HighestX)
                {
                    continue;
                }
                HighestX = slot.Position.X;
            }
            float xDistance = HighestX - LowestX;
            Viewport viewport5 = Viewport;
            Vector3 pScreenSpace = viewport5.Project(Vector3.Zero, Projection, View, Matrix.Identity);
            var pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
            Vector2 radialPos = MathExt.PointOnCircle(90f, xDistance);
            Viewport viewport6 = Viewport;
            Vector3 insetRadialPos = viewport6.Project(new Vector3(radialPos, 0f), Projection, View,
                Matrix.Identity);
            Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
            float radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
            if (radius >= xDistance)
            {
                while (radius > xDistance)
                {
                    camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
                    View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                                 Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                                    new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    Viewport viewport7 = Viewport;
                    pScreenSpace = viewport7.Project(Vector3.Zero, Projection, View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport8 = Viewport;
                    insetRadialPos = viewport8.Project(new Vector3(radialPos, 0f), Projection, View,
                        Matrix.Identity);
                    insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    cameraPosition.Z = cameraPosition.Z + 1f;
                }
            }
            else
            {
                while (radius < xDistance)
                {
                    camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
                    View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                                 Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                                    new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    Viewport viewport9 = Viewport;
                    pScreenSpace = viewport9.Project(Vector3.Zero, Projection, View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport10 = Viewport;
                    insetRadialPos = viewport10.Project(new Vector3(radialPos, 0f), Projection, View,
                        Matrix.Identity);
                    insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    cameraPosition.Z = cameraPosition.Z - 1f;
                }
            }
            BlackBar = new Rectangle(0,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70, 3000, 70);
            SideBar = new Rectangle(0, 0, 280,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            Rectangle w = new Rectangle(20, ModSel.Menu.Y - 10, 32, 32);
            Rectangle p = new Rectangle(80, w.Y, 32, 32);
            Rectangle df = new Rectangle(150, w.Y, 32, 32);
            Rectangle sp = new Rectangle(220, w.Y, 32, 32);
            wpn = new SkinnableButton(w, "Modules/FlakTurret3x3")
            {
                IsToggle = true,
                Toggled = true
            };
            pwr = new SkinnableButton(p, "Modules/NuclearReactorMedium")
            {
                IsToggle = true
            };
            def = new SkinnableButton(df, "Modules/SteelArmorMedium")
            {
                IsToggle = true
            };
            spc = new SkinnableButton(sp, "Modules/sensors_2x2")
            {
                IsToggle = true
            };
            SelectedCatTextPos = new Vector2(20f, w.Y - 25 - Fonts.Arial20Bold.LineSpacing / 2);
            SearchBar =
                new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 585,
                    ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47, 210, 25);
            ClassifCursor =
                new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .5f,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height + 10);
            var cursor = new Vector2(ClassifCursor.X, ClassifCursor.Y);
            Vector2 ordersBarPos = new Vector2(cursor.X, (int) cursor.Y + 20);
            ordersBarPos.X = ordersBarPos.X - 15;
            ToggleButton attackRuns =
                new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_headon");
            CombatStatusButtons.Add(attackRuns);
            attackRuns.Action = "attack";
            attackRuns.HasToolTip = true;
            attackRuns.WhichToolTip = 1;

            ordersBarPos.X = ordersBarPos.X + 29f;
            ToggleButton shortRange =
                new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_grid");
            CombatStatusButtons.Add(shortRange);
            shortRange.Action = "short";
            shortRange.HasToolTip = true;
            shortRange.WhichToolTip = 228;

            ordersBarPos.X = ordersBarPos.X + 29f;
            ToggleButton artillery = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_aft");
            CombatStatusButtons.Add(artillery);
            artillery.Action = "arty";
            artillery.HasToolTip = true;
            artillery.WhichToolTip = 2;

            ordersBarPos.X = ordersBarPos.X + 29f;
            ToggleButton holdPos = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_x");
            CombatStatusButtons.Add(holdPos);
            holdPos.Action = "hold";
            holdPos.HasToolTip = true;
            holdPos.WhichToolTip = 65;
            ordersBarPos.X = ordersBarPos.X + 29f;
            ToggleButton orbitLeft = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_left");
            CombatStatusButtons.Add(orbitLeft);
            orbitLeft.Action = "orbit_left";
            orbitLeft.HasToolTip = true;
            orbitLeft.WhichToolTip = 3;
            ordersBarPos.Y = ordersBarPos.Y + 29f;

            ToggleButton broadsideLeft =
                new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_bleft");
            CombatStatusButtons.Add(broadsideLeft);
            broadsideLeft.Action = "broadside_left";
            broadsideLeft.HasToolTip = true;
            broadsideLeft.WhichToolTip = 159;
            ordersBarPos.Y = ordersBarPos.Y - 29f;
            ordersBarPos.X = ordersBarPos.X + 29f;
            ToggleButton orbitRight =
                new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_right");
            CombatStatusButtons.Add(orbitRight);
            orbitRight.Action = "orbit_right";
            orbitRight.HasToolTip = true;
            orbitRight.WhichToolTip = 4;
            ordersBarPos.Y = ordersBarPos.Y + 29f;

            ToggleButton broadsideRight =
                new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_bright");
            CombatStatusButtons.Add(broadsideRight);
            broadsideRight.Action = "broadside_right";
            broadsideRight.HasToolTip = true;
            broadsideRight.WhichToolTip = 160;
            ordersBarPos.Y = ordersBarPos.Y - 29f;
            ordersBarPos.X = ordersBarPos.X + 29f;
            ToggleButton evade = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_stop");
            CombatStatusButtons.Add(evade);
            evade.Action = "evade";
            evade.HasToolTip = true;
            evade.WhichToolTip = 6;

            cursor = new Vector2(
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 150,
                (float) ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47);

            SaveButton = new UIButton
            {
                Rect = new Rectangle((int) cursor.X, (int) cursor.Y,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
                Text = Localizer.Token(105),
                Launches = "Save As..."
            };
            Buttons.Add(SaveButton);
            LoadButton = new UIButton
            {
                Rect = new Rectangle((int) cursor.X - 78, (int) cursor.Y,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
                Text = Localizer.Token(8),
                Launches = "Load"
            };
            Buttons.Add(LoadButton);
            ToggleOverlayButton = new UIButton
            {
                Rect = new Rectangle(LoadButton.Rect.X - 140, (int) cursor.Y,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width,
                    ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
                NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
                Launches = "Toggle Overlay",
                Text = Localizer.Token(106)
            };
            Buttons.Add(ToggleOverlayButton);
            BottomSep = new Rectangle(BlackBar.X, BlackBar.Y, BlackBar.Width, 1);
            HullSelectionRect =
                new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 285,
                    (LowRes ? 45 : 100), 280, (LowRes ? 350 : 400));
            HullSelectionSub = new Submenu(ScreenManager, HullSelectionRect, true);
            WeaponSl = new ScrollList(ModSel);
            HullSelectionSub.AddTab(Localizer.Token(107));
            HullSL = new ScrollList(HullSelectionSub);
            var categories = new Array<string>();
            foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
            {
                if (!EmpireManager.Player.GetHDict()[hull.Key])
                {
                    continue;
                }
                string cat = Localizer.GetRole(hull.Value.Role, EmpireManager.Player);
                if (categories.Contains(cat))
                {
                    continue;
                }
                categories.Add(cat);
            }
            categories.Sort();
            foreach (string cat in categories)
            {
                var type = new ModuleHeader(cat, 240f);
                HullSL.AddItem(type);
            }
            foreach (ScrollList.Entry e in HullSL.Entries)
            {
                foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
                {
                    if (!EmpireManager.Player.GetHDict()[hull.Key] ||
                        ((ModuleHeader) e.item).Text != Localizer.GetRole(hull.Value.Role, EmpireManager.Player))
                    {
                        continue;
                    }
                    e.AddItem(hull.Value);
                }
            }
            Rectangle shipStatsPanel = new Rectangle(HullSelectionRect.X + 50,
                HullSelectionRect.Y + HullSelectionRect.Height - 20, 280, 320);


            //base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth
            DropdownRect =
                new Rectangle((int) (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .25f),
                    (int) ordersBarPos.Y, 100, 18);

            CategoryList = new DropOptions(DropdownRect);

            foreach (ShipData.Category item in Enum.GetValues(typeof(ShipData.Category))
                .Cast<ShipData.Category>())
            {
                CategoryList.AddOption(item.ToString(), (int) item + 1);
            }

            CarrierOnly = ActiveHull.CarrierShip;
            CoBoxCursor = new Vector2(DropdownRect.X + 106, DropdownRect.Y);
            CarrierOnlyBox = new Checkbox(CoBoxCursor.X, CoBoxCursor.Y, () => CarrierOnly, Fonts.Arial12Bold,
                "Carrier Only", 0);

            ShipStats = new Menu1(ScreenManager, shipStatsPanel);
            StatsSub  = new Submenu(ScreenManager, shipStatsPanel);
            StatsSub.AddTab(Localizer.Token(108));
            ArcsButton = new GenericButton(new Vector2(HullSelectionRect.X - 32, 97f), "Arcs",
                Fonts.Pirulen20,
                Fonts.Pirulen16); 
            Close = new CloseButton(new Rectangle(
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 27, 99, 20, 20));
            OriginalZ = cameraPosition.Z;
        }

        private string parseText(string text, float width, SpriteFont font)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strArrays = text.Split(' ');
            for (int i = 0; i < strArrays.Length; i++)
            {
                string word = strArrays[i];
                if (font.MeasureString(string.Concat(line, word)).Length() > width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }

        private void ReallyExit()
        {
            LightRig rig = TransientContent.Load<LightRig>("example/NewGamelight_rig");
            rig.AssignTo(this);

            lock (GlobalStats.ObjectManagerLocker)
            {
                ScreenManager.Remove(this.shipSO);
            }
            if (Empire.Universe.LookingAtPlanet && Empire.Universe.workersPanel is ColonyScreen)
            {
                (Empire.Universe.workersPanel as ColonyScreen).Reset = true;
            }
            //this should go some where else, need to find it a home
            ScreenManager.RemoveScreen(this);
            base.ExitScreen();
        }

        public void ResetLists()
        {
            Reset = true;
            WeaponSl.indexAtTop = 0;
        }

        private void ResetModuleState()
        {
            ActiveModState = ActiveModuleState.Normal;
        }

        private void SaveChanges(object sender, EventArgs e)
        {
            ScreenManager.AddScreen(new DesignManager(this, ActiveHull.Name));
            ShipSaved = true;
        }

        public void SaveShipDesign(string name)
        {
            ActiveHull.ModuleSlots = Empty<ModuleSlotData>.Array;
            ActiveHull.Name        = name;
            ShipData toSave        = ActiveHull.GetClone();

            toSave.ModuleSlots = new ModuleSlotData[Slots.Count];
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                ModuleSlotData savedSlot = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions
                };
                if (slot.Module != null)
                {
                    savedSlot.Facing = slot.Module.Facing;

                    if (slot.Module.ModuleType == ShipModuleType.Hangar)
                        savedSlot.SlotOptions = slot.Module.hangarShipUID;
                }
                toSave.ModuleSlots[i] = savedSlot;
            }
            string path = Dir.ApplicationData;
            toSave.CombatState = CombatState;
            toSave.Name = name;

            //Cases correspond to the 5 options in the drop-down menu; default exists for... Propriety, mainly. The option selected when saving will always be the Category saved, pretty straightforward.
            foreach (var item in Enum.GetValues(typeof(ShipData.Category)).Cast<ShipData.Category>())
            {
                if (CategoryList.Options[CategoryList.ActiveIndex].Name == item.ToString())
                {
                    ActiveHull.ShipCategory = item;
                    break;
                }
            }

            //Adds the category determined by the case from the dropdown to the 'toSave' ShipData.
            toSave.ShipCategory = ActiveHull.ShipCategory;

            //Adds the boolean derived from the checkbox boolean (CarrierOnly) to the ShipData. Defaults to 'false'.
            toSave.CarrierShip = CarrierOnly;
            var serializer = new XmlSerializer(typeof(ShipData));
            using (var ws = new StreamWriter($"{path}/StarDrive/Saved Designs/{name}.xml"))
                serializer.Serialize(ws, toSave);
            ShipSaved = true;

            Ship newShip = Ship.CreateShipFromShipData(toSave, fromSave: false);
            if (newShip == null) // happens if module creation failed
                return;
            newShip.InitializeStatus(fromSave: false);
            newShip.IsPlayerDesign = true;
            ResourceManager.ShipsDict[name] = newShip;

            newShip.BaseStrength = -1;
            newShip.BaseStrength = newShip.GetStrength();
            EmpireManager.Player.UpdateShipsWeCanBuild();
            ActiveHull.CombatState = CombatState;
            ChangeHull(ActiveHull);
        }

        private void SaveWIP(object sender, EventArgs e)
        {
            var savedShip = new ShipData
            {
                Animated     = this.ActiveHull.Animated,
                CombatState  = this.ActiveHull.CombatState,
                Hull         = this.ActiveHull.Hull,
                IconPath     = this.ActiveHull.IconPath,
                ModelPath    = this.ActiveHull.ModelPath,
                Name         = this.ActiveHull.Name,
                Role         = this.ActiveHull.Role,
                ShipStyle    = this.ActiveHull.ShipStyle,
                ThrusterList = this.ActiveHull.ThrusterList
            };

            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                var data = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions
                };
                if (slot.Module?.ModuleType == ShipModuleType.Hangar)
                    data.SlotOptions = slot.Module.hangarShipUID;
                savedShip.ModuleSlots[i] = data;
            }
            string path                = Dir.ApplicationData;
            CombatState defaultstate   = ActiveHull.CombatState;
            savedShip.CombatState      = CombatState;
            savedShip.Name             = $"{DateTime.Now:yyyy-MM-dd}__{ActiveHull.Name}";
            var serializer             = new XmlSerializer(typeof(ShipData));
            TextWriter writeFileStream =
                new StreamWriter(string.Concat(path, "/StarDrive/WIP/", savedShip.Name, ".xml"));
            serializer.Serialize(writeFileStream, savedShip);
            writeFileStream.Close();
            savedShip.CombatState = defaultstate;
            ShipSaved = true;
        }

        private void SaveWIPThenChangeHull(object sender, EventArgs e)
        {
            SaveWIP(sender, e);
            ChangeHull(Changeto);
        }

        private void SaveWIPThenLaunchScreen(object sender, EventArgs e)
        {
            SaveWIP(sender, e);
            string str  = ScreenToLaunch;
            string str1 = str;
            if (str    != null)
            {
                if (str1 == "Research")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = ScreenToLaunch;
            string str3 = str2;
            if (str2 != null)
            {
                if (str3 == "Main Menu")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                }
                else if (str3 == "Shipyard")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Empire")
                {
                    ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, EmpireUI));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Diplomacy")
                {
                    ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "?")
                {
                    GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                    InGameWiki wiki = new InGameWiki(this, new Rectangle(0, 0, 750, 600))
                    {
                        TitleText = "StarDrive Help",
                        MiddleText =
                            "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                    ScreenManager.AddScreen(wiki);
                }
            }
            ReallyExit();
        }

        private void SetupSlots()
        {
            Slots.Clear();
            foreach (ModuleSlotData slot in ActiveHull.ModuleSlots)
            {
                PrimitiveQuad pq = new PrimitiveQuad(slot.Position.X + offset.X - 8f,
                    slot.Position.Y + offset.Y - 8f, 16f, 16f);
                SlotStruct ss = new SlotStruct
                {
                    PQ            = pq,
                    Restrictions  = slot.Restrictions,
                    Facing        = slot.Facing,
                    ModuleUID     = slot.InstalledModuleUID,
                    SlotReference = slot,
                    SlotOptions   = slot.SlotOptions
                };
                Slots.Add(ss);
            }
            foreach (SlotStruct slot in Slots)
            {
                slot.SetValidity();
                if (slot.ModuleUID == null)
                {
                    continue;
                }
                ActiveModule = ShipModule.CreateNoParent(slot.ModuleUID);
                ChangeModuleState(slot.State);
                InstallModuleFromLoad(slot);
                if (slot.Module == null || slot.Module.ModuleType != ShipModuleType.Hangar)
                {
                    continue;
                }
                slot.Module.hangarShipUID = slot.SlotOptions;
            }
            ActiveModule = null;
            ActiveModState = ActiveModuleState.Normal;
        }

        private float GetHullDamageBonus()
        {
            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses)
                return 1f;
            HullBonus bonus;
            if (ResourceManager.HullBonuses.TryGetValue(ActiveHull.Hull, out bonus))
            {
                return 1f + bonus.DamageBonus;
            }
            return 1f;
        }

        private float GetHullFireRateBonus()
        {
            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses)
                return 1f;
            HullBonus bonus;
            if (ResourceManager.HullBonuses.TryGetValue(ActiveHull.Hull, out bonus))
            {
                return 1f - bonus.FireRateBonus;
            }
            return 1f;
        }
    }
}