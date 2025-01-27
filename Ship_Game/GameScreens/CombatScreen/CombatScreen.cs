using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.SpriteSystem;
using Ship_Game.Data;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using Ship_Game.UI;
using Ship_Game.Spatial;

namespace Ship_Game
{
    // TODO: GroundCombatScreen
    public sealed class CombatScreen : PlanetScreen
    {
        readonly Vector2 TitlePos;
        readonly Rectangle GridPos;

        readonly ScrollList<CombatScreenOrbitListItem> OrbitSL;
        PlanetGridSquare HoveredSquare;
        readonly Rectangle SelectedItemRect;
        Rectangle HoveredItemRect;
        Rectangle AssetsRect;
        readonly TroopInfoUIElement TInfo;
        readonly TroopInfoUIElement HInfo;
        readonly UIButton LandAll;
        readonly UIButton LaunchAll;
        readonly UIButton Bombard;
        readonly Rectangle GridRect;
        readonly Array<PointSet> CenterPoints = new();
        readonly Array<PointSet> PointsList   = new();
        readonly Array<PlanetGridSquare> MovementTiles = new();
        readonly Array<PlanetGridSquare> AttackTiles = new();
        bool ResetNextFrame;
        public PlanetGridSquare ActiveTile;
        float OrbitalAssetsTimer; // X seconds per Orbital Assets update

        readonly Array<PlanetGridSquare> ReversedList = new();
        readonly Array<SmallExplosion> Explosions = new();

        readonly float[] DistancesByRow = { 437f, 379f, 311f, 229f, 128f, 0f };
        readonly float[] WidthByRow     = { 110f, 120f, 132f, 144f, 162f, 183f };
        readonly float[] StartXByRow    =  { 254f, 222f, 181f, 133f, 74f, 0f };
        const string BombardDefaultText = "Bombard";

        public CombatScreen(GameScreen parent, Planet p) : base(parent, p)
        {
            GridRect            = new Rectangle(ScreenWidth / 2 - 639, ScreenHeight - 490, 1278, 437);
            Rectangle titleRect = new Rectangle(ScreenWidth / 2 - 250, 44, 500, 80);
            TitlePos            = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Arial20Bold.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            AssetsRect          = new Rectangle(10, 48, 225, 200);
            SelectedItemRect    = new Rectangle(10, 250, 225, 380);
            HoveredItemRect     = new Rectangle(10, 250, 225, 380);
            Add(new OrbitalAssetsUIElement(AssetsRect, ScreenManager, p.Universe.Screen, p));
            TInfo = Add(new TroopInfoUIElement(SelectedItemRect, ScreenManager, p.Universe.Screen));
            HInfo = Add(new TroopInfoUIElement(HoveredItemRect, ScreenManager, p.Universe.Screen));
            TInfo.Visible = HInfo.Visible = false;
            
            int assetsX = AssetsRect.X + 20;

            LandAll   = Button(ButtonStyle.DanButtonBlue, assetsX, AssetsRect.Y + 80, "Land All", OnLandAllClicked);
            LaunchAll = Button(ButtonStyle.DanButtonBlue, assetsX, AssetsRect.Y + 110, "Launch All", OnLaunchAllClicked);
            Bombard   = Button(ButtonStyle.DanButtonBlue, assetsX, AssetsRect.Y + 140, BombardDefaultText, OnBombardClicked);
            LandAll.Tooltip   = GameText.LandAllTroopsListedIn;
            LaunchAll.Tooltip = GameText.LaunchToSpaceAllTroops;
            Bombard.Tooltip   = GameText.OrdersAllBombequippedShipsIn;
            LandAll.TextAlign = LaunchAll.TextAlign = Bombard.TextAlign = ButtonTextAlign.Left;

            if (IsPlayerBombing())
                Bombard.Style = ButtonStyle.DanButtonRed;

            RectF orbitalAssetRect = new(assetsX + 220, AssetsRect.Y, 200, AssetsRect.Height * 2);
            var orbitalAssets = Add(new SubmenuScrollList<CombatScreenOrbitListItem>(orbitalAssetRect, "In Orbit", style:ListStyle.Blue));
            OrbitSL = orbitalAssets.List;
            OrbitSL.OnDoubleClick = OnTroopItemDoubleClick;
            OrbitSL.OnDragOut = OnTroopItemDrag;
            OrbitSL.EnableDragOutEvents = true;

            var colonyGrid = new Rectangle(ScreenWidth / 2 - ScreenWidth * 2 / 3 / 2, 130, ScreenWidth * 2 / 3, ScreenWidth * 2 / 3 * 5 / 7);
            GridPos = new Rectangle(colonyGrid.X + 20, colonyGrid.Y + 20, colonyGrid.Width - 40, colonyGrid.Height - 40);
            int xSize = GridPos.Width / 7;
            int ySize = GridPos.Height / 5;
            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                pgs.ClickRect = new Rectangle(GridPos.X + pgs.X * xSize, GridPos.Y + pgs.Y * ySize, xSize, ySize);
                foreach (var troop in pgs.TroopsHere)
                {
                    //@TODO HACK. first frame is getting overwritten or lost somewhere.
                    troop.WhichFrame = troop.first_frame;
                }
            }
            for (int row = 0; row < 6; row++)
            {
                for (int i = 0; i < 7; i++)
                {
                    var ps = new PointSet
                    {
                        point = new Vector2(GridRect.X + i * WidthByRow[row] + WidthByRow[row] / 2f + StartXByRow[row], GridRect.Y + GridRect.Height - DistancesByRow[row]),
                        row = row,
                        column = i
                    };
                    PointsList.Add(ps);
                }
            }

