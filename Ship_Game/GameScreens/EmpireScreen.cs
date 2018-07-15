using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Ship_Game
{
    public sealed class EmpireScreen : GameScreen
    {
        private EmpireUIOverlay eui;

        //private bool LowRes;

        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 EMenu;

        private ScrollList ColoniesList;

        private Submenu ColonySubMenu;

        private Rectangle leftRect;

        private DropOptions<int> GovernorDropdown;

        private CloseButton close;

        private Rectangle eRect;

        private float ClickDelay = 0.25f;

        public float ClickTimer;

        private SortButton pop;

        private SortButton food;

        private SortButton prod;

        private SortButton res;

        private SortButton money;

        private Rectangle AutoButton;

        private Planet SelectedPlanet;


        public EmpireScreen(GameScreen parent, EmpireUIOverlay empUI) : base(parent)
        {
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
            base.IsPopup = true;
            eui = empUI;
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
            {
                //LowRes = true;
            }

            Rectangle titleRect = new Rectangle(2, 44,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 2 / 3, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(
                (float) (titleRect.X + titleRect.Width / 2) -
                Fonts.Laserian14.MeasureString(Localizer.Token(383)).X / 2f,
                (float) (titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
            leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 10,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight -
                (titleRect.Y + titleRect.Height) - 7);
            close = new CloseButton(this, new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
            EMenu = new Menu2(leftRect);
            eRect = new Rectangle(2, titleRect.Y + titleRect.Height + 25,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 40,
                (int) (0.66f * (float) (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight -
                                        (titleRect.Y + titleRect.Height) - 7)));
            while (eRect.Height % 80 != 0)
            {
                eRect.Height = eRect.Height - 1;
            }
            ColonySubMenu = new Submenu(eRect);
            ColoniesList = new ScrollList(ColonySubMenu, 80);
            //if (!firstSort || pop.Ascending !=true)

            foreach (Planet p in EmpireManager.Player.GetPlanets())
            {
                EmpireScreenEntry entry =
                    new EmpireScreenEntry(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 80, this);
                ColoniesList.AddItem(entry);
            }
            pop = new SortButton(eui.empire.data.ESSort, "pop");
            food = new SortButton(eui.empire.data.ESSort, "food");
            prod = new SortButton(eui.empire.data.ESSort, "prod");
            res = new SortButton(eui.empire.data.ESSort, "res");
            money = new SortButton(eui.empire.data.ESSort, "money");
            SelectedPlanet = ColoniesList.ItemAtTop<EmpireScreenEntry>().p;
            GovernorDropdown = new DropOptions<int>(this, new Rectangle(0, 0, 100, 18));
            GovernorDropdown.AddOption("--", 1);
            GovernorDropdown.AddOption(Localizer.Token(4064), 0);
            GovernorDropdown.AddOption(Localizer.Token(4065), 2);
            GovernorDropdown.AddOption(Localizer.Token(4066), 4);
            GovernorDropdown.AddOption(Localizer.Token(4067), 3);
            GovernorDropdown.AddOption(Localizer.Token(4068), 5);
            GovernorDropdown.AddOption(Localizer.Token(393), 6);
            GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(SelectedPlanet);
            if (GovernorDropdown.ActiveValue != (int) SelectedPlanet.colonyType)
            {
                SelectedPlanet.colonyType = (Planet.ColonyType) GovernorDropdown.ActiveValue;
                if (SelectedPlanet.colonyType != Planet.ColonyType.Colony)
                {
                    SelectedPlanet.FoodLocked = true;
                    SelectedPlanet.ProdLocked = true;
                    SelectedPlanet.ResLocked = true;
                    SelectedPlanet.GovernorOn = true;
                }
                else
                {
                    SelectedPlanet.GovernorOn = false;
                    SelectedPlanet.FoodLocked = false;
                    SelectedPlanet.ProdLocked = false;
                    SelectedPlanet.ResLocked = false;
                }
            }
            AutoButton = new Rectangle(0, 0, 140, 33);
            //firstSort = true;
        }

        public override void Draw(SpriteBatch batch)
        {
            Rectangle buildingsRect;
            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            TitleBar.Draw();
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(383), TitlePos, new Color(255, 239, 208));
            EMenu.Draw();
            Color TextColor = new Color(118, 102, 67, 50);
            ColoniesList.Draw(base.ScreenManager.SpriteBatch);
            EmpireScreenEntry e1 = ColoniesList.ItemAtTop<EmpireScreenEntry>();
            Rectangle PlanetInfoRect = new Rectangle(eRect.X + 22, eRect.Y + eRect.Height, (int)((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.3f), base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - eRect.Y - eRect.Height - 22);
            int iconSize = PlanetInfoRect.X + PlanetInfoRect.Height - (int)((float)(PlanetInfoRect.X + PlanetInfoRect.Height) * 0.4f);
            Rectangle PlanetIconRect = new Rectangle(PlanetInfoRect.X + 10, PlanetInfoRect.Y + PlanetInfoRect.Height / 2 - iconSize / 2, iconSize, iconSize);
            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", SelectedPlanet.PlanetType)], PlanetIconRect, Color.White);
            Vector2 nameCursor = new Vector2((float)(PlanetIconRect.X + PlanetIconRect.Width / 2) - Fonts.Pirulen16.MeasureString(SelectedPlanet.Name).X / 2f, (float)(PlanetInfoRect.Y + 15));
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, SelectedPlanet.Name, nameCursor, Color.White);
            Vector2 PNameCursor = new Vector2((float)(PlanetIconRect.X + PlanetIconRect.Width + 5), nameCursor.Y + 20f);
            string fmt = "0.#";
            float amount = 80f;
            if (GlobalStats.IsGermanOrPolish)
            {
                amount = amount + 25f;
            }
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(384), ":"), PNameCursor, Color.Orange);
            Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, SelectedPlanet.Type, InfoCursor, new Color(255, 239, 208));
            PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(385), ":"), PNameCursor, Color.Orange);
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            float population = SelectedPlanet.Population / 1000f;
            string str = population.ToString(fmt);
            float maxPopulation = (SelectedPlanet.MaxPopulation + SelectedPlanet.MaxPopBonus) / 1000f;
            batch.DrawString(arial12Bold, string.Concat(str, "/", maxPopulation.ToString(fmt)), InfoCursor, new Color(255, 239, 208));
            Rectangle hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(385), ":")).X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(75);
            }
            PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(386), ":"), PNameCursor, Color.Orange);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, SelectedPlanet.Fertility.ToString(fmt), InfoCursor, new Color(255, 239, 208));
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(20);
            }
            PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(387), ":"), PNameCursor, Color.Orange);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, SelectedPlanet.MineralRichness.ToString(fmt), InfoCursor, new Color(255, 239, 208));
            hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
            if (hoverRect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(21);
            }
            PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            PNameCursor.Y = PNameCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            string text = HelperFunctions.ParseText(Fonts.Arial12Bold, SelectedPlanet.Description, (float)(PlanetInfoRect.Width - PlanetIconRect.Width + 15));
            if (Fonts.Arial12Bold.MeasureString(text).Y + PNameCursor.Y <= (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 20))
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, PNameCursor, Color.White);
            }
            else
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, HelperFunctions.ParseText(Fonts.Arial12, SelectedPlanet.Description, (float)(PlanetInfoRect.Width - PlanetIconRect.Width + 15)), PNameCursor, Color.White);
            }
            Rectangle MapRect = new Rectangle(PlanetInfoRect.X + PlanetInfoRect.Width, PlanetInfoRect.Y, e1.QueueRect.X - (PlanetInfoRect.X + PlanetInfoRect.Width), PlanetInfoRect.Height);
            int desiredWidth = 700;
            int desiredHeight = 500;
            for (buildingsRect = new Rectangle(MapRect.X, MapRect.Y, desiredWidth, desiredHeight); !MapRect.Contains(buildingsRect); buildingsRect = new Rectangle(MapRect.X, MapRect.Y, desiredWidth, desiredHeight))
            {
                desiredWidth = desiredWidth - 7;
                desiredHeight = desiredHeight - 5;
            }
            buildingsRect = new Rectangle(MapRect.X + MapRect.Width / 2 - desiredWidth / 2, MapRect.Y, desiredWidth, desiredHeight);
            MapRect.X = buildingsRect.X;
            MapRect.Width = buildingsRect.Width;
            int xsize = buildingsRect.Width / 7;
            int ysize = buildingsRect.Height / 5;
            PlanetGridSquare pgs = new PlanetGridSquare();
            foreach (PlanetGridSquare realPgs in SelectedPlanet.TilesList)
            {
                pgs.Biosphere  = realPgs.Biosphere;
                pgs.building   = realPgs.building;
                pgs.ClickRect  = new Rectangle(buildingsRect.X + realPgs.x * xsize, buildingsRect.Y + realPgs.y * ysize, xsize, ysize);
                pgs.foodbonus  = realPgs.foodbonus;
                pgs.Habitable  = realPgs.Habitable;
                pgs.prodbonus  = realPgs.prodbonus;
                pgs.TroopsHere = realPgs.TroopsHere;
                pgs.resbonus   = realPgs.resbonus;
                

                pgs.ClickRect = new Rectangle(buildingsRect.X + pgs.x * xsize, buildingsRect.Y + pgs.y * ysize, xsize, ysize);


                if (!pgs.Habitable)
                {
                    base.ScreenManager.SpriteBatch.FillRectangle(pgs.ClickRect, new Color(0, 0, 0, 200));
                }
                base.ScreenManager.SpriteBatch.DrawRectangle(pgs.ClickRect, new Color(211, 211, 211, 70), 2f);
                if (pgs.building != null)
                {
                    Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 24, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 24, 48, 48);
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", pgs.building.Icon, "_48x48")), bRect, Color.White);
                }
                else if (pgs.QItem != null)
                {
                    Rectangle bRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width / 2 - 24, pgs.ClickRect.Y + pgs.ClickRect.Height / 2 - 24, 48, 48);
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Buildings/icon_", pgs.QItem.Building.Icon, "_48x48")), bRect, new Color(255, 255, 255, 128));
                }
                DrawPGSIcons(pgs);
            }
            base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("PlanetTiles/", SelectedPlanet.GetTile())), buildingsRect, Color.White);
    
            int xpos = (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - MapRect.Width) / 2;
            int ypos = (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - MapRect.Height) / 2;
            Rectangle rectangle = new Rectangle(xpos, ypos, MapRect.Width, MapRect.Height);
            base.ScreenManager.SpriteBatch.DrawRectangle(MapRect, new Color(118, 102, 67, 255));
            Rectangle GovernorRect = new Rectangle(MapRect.X + MapRect.Width, MapRect.Y, e1.TotalEntrySize.X + e1.TotalEntrySize.Width - (MapRect.X + MapRect.Width), MapRect.Height);
            base.ScreenManager.SpriteBatch.DrawRectangle(GovernorRect, new Color(118, 102, 67, 255));
            Rectangle portraitRect = new Rectangle(GovernorRect.X + 25, GovernorRect.Y + 25, 124, 148);
            if ((float)portraitRect.Width > 0.35f * (float)GovernorRect.Width)
            {
                portraitRect.Height = portraitRect.Height - (int)(0.25 * (double)portraitRect.Height);
                portraitRect.Width = portraitRect.Width - (int)(0.25 * (double)portraitRect.Width);
            }
            base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", EmpireManager.Player.data.PortraitName)), portraitRect, Color.White);
            base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), portraitRect, Color.White);
            if (SelectedPlanet.colonyType == Planet.ColonyType.Colony)
            {
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/x_red"), portraitRect, Color.White);
            }
            base.ScreenManager.SpriteBatch.DrawRectangle(portraitRect, new Color(118, 102, 67, 255));
            Vector2 TextPosition = new Vector2((float)(portraitRect.X + portraitRect.Width + 25), (float)portraitRect.Y);
            Vector2 GovPos = TextPosition;
            switch (SelectedPlanet.colonyType)
            {
                case Planet.ColonyType.Core:
                {
                    Localizer.Token(372);
                    break;
                }
                case Planet.ColonyType.Colony:
                {
                    Localizer.Token(376);
                    break;
                }
                case Planet.ColonyType.Industrial:
                {
                    Localizer.Token(373);
                    break;
                }
                case Planet.ColonyType.Research:
                {
                    Localizer.Token(375);
                    break;
                }
                case Planet.ColonyType.Agricultural:
                {
                    Localizer.Token(371);
                    break;
                }
                case Planet.ColonyType.Military:
                {
                    Localizer.Token(374);
                    break;
                }
                case Planet.ColonyType.TradeHub:
                {
                    Localizer.Token(393);
                    break;
                }
            }
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Governor", TextPosition, Color.White);
            TextPosition.Y = (float)(GovernorDropdown.Rect.Y + 25);
            string desc = "";
            switch (SelectedPlanet.colonyType)
            {
                case Planet.ColonyType.Core:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(378), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
                case Planet.ColonyType.Colony:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(382), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
                case Planet.ColonyType.Industrial:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(379), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
                case Planet.ColonyType.Research:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(381), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
                case Planet.ColonyType.Agricultural:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(377), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
                case Planet.ColonyType.Military:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(380), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
                case Planet.ColonyType.TradeHub:
                {
                    desc = HelperFunctions.ParseText(Fonts.Arial12Bold, Localizer.Token(394), (float)(GovernorRect.Width - 50 - portraitRect.Width - 25));
                    break;
                }
            }
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, desc, TextPosition, Color.White);
            desc = Localizer.Token(388);
            TextPosition = new Vector2((float)(AutoButton.X + AutoButton.Width / 2) - Fonts.Pirulen16.MeasureString(desc).X / 2f, (float)(AutoButton.Y + AutoButton.Height / 2 - Fonts.Pirulen16.LineSpacing / 2));

            GovernorDropdown.SetAbsPos(GovPos.X, GovPos.Y + Fonts.Arial12Bold.LineSpacing + 5);
            GovernorDropdown.Reset();
            GovernorDropdown.Draw(ScreenManager.SpriteBatch);

            if (ColoniesList.NumEntries > 0)
            {
                EmpireScreenEntry entry = ColoniesList.ItemAtTop<EmpireScreenEntry>();
                Vector2 TextCursor = new Vector2((float)(entry.SysNameRect.X + 30), (float)(eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(192), TextCursor, new Color(255, 239, 208));
                TextCursor = new Vector2((float)(entry.PlanetNameRect.X + 30), (float)(eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(389), TextCursor, new Color(255, 239, 208));
                pop.rect = new Rectangle(entry.PopRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_food"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_pop"], pop.rect, Color.White);
                food.rect = new Rectangle(entry.FoodRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_food"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], food.rect, Color.White);
                prod.rect = new Rectangle(entry.ProdRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_production"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], prod.rect, Color.White);
                res.rect = new Rectangle(entry.ResRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_science"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_science"].Width, ResourceManager.TextureDict["NewUI/icon_science"].Height);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], res.rect, Color.White);
                money.rect = new Rectangle(entry.MoneyRect.X + 15 - ResourceManager.TextureDict["NewUI/icon_money"].Width / 2, (int)TextCursor.Y, ResourceManager.TextureDict["NewUI/icon_money"].Width, ResourceManager.TextureDict["NewUI/icon_money"].Height);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_money"], money.rect, Color.White);
                TextCursor = new Vector2((float)(entry.SliderRect.X + 30), (float)(eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(390), TextCursor, new Color(255, 239, 208));
                TextCursor = new Vector2((float)(entry.StorageRect.X + 30), (float)(eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(391), TextCursor, new Color(255, 239, 208));
                TextCursor = new Vector2((float)(entry.QueueRect.X + 30), (float)(eRect.Y - Fonts.Arial20Bold.LineSpacing + 33));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(392), TextCursor, new Color(255, 239, 208));
            }
            Color smallHighlight = TextColor;
            smallHighlight.A = (byte)(TextColor.A / 2);

            int i = ColoniesList.indexAtTop;
            foreach (ScrollList.Entry e in ColoniesList.VisibleEntries)
            {
                var entry = (EmpireScreenEntry)e.item;
                if (i % 2 == 0)
                {
                    ScreenManager.SpriteBatch.FillRectangle(entry.TotalEntrySize, smallHighlight);
                }
                if (entry.p == SelectedPlanet)
                {
                    ScreenManager.SpriteBatch.FillRectangle(entry.TotalEntrySize, TextColor);
                }
                entry.SetNewPos(eRect.X + 22, e.Y);
                entry.Draw(ScreenManager);
                ScreenManager.SpriteBatch.DrawRectangle(entry.TotalEntrySize, TextColor);
                ++i;
            }
            Color lineColor = new Color(118, 102, 67, 255);
            Vector2 topLeftSL = new Vector2((float)e1.SysNameRect.X, (float)(eRect.Y + 35));
            Vector2 botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)e1.PlanetNameRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)e1.PopRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)e1.FoodRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2((float)e1.ProdRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2((float)e1.ResRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2((float)e1.MoneyRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, new Color(lineColor, 100));
            topLeftSL = new Vector2((float)e1.SliderRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)(e1.StorageRect.X + 5), (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)e1.QueueRect.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)e1.TotalEntrySize.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            topLeftSL = new Vector2((float)(e1.TotalEntrySize.X + e1.TotalEntrySize.Width), (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(topLeftSL, botSL, lineColor);
            Vector2 leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)PlanetInfoRect.Y);
            base.ScreenManager.SpriteBatch.DrawLine(leftBot, botSL, lineColor);
            leftBot = new Vector2((float)e1.TotalEntrySize.X, (float)(eRect.Y + 35));
            botSL = new Vector2(topLeftSL.X, (float)(eRect.Y + 35));
            base.ScreenManager.SpriteBatch.DrawLine(leftBot, botSL, lineColor);
            Vector2 pos = new Vector2((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - Fonts.Pirulen16.MeasureString("Paused").X - 13f, 44f);
            batch.DrawString(Fonts.Pirulen16, "Paused", pos, Color.White);
            close.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        private void DrawPGSIcons(PlanetGridSquare pgs)
        {
            if (pgs.Biosphere)
            {
                Rectangle biosphere = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y, 20, 20);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_biosphere_48x48"], biosphere, Color.White);
            }
            if (pgs.TroopsHere.Count > 0)
            {
                pgs.TroopClickRect = new Rectangle(pgs.ClickRect.X + pgs.ClickRect.Width - 36, pgs.ClickRect.Y, 35, 35);
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Troops/", pgs.TroopsHere[0].TexturePath)], pgs.TroopClickRect, Color.White);
            }
            float numFood = 0f;
            float numProd = 0f;
            float numRes = 0f;
            if (pgs.building != null)
            {
                if (pgs.building.PlusFlatFoodAmount > 0f || pgs.building.PlusFoodPerColonist > 0f)
                {
                    numFood = numFood + pgs.building.PlusFoodPerColonist * SelectedPlanet.Population / 1000f * SelectedPlanet.FarmerPercentage;
                    numFood = numFood + pgs.building.PlusFlatFoodAmount;
                }
                if (pgs.building.PlusFlatProductionAmount > 0f || pgs.building.PlusProdPerColonist > 0f)
                {
                    numProd = numProd + pgs.building.PlusFlatProductionAmount;
                    numProd = numProd + pgs.building.PlusProdPerColonist * SelectedPlanet.Population / 1000f * SelectedPlanet.WorkerPercentage;
                }
                if (pgs.building.PlusResearchPerColonist > 0f || pgs.building.PlusFlatResearchAmount > 0f)
                {
                    numRes = numRes + pgs.building.PlusResearchPerColonist * SelectedPlanet.Population / 1000f * SelectedPlanet.ResearcherPercentage;
                    numRes = numRes + pgs.building.PlusFlatResearchAmount;
                }
            }
            float total = numFood + numProd + numRes;
            float totalSpace = (float)(pgs.ClickRect.Width - 30);
            float spacing = totalSpace / total;
            Rectangle rect = new Rectangle(pgs.ClickRect.X, pgs.ClickRect.Y + pgs.ClickRect.Height - ResourceManager.TextureDict["NewUI/icon_food"].Height, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
            for (int i = 0; (float)i < numFood; i++)
            {
                if (numFood - (float)i <= 0f || numFood - (float)i >= 1f)
                {
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], rect, Color.White);
                }
                else
                {
                    Rectangle? nullable = null;
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], new Vector2((float)rect.X, (float)rect.Y), nullable, Color.White, 0f, Vector2.Zero, numFood - (float)i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numProd; i++)
            {
                if (numProd - (float)i <= 0f || numProd - (float)i >= 1f)
                {
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], rect, Color.White);
                }
                else
                {
                    Rectangle? nullable1 = null;
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], new Vector2((float)rect.X, (float)rect.Y), nullable1, Color.White, 0f, Vector2.Zero, numProd - (float)i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
            for (int i = 0; (float)i < numRes; i++)
            {
                if (numRes - (float)i <= 0f || numRes - (float)i >= 1f)
                {
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], rect, Color.White);
                }
                else
                {
                    Rectangle? nullable2 = null;
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_science"], new Vector2((float)rect.X, (float)rect.Y), nullable2, Color.White, 0f, Vector2.Zero, numRes - (float)i, SpriteEffects.None, 1f);
                }
                rect.X = rect.X + (int)spacing;
            }
        }

        public override bool HandleInput(InputState input)
        {
            Vector2 MousePos = new Vector2((float)input.MouseCurr.X, (float)input.MouseCurr.Y);
            ColoniesList.HandleInput(input);
            if (pop.rect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(Localizer.Token(2278));
            }
            if (pop.HandleInput(input) )
            {
                pop.saved = false;
                if (!pop.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.Population
                        select p;
                    pop.Ascending = true;
                    ResetListSorted(sortedList);
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.Population descending
                        select p;
                    ResetListSorted(sortedList);
                    pop.Ascending = false;
                }
            }
            if (food.rect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(139);
            }
            if (food.HandleInput(input) )
            {
                if (!food.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.NetFoodPerTurn - p.Consumption
                        select p;
                    food.Ascending = true;
                    ResetListSorted(sortedList);
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.NetFoodPerTurn - p.Consumption descending
                        select p;
                    ResetListSorted(sortedList);
                    food.Ascending = false;
                }
            }
            if (prod.rect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(140);
            }
            if (prod.HandleInput(input) )
            {
                
                if (!prod.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = EmpireManager.Player.GetPlanets().OrderBy<Planet, float>((Planet p) => {
                        if (p.Owner.data.Traits.Cybernetic == 0)
                        {
                            return p.NetProductionPerTurn;
                        }
                        return p.NetProductionPerTurn - p.Consumption;
                    });
                    prod.Ascending = true;
                    ResetListSorted(sortedList);
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = EmpireManager.Player.GetPlanets().OrderByDescending<Planet, float>((Planet p) => {
                        if (p.Owner.data.Traits.Cybernetic == 0)
                        {
                            return p.NetProductionPerTurn;
                        }
                        return p.NetProductionPerTurn - p.Consumption;
                    });
                    ResetListSorted(sortedList);
                    prod.Ascending = false;
                }
            }
            if (res.rect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(141);
            }
            if (res.HandleInput(input))
            {
                if (!res.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.NetResearchPerTurn
                        select p;
                    res.Ascending = true;
                    ResetListSorted(sortedList);
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.NetResearchPerTurn descending
                        select p;
                    ResetListSorted(sortedList);
                    res.Ascending = false;
                }
            }
            if (money.rect.HitTest(MousePos))
            {
                ToolTip.CreateTooltip(142);
            }
            if (money.HandleInput(input))
            {
                if (!money.Ascending)
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.NetIncome
                        select p;
                    money.Ascending = true;
                    ResetListSorted(sortedList);
                }
                else
                {
                    IOrderedEnumerable<Planet> sortedList = 
                        from p in EmpireManager.Player.GetPlanets()
                        orderby p.NetIncome descending
                        select p;
                    ResetListSorted(sortedList);
                    money.Ascending = false;
                }
            }
            foreach (ScrollList.Entry e in ColoniesList.VisibleEntries)
            {
                var entry = (EmpireScreenEntry)e.item;
                entry.HandleInput(input, base.ScreenManager);
                if (entry.TotalEntrySize.HitTest(MousePos) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                {
                    if (SelectedPlanet != entry.p)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        SelectedPlanet = entry.p;
                        GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(SelectedPlanet);
                        if (GovernorDropdown.ActiveValue != (int)SelectedPlanet.colonyType)
                        {
                            SelectedPlanet.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;
                            if (SelectedPlanet.colonyType != Planet.ColonyType.Colony)
                            {
                                SelectedPlanet.FoodLocked = true;
                                SelectedPlanet.ProdLocked = true;
                                SelectedPlanet.ResLocked = true;
                                SelectedPlanet.GovernorOn = true;
                            }
                            else
                            {
                                SelectedPlanet.GovernorOn = false;
                                SelectedPlanet.FoodLocked = false;
                                SelectedPlanet.ProdLocked = false;
                                SelectedPlanet.ResLocked = false;
                            }
                        }
                    }
                    if (ClickTimer >= ClickDelay || SelectedPlanet == null)
                    {
                        ClickTimer = 0f;
                    }
                    else
                    {
                        
                        Empire.Universe.SelectedPlanet = SelectedPlanet;
                        Empire.Universe.ViewPlanet(null);
                        ExitScreen();
                    }
                }
            }
            GovernorDropdown.HandleInput(input);
            if (GovernorDropdown.ActiveValue != (int)SelectedPlanet.colonyType)
            {
                SelectedPlanet.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;
                if (SelectedPlanet.colonyType != Planet.ColonyType.Colony)
                {
                    SelectedPlanet.FoodLocked = true;
                    SelectedPlanet.ProdLocked = true;
                    SelectedPlanet.ResLocked = true;
                    SelectedPlanet.GovernorOn = true;
                }
                else
                {
                    SelectedPlanet.GovernorOn = false;
                    SelectedPlanet.FoodLocked = false;
                    SelectedPlanet.ProdLocked = false;
                    SelectedPlanet.ResLocked = false;
                }
            }
            if (input.KeysCurr.IsKeyDown(Keys.U) && !input.KeysPrev.IsKeyDown(Keys.U) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }                
            if (input.Escaped || input.RightMouseClick || close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        public void ResetListSorted(IOrderedEnumerable<Planet> SortedList)
        {
            ColoniesList.Reset();
            foreach (Planet p in SortedList)
            {
                var entry = new EmpireScreenEntry(p, eRect.X + 22, leftRect.Y + 20, EMenu.Menu.Width - 30, 80, this);
                ColoniesList.AddItem(entry);
            }
            SelectedPlanet = ColoniesList.ItemAtTop<EmpireScreenEntry>().p;
            GovernorDropdown.ActiveIndex = ColonyScreen.GetIndex(SelectedPlanet);
            if (GovernorDropdown.ActiveValue != (int)SelectedPlanet.colonyType)
            {
                SelectedPlanet.colonyType = (Planet.ColonyType)GovernorDropdown.ActiveValue;
                if (SelectedPlanet.colonyType != Planet.ColonyType.Colony)
                {
                    SelectedPlanet.FoodLocked = true;
                    SelectedPlanet.ProdLocked = true;
                    SelectedPlanet.ResLocked = true;
                    SelectedPlanet.GovernorOn = true;
                }
                else
                {
                    SelectedPlanet.GovernorOn = false;
                    SelectedPlanet.FoodLocked = false;
                    SelectedPlanet.ProdLocked = false;
                    SelectedPlanet.ResLocked = false;
                }
            }
            foreach (ScrollList.Entry e in ColoniesList.VisibleEntries)
            {
                e.Get<EmpireScreenEntry>().SetNewPos(eRect.X + 22, e.Y);
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            EmpireScreen clickTimer = this;
            clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}