            foreach (PointSet ps in PointsList)
            {
                foreach (PointSet toCheck in PointsList)
                {
                    if (ps.column == toCheck.column && ps.row == toCheck.row - 1)
                    {
                        float distance = ps.point.Distance(toCheck.point);
                        Vector2 vtt = toCheck.point - ps.point;
                        vtt = vtt.Normalized();
                        Vector2 cPoint = ps.point + ((vtt * distance) / 2f);
                        var cp = new PointSet
                        {
                            point = cPoint,
                            row = ps.row,
                            column = ps.column
                        };
                        CenterPoints.Add(cp);
                    }
                }
            }

            foreach (PlanetGridSquare pgs in p.TilesList)
            {
                foreach (PointSet ps in CenterPoints)
                {
                    if (pgs.X == ps.column && pgs.Y == ps.row)
                        pgs.ClickRect = new Rectangle((int) ps.point.X - 32, (int) ps.point.Y - 32, 64, 64);
                }
            }

            foreach (PlanetGridSquare pgs in p.TilesList)
                ReversedList.Add(pgs);
        }

        void DetermineAttackAndMove()
        {
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (ActiveTile == null)
                    pgs.ShowAttackHover = false;
            }

            if (ActiveTile != null)
            {
                int range;
                MovementTiles.Clear();
                AttackTiles.Clear();
                if (!ActiveTile.LockOnOurTroop(us:Player, out Troop troop))
                {
                    if (ActiveTile.CombatBuildingOnTile)
                        range = 1;
                    else
                        return; // Nothing on this tile can move or attack
                }
                else
                {
                    range = troop.ActualRange;
                }

                foreach (PlanetGridSquare tile in P.TilesList)
                {
                    if (tile == ActiveTile)
                        continue;

                    int xTotalDistance = Math.Abs(ActiveTile.X - tile.X);
                    int yTotalDistance = Math.Abs(ActiveTile.Y - tile.Y);
                    int rangeToTile    = Math.Max(xTotalDistance, yTotalDistance);
                    if (rangeToTile <= range && tile.IsTileFree(Player))
                    {
                        if (!ActiveTile.CombatBuildingOnTile) 
                            MovementTiles.Add(tile); // Movement options only for mobile assets

                        if (tile.LockOnEnemyTroop(Player, out _) || tile.CombatBuildingOnTile && P.Owner != Player)
                            AttackTiles.Add(tile);
                    }
                }
            }
        }

        void DrawTroopDragDestinations()
        {
            if (!IsDraggingTroop)
                return;

            foreach (PlanetGridSquare pgs in ReversedList)
            {
                if ((pgs.Building == null && pgs.TroopsHere.Count == 0) ||
                    (pgs.Building != null && pgs.Building.CombatStrength == 0 && pgs.TroopsHere.Count == 0))
                {
                    Vector2 center = pgs.ClickRect.CenterF;
                    DrawCircle(center, 8f, Color.White, 4f);
                    DrawCircle(center, 6f, Color.Black, 3f);
                }
            }

            PlanetGridSquare toLand = P.FindTileUnderMouse(Input.CursorPosition);
            if (toLand != null)
            {
                DrawCircle(toLand.ClickRect.CenterF, 12f, Color.Orange, 2f);
            }
        }

        Color OwnerColor => P.Owner?.EmpireColor ?? Color.Gray;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (TransientContent == null) // disposed
                return;

            batch.Draw(ResourceManager.Texture($"PlanetTiles/{P.PlanetTileId}_tilt"), GridRect, Color.White);
            batch.Draw(ResourceManager.Texture("Ground_UI/grid"), GridRect, Color.White);
            batch.DrawString(Fonts.Arial20Bold, P.Name, TitlePos, OwnerColor);

            LaunchAll.Draw(batch, elapsed);
            LandAll.Draw(batch, elapsed);
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                if (pgs.BuildingOnTile)
                {
                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    var icon = TransientContent.LoadTextureOrDefault($"Textures/Buildings/icon_{pgs.Building.Icon}_64x64");
                    batch.Draw(icon, bRect, Color.White);
                }
            }
            foreach (PlanetGridSquare pgs in ReversedList)
            {
                DrawTileIcons(pgs);
                DrawCombatInfo(pgs);
            }

            DrawTroopDragDestinations();

            base.Draw(batch, elapsed);
            batch.SafeEnd();

            batch.SafeBegin(SpriteBlendMode.Additive);

            for (int i = 0; i < Explosions.Count; ++i)
            {
                SmallExplosion exp = Explosions[i];
                exp.Draw(batch);
            }
            batch.SafeEnd();

            batch.SafeBegin();
        }

        void DrawCombatInfo(PlanetGridSquare pgs)
        {
            if ((ActiveTile == null || ActiveTile != pgs) &&
                (pgs.Building == null || pgs.Building.CombatStrength <= 0 || ActiveTile == null ||
                 ActiveTile != pgs))
                return;

            var activeSel = new Rectangle(pgs.ClickRect.X - 5, pgs.ClickRect.Y - 5, pgs.ClickRect.Width + 10, pgs.ClickRect.Height + 10);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Square Selection"), activeSel, Color.White);
            foreach (PlanetGridSquare nearby in ReversedList)
            {
                if (nearby != pgs && nearby.ShowAttackHover)
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Ground_UI/GC_Attack_Confirm"),
                        nearby.ClickRect, Color.White);
            }
        }

        void DrawTileIcons(PlanetGridSquare pgs)
        {
            SpriteBatch batch = ScreenManager.SpriteBatch;

            int width = (pgs.Y * 15 + 64).UpperBound(128);
            if (pgs.CombatBuildingOnTile)
                width = 64;

            if (pgs.TroopsAreOnTile)
            {
                for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                {
                    Troop troop = pgs.TroopsHere[i];
                    troop.SetCombatScreenRect(pgs, width);
                    Rectangle troopClickRect = troop.ClickRect;
                    if (troop.MovingTimer > 0f)
                    {
                        float amount = 1f - troop.MovingTimer;
                        troopClickRect.X = troop.FromRect.X.LerpTo(troop.ClickRect.X, amount);
                        troopClickRect.Y = troop.FromRect.Y.LerpTo(troop.ClickRect.Y, amount);
                        troopClickRect.Width = troop.FromRect.Width.LerpTo(troop.ClickRect.Width, amount);
                        troopClickRect.Height = troop.FromRect.Height.LerpTo(troop.ClickRect.Height, amount);
                    }
                    troop.Draw(P.Universe, batch, troopClickRect);
                    var moveRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 38, 12, 12);
                    if (troop.AvailableMoveActions <= 0)
                    {
                        int moveTimer = (int)troop.MoveTimer + 1;
                        batch.DrawDropShadowText1(moveTimer.ToString(), new Vector2((moveRect.X + 4), moveRect.Y), Fonts.Arial12, Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Move"), moveRect, Color.White);
                    }
                    var attackRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 23, 12, 12);
                    if (troop.AvailableAttackActions <= 0)
                    {
                        int attackTimer = (int)troop.AttackTimer + 1;
                        batch.DrawDropShadowText1(attackTimer.ToString(), new Vector2((attackRect.X + 4), attackRect.Y), Fonts.Arial12, Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), attackRect, Color.White);
                    }

                    var strengthRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 5,
                                                     Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    DrawTroopData(batch, strengthRect, troop, troop.Strength.String(1), Color.White);

                    //Fat Bastard - show TroopLevel
                    if (troop.Level > 0)
                    {
                        var levelRect = new Rectangle(troopClickRect.X + troopClickRect.Width + 2, troopClickRect.Y + 52,
                                                      Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                        DrawTroopData(batch, levelRect, troop, troop.Level.ToString(), Color.Gold);
                    }
                    if (ActiveTile != null && ActiveTile == pgs)
                    {
                        if (troop.AvailableAttackActions > 0)
                        {
                            foreach (PlanetGridSquare attackTile in AttackTiles)
                            {
                                batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), attackTile.ClickRect, Color.White);
                            }
                        }

                        if (troop.CanMove)
                        {
                            foreach (PlanetGridSquare moveTile in MovementTiles)
                            {
                                batch.FillRectangle(moveTile.ClickRect, new Color(255, 255, 255, 30));
                                Vector2 center = moveTile.ClickRect.CenterF;
                                DrawCircle(center, 5f, Color.White, 5f);
                                DrawCircle(center, 5f, Color.Black);
                            }
                        }
                    }
                }
            }
            else if (pgs.BuildingOnTile)
            {
                if (!pgs.CombatBuildingOnTile)
                {
                    var bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 32, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 32, 64, 64);
                    var strengthRect = new Rectangle(bRect.X + bRect.Width + 2, bRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                    batch.DrawRectangle(strengthRect, P.Owner?.EmpireColor ?? Color.Gray);
                    var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.Building.Strength.ToString()).X / 2f,
                                             (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12, pgs.Building.Strength.ToString(), cursor, Color.White);
                }
                else
                {
                    var attackRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width + 2, pgs.ClickRect.Y + 23, 12, 12);
                    if (pgs.Building.AvailableAttackActions <= 0)
                    {
                        int num = (int)pgs.Building.AttackTimer + 1;
                        batch.DrawString(Fonts.Arial12, num.ToString(), new Vector2((attackRect.X + 4), attackRect.Y), Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/Ground_Attack"), attackRect, Color.White);
                    }
                    var strengthRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width + 2, pgs.ClickRect.Y + 5, Fonts.Arial12.LineSpacing + 8, Fonts.Arial12.LineSpacing + 4);
                    batch.FillRectangle(strengthRect, new Color(0, 0, 0, 200));
                    batch.DrawRectangle(strengthRect, P.Owner?.EmpireColor ?? Color.LightGray);
                    var cursor = new Vector2((strengthRect.X + strengthRect.Width / 2) - Fonts.Arial12.MeasureString(pgs.Building.CombatStrength.ToString()).X / 2f,
                                             (1 + strengthRect.Y + strengthRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
                    batch.DrawString(Fonts.Arial12, pgs.Building.CombatStrength.ToString(), cursor, Color.White);
                }

                if (ActiveTile != null && ActiveTile == pgs && ActiveTile.Building.CanAttack)
                {
                    foreach (PlanetGridSquare attackTile in AttackTiles)
                    {
                        batch.Draw(ResourceManager.Texture("Ground_UI/GC_Potential_Attack"), attackTile.ClickRect, Color.White);
                    }
                }
            }
        }

        void DrawTroopData(SpriteBatch batch, Rectangle rect, Troop troop, string data, Color color)
        {
            Graphics.Font font = Fonts.Arial12;
            batch.FillRectangle(rect, new Color(0, 0, 0, 200));
            batch.DrawRectangle(rect, troop.Loyalty.EmpireColor);
            var cursor = new Vector2((rect.X + rect.Width / 2) - font.MeasureString(troop.Strength.String(1)).X / 2f,
                (1 + rect.Y + rect.Height / 2 - font.LineSpacing / 2));
            batch.DrawString(font, data, cursor, color);
        }

        void OnLandAllClicked(UIButton b)
        {
            bool instantLand = P.WeCanLandTroopsViaSpacePort(Player);
            if (instantLand)
                GameAudio.TroopLand();

            for (int i = OrbitSL.AllEntries.Count-1; i >= 0; i--)
            {
                CombatScreenOrbitListItem item = OrbitSL.AllEntries[i];
                if (instantLand)
                {
                    TryLandTroop(item);
                    continue;
                }

                Ship troopShip = item.Troop.HostShip;
                if (troopShip != null
                    && troopShip.AI.State != AI.AIState.Rebase
                    && troopShip.AI.State != AI.AIState.RebaseToShip
                    && troopShip.AI.State != AI.AIState.AssaultPlanet)
                {
                    troopShip.AI.OrderLandAllTroops(P, clearOrders:true);
                }
            }
            OrbitSL.Reset();
        }

        void OnLaunchAllClicked(UIButton b)
        {
            bool play = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.NoTroopsOnTile || !pgs.LockOnOurTroop(us:Player, out Troop troop))
                    continue;

                try
                {
                    troop.UpdateAttackActions(-troop.MaxStoredActions);
                    troop.ResetAttackTimer();
                    Ship troopShip = troop.Launch(pgs);
                    if (troopShip != null)
                        play = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Troop Launch Crash");
                }
            }

            if (!play)
                GameAudio.NegativeClick();
            else
            {
                GameAudio.TroopTakeOff();
                ResetNextFrame = true;
            }
        }

        bool IsPlayerBombing()
        {
            if (!TryGetNumBombersCanBomb(out Ship[] bomberList))
                return false;

            return bomberList.Any(s => s.AI.State == AI.AIState.Bombard && s.AI.OrderQueue.Any(o => o.TargetPlanet == P));
        }

        void OnBombardClicked(UIButton b)
        {
            if (!TryGetNumBombersCanBomb(out Ship[] bomberList))
                return;

            var bombingNowList = bomberList.Filter(s => s.AI.State == AI.AIState.Bombard && s.AI.OrderQueue.Any(o => o.TargetPlanet == P));
            if (bombingNowList.Length > 0) // need to cancel bombing
            {
                Bombard.Style = ButtonStyle.DanButtonBlue;
                foreach (Ship bomber in bombingNowList)
                    bomber.OrderToOrbit(P, clearOrders:!Input.IsShiftKeyDown, AI.MoveOrder.Aggressive);
            }
            else
            {
                // Cancel bombardment 
                Bombard.Style = ButtonStyle.DanButtonRed;
                foreach (Ship bomber in bomberList)
                {
                    bomber.AI.OrderBombardPlanet(P, clearOrders:true);
                }
            }
        }

        bool TryGetNumBombersCanBomb(out Ship[] bombersList)
        {
            bombersList = P.System.ShipList.Filter(s => s.Loyalty == Player
                                                         && s.BombBays.Count > 0
                                                         && s.Position.InRadius(P.Position, P.Radius + 15000f));

            return bombersList?.Length > 0;
        }

        void OnTroopItemDoubleClick(CombatScreenOrbitListItem item)
        {
            if (P.WeCanLandTroopsViaSpacePort(item.Troop.Loyalty))
                TryLandTroop(item);
            else
                TryLandTroopViaShip(item);
        }

        bool IsDraggingTroop;

        void OnTroopItemDrag(CombatScreenOrbitListItem item, DragEvent evt, bool outside)
        {
            if (evt == DragEvent.Begin)
            {
                IsDraggingTroop = true;
            }
            else if (evt == DragEvent.End)
            {
                IsDraggingTroop = false;
                if (outside && item != null) // TODO: not sure how this can be null, but somehow it happens
                {
                    PlanetGridSquare toLand = P.FindTileUnderMouse(Input.CursorPosition);
                    if (P.WeCanLandTroopsViaSpacePort(item.Troop.Loyalty))
                        TryLandTroop(item, toLand);
                    else
                        TryLandTroopViaShip(item);
                }
                else
                {
                    GameAudio.NegativeClick();
                }
            }
        }

        void TryLandTroopViaShip(CombatScreenOrbitListItem item)
        {
            Ship ship = item.Troop.HostShip;
            if (ship != null && ship.Carrier.TryScrambleSingleAssaultShuttle(item.Troop, out Ship shuttle))
                shuttle.AI.OrderLandAllTroops(P, clearOrders:true);
        }

        void TryLandTroop(CombatScreenOrbitListItem item,
                          PlanetGridSquare where = null)
        {
            if (item.Troop.TryLandTroop(P, where))
            {
                GameAudio.TroopLand();
                OrbitSL.Remove(item);
                OrbitalAssetsTimer = 0;
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }


        public override bool HandleInput(InputState input)
        {
            bool inputCaptured = base.HandleInput(input);

            if (P.Universe.Debug && (input.SpawnRemnant || input.SpawnPlayerTroop))
            {
                Empire spawnFor = input.SpawnRemnant ? Universe.Remnants : Player;
                if (Universe.Remnants == null)
                    Log.Warning("Remnant faction missing!");
                else
                {
                    if (!ResourceManager.TryCreateTroop("Wyvern", spawnFor, out Troop troop) ||
                        !troop.TryLandTroop(P))
                    {
                        return false; // eek-eek
                    }
                }
            }

            HoveredSquare = null;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.ClickRect.HitTest(input.CursorPosition) && (pgs.TroopsHere.Count != 0 || pgs.Building != null))
                    HoveredSquare = pgs;
            }

            inputCaptured |= HandleInputPlanetGridSquares();
            
            if (ActiveTile != null && !inputCaptured && Input.LeftMouseClick && !SelectedItemRect.HitTest(input.CursorPosition))
                ActiveTile = null;
            
            TInfo.SetTile(ActiveTile);
            HInfo.SetTile(HoveredSquare);
            if (HInfo.Visible)
                TInfo.Visible = false;
            else
                TInfo.Visible = ActiveTile != null;

            DetermineAttackAndMove();

            return inputCaptured;
        }

        // TODO: this needs a majro refactor
        bool HandleInputPlanetGridSquares()
        {
            bool capturedInput = false;
            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (!pgs.ClickRect.HitTest(Input.CursorPosition))
                    pgs.Highlighted = false;
                else
                {
                    if (!pgs.Highlighted)
                        GameAudio.ButtonMouseOver();

                    pgs.Highlighted = true;
                }

                if (pgs.BuildingOnTile && pgs.ClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                {
                    ActiveTile = pgs;
                    TInfo.SetTile(pgs);
                    capturedInput = true;
                }

                if (pgs.TroopsAreOnTile)
                {
                    for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                    {
                        Troop troop = pgs.TroopsHere[i];
                        if (troop.ClickRect.HitTest(Input.CursorPosition) && Input.LeftMouseClick)
                        {
                            if (P.Owner != Player)
                            {
                                ActiveTile = pgs;
                                TInfo.SetTile(pgs, troop);
                                capturedInput = true;
                            }
                            else
                            {
                                foreach (PlanetGridSquare p1 in P.TilesList)
                                {
                                    p1.ShowAttackHover = false;
                                }

                                ActiveTile = pgs;
                                TInfo.SetTile(pgs, troop);
                                capturedInput = true;
                            }
                        }
                    }
                }

                if (ActiveTile == null) 
                    continue;

                if (Input.LeftMouseClick && pgs.ClickRect.HitTest(Input.CursorPosition))
                {
                    if (ActiveTile.CombatBuildingOnTile 
                        && ActiveTile.Building.CanAttack  // Attacking building
                        && pgs.LockOnEnemyTroop(Player, out Troop enemy))
                    {
                        ActiveTile.Building.UpdateAttackActions(-1);
                        ActiveTile.Building.ResetAttackTimer();
                        StartCombat(ActiveTile.Building, enemy, pgs, P);
                    }
                    else if (ActiveTile.LockOnOurTroop(us:Player, out Troop ourTroop)) // Attacking troops
                    {
                        if (AttackTiles.Contains(pgs))
                        {
                            if (pgs.CombatBuildingOnTile) // Defending building
                            {
                                StartCombat(ourTroop, pgs.Building, pgs, P);
                                capturedInput = true;
                            }
                            else if (pgs.LockOnEnemyTroop(Player, out Troop enemyTroop))
                            {
                                ourTroop.UpdateAttackActions(-1);
                                ourTroop.ResetAttackTimer();
                                ourTroop.UpdateMoveActions(-1);
                                ourTroop.ResetMoveTimer();
                                StartCombat(ourTroop, enemyTroop, pgs, P);
                                capturedInput = true;
                            }
                        }

                        if (ourTroop.CanMove && MovementTiles.Contains(pgs))
                        {
                            ourTroop.facingRight = pgs.X > ActiveTile.X;

                            P.Troops.MoveTowardsTarget(ourTroop, ActiveTile, pgs);

                            P.SetInGroundCombat(ourTroop.Loyalty);
                            GameAudio.PlaySfxAsync(ourTroop.MovementCue);

                            ActiveTile = pgs;
                            MovementTiles.Remove(pgs);
                            capturedInput = true;
                        }
                    }
                }
            }
            
            return capturedInput;
        }

        public static void StartCombat(Troop attacker, Troop defender, PlanetGridSquare defenseTile, Planet planet)
        {
            Combat c = new(attacker, defender, defenseTile, planet);
            attacker.DoAttack();
            planet.ActiveCombats.Add(c);
        }

        public static void StartCombat(Troop attacker, Building defender, PlanetGridSquare defenseTile, Planet planet)
        {
            Combat c = new(attacker, defender, defenseTile, planet);
            attacker.DoAttack();
            planet.ActiveCombats.Add(c);
        }

        public static void StartCombat(Building attacker, Troop defender, PlanetGridSquare defenseTile, Planet planet)
        {
            Combat c = new(attacker, defender, defenseTile, planet);
            planet.ActiveCombats.Add(c);
        }

        public override void Update(float elapsedTime)
        {
            if (ResetNextFrame)
            {
                OrbitalAssetsTimer = 2;
                ResetNextFrame     = false;
            }

            OrbitSL.Visible = OrbitSL.NumEntries > 0;
            UpdateOrbitalAssets(elapsedTime);

            foreach (PlanetGridSquare pgs in P.TilesList)
            {
                if (pgs.TroopsAreOnTile)
                    for (int i = 0; i < pgs.TroopsHere.Count; ++i)
                        pgs.TroopsHere[i].Update(elapsedTime);
            }

            for (int i = Explosions.Count - 1; i >= 0; --i)
            {
                SmallExplosion exp = Explosions[i];
                if (exp.Update(elapsedTime))
                    Explosions.Remove(exp);
            }

            base.Update(elapsedTime);
        }

        void UpdateOrbitalAssets(float elapsedTime)
        {
            OrbitalAssetsTimer -= elapsedTime;
            if (OrbitalAssetsTimer > 0f)
                return;

            OrbitalAssetsTimer = 1;

            Array<Troop> orbitingTroops = GetOrbitingTroops(Player);

            OrbitSL.RemoveFirstIf(item => !orbitingTroops.ContainsRef(item.Troop));
            Troop[] toAdd = orbitingTroops.Filter(troop => !OrbitSL.Any(item => item.Troop == troop));

            foreach (Troop troop in toAdd)
                OrbitSL.AddItem(new CombatScreenOrbitListItem(P.Universe, troop));

            UpdateLaunchAllButton(P.Troops.NumTroopsCanLaunchFor(P.Universe.Player));
            UpdateLandAllButton(OrbitSL.NumEntries);
            UpdateBombersButton();
        }

        Array<Troop> GetOrbitingTroops(Empire owner)
        {
            // get our friendly ships
            SpatialObjectBase[] orbitingShips = P.Universe.Spatial.FindNearby(GameObjectType.Ship,
                                                P.Position, P.Radius+1500f, maxResults:128, onlyLoyalty:owner);

            // get a list of all the troops on those ships
            var troops = new Array<Troop>();
            foreach (SpatialObjectBase go in orbitingShips)
            {
                var ship = (Ship)go;
                if (ship.ShipData.Role != RoleName.troop)
                {
                    if (ship.HasOurTroops && (ship.Carrier.HasActiveTroopBays || ship.Carrier.HasTransporters || P.HasSpacePort && P.Owner == ship.Loyalty))  // fbedard
                    {
                        int landingLimit = LandingLimit(ship);
                        if (landingLimit > 0)
                            troops.AddRange(ship.GetOurTroops(landingLimit));
                    }
                }
                else if (ship.AI.State != AI.AIState.Rebase
                         && ship.AI.State != AI.AIState.RebaseToShip
                         && ship.AI.State != AI.AIState.AssaultPlanet)
                {
                    // this the default 1 troop ship or assault shuttle
                    if (ship.GetOurFirstTroop(out Troop first))
                        troops.Add(first);
                }
            }
            return troops;
        }


        int LandingLimit(Ship ship)
        {
            int landingLimit;
            if (P.WeCanLandTroopsViaSpacePort(ship.Loyalty))
            {
                // fbedard: Allows to unload all troops if there is a space port
                landingLimit = ship.TroopCount;
            }
            else
            {
                landingLimit  = ship.Carrier.AllActiveTroopBays.Count(bay => bay.HangarTimer <= 0);
                landingLimit += ship.Carrier.AllTransporters.Where(module => module.TransporterTimer <= 1).Sum(m => m.TransporterTroopLanding);
            }
            return landingLimit;
        }

        void UpdateLandAllButton(int numTroops)
        {
            if (numTroops > 0)
            {
                LandAll.Enabled = true;
                LandAll.Text    = $"Land All ({Math.Min(OrbitSL.NumEntries, P.GetFreeTiles(Player))})";
            }
            else
            {
                LandAll.Enabled = false;
                LandAll.Text     = "Land All";
            }

        }

        void UpdateLaunchAllButton(int numTroopsCanLaunch)
        {
            if (numTroopsCanLaunch > 0)
            {
                LaunchAll.Enabled = true;
                LaunchAll.Text    = $"Launch All ({numTroopsCanLaunch})";
            }
            else
            {
                LaunchAll.Enabled = false;
                LaunchAll.Text    = "Launch All";
            }
        }

        void UpdateBombersButton()
        {
            if (P.Owner == null || P.Owner == Player)
            {
                Bombard.Enabled = false;
                return;
            }

            if (TryGetNumBombersCanBomb(out Ship[] bomberList))
            {
                Bombard.Enabled = true;
                Bombard.Text = $"{BombardDefaultText} ({bomberList.Length})";
            }
            else
            {
                Bombard.Enabled = false;
                Bombard.Text    = BombardDefaultText;
            }
        }

        public bool TryLaunchTroopFromActiveTile()
        {
            PlanetGridSquare tile = ActiveTile;
            if (tile == null || tile.TroopsHere.Count < 1)
                return false;

            Ship launched = tile.TroopsHere[0].Launch(tile);
            if (launched == null)
                return false;

            ActiveTile = null; // TODO: Handle ActiveTile in a better way?
            return true;
        }

        public void AddExplosion(Rectangle grid, int size)
        {
            if (IsDisposed) return;
            var exp = new SmallExplosion(TransientContent, grid, size);
            Explosions.Add(exp);
        }

        struct PointSet
        {
            public Vector2 point;
            public int row;
            public int column;
        }

        // small explosion in planetary combat screen
        public class SmallExplosion
        {
            float Time;
            int Frame;
            const float Duration = 2.25f;
            readonly TextureAtlas Animation;
            readonly Rectangle Grid;

            public SmallExplosion(GameContentManager content, Rectangle grid, int size)
            {
                Grid = grid;
                string anim = size <= 3 ? "Textures/sd_explosion_12a_bb" : "Textures/sd_explosion_14a_bb";
                Animation = content.LoadTextureAtlas(anim);
            }

            public bool Update(float elapsedTime)
            {
                Time += elapsedTime;
                if (Time > Duration)
                    return true;

                int frame = (int)(Time / Duration * Animation.Count) ;
                Frame = frame.Clamped(0, Animation.Count-1);
                return false;
            }

            public void Draw(SpriteBatch batch)
            {
                batch.Draw(Animation[Frame], Grid, Color.White);
            }
        }
    }
}