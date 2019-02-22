using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class TreeNode : Node
	{
		public SpriteFont TitleFont = Fonts.Visitor10;

		public NodeState nodeState;

		public ResearchScreenNew screen;

		public string TechName;

		public Rectangle TitleRect;

		public Rectangle BaseRect = new Rectangle(0, 0, 92, 98);
        public Vector2 RightPoint => new Vector2(BaseRect.Right - 25,
                                                 BaseRect.CenterY() - 10);

        public bool complete;

		private Rectangle IconRect;

		private Rectangle UnlocksRect;

		private Array<UnlockItem> Unlocks = new Array<UnlockItem>();

		private UnlocksGrid grid;

		private Rectangle progressRect;

		private float TitleWidth = 73f;

		private Vector2 CostPos;

		public TreeNode(Vector2 Position, TechEntry Tech, ResearchScreenNew screen)
		{
			if (GlobalStats.IsRussian || GlobalStats.IsPolish)
			{
				TitleFont = Fonts.Arial10;
			}
			this.screen = screen;
			tech = Tech;
			TechName = string.Concat(Localizer.Token(ResourceManager.TechTree[Tech.UID].NameIndex), ResourceManager.TechTree[Tech.UID].MaxLevel > 1 ? " " + RomanNumerals.ToRoman(Tech.Level) + "/" + RomanNumerals.ToRoman(ResourceManager.TechTree[Tech.UID].MaxLevel) : "");
			BaseRect.X = (int)Position.X;
			BaseRect.Y = (int)Position.Y;
			progressRect = new Rectangle(BaseRect.X + 14, BaseRect.Y + 21, 1, 34);
			int numUnlocks = 0;
            Technology techTemplate = ResourceManager.TechTree[tech.UID];

            for (int i = 0; i < techTemplate.ModulesUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.ModulesUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.ModulesUnlocked[i].Type == null || 
                    techTemplate.ModulesUnlocked[i].Type == EmpireManager.Player.AcquiredFrom(tech))
                {
                    UnlockItem unlock  = new UnlockItem();
                    unlock.module      = ResourceManager.GetModuleTemplate(techTemplate.ModulesUnlocked[i].ModuleUID);
                    unlock.privateName = Localizer.Token(unlock.module.NameIndex);
                    unlock.Description = Localizer.Token(unlock.module.DescriptionIndex);
                    unlock.Type        = UnlockType.SHIPMODULE;
                    Unlocks.Add(unlock);
                    numUnlocks++;
                }
			}
			for (int i = 0; i < techTemplate.BonusUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.BonusUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType ||
                    techTemplate.BonusUnlocked[i].Type == null ||
                    techTemplate.BonusUnlocked[i].Type == EmpireManager.Player.AcquiredFrom(tech))
                {
                    UnlockItem unlock = new UnlockItem
                    {
                        privateName = techTemplate.BonusUnlocked[i].Name,
                        Description = Localizer.Token(techTemplate.BonusUnlocked[i].BonusIndex),
                        Type = UnlockType.ADVANCE
                    };
                    numUnlocks++;
                    Unlocks.Add(unlock);
                }
			}
			for (int i = 0; i < techTemplate.BuildingsUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.BuildingsUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.BuildingsUnlocked[i].Type == null || 
                    techTemplate.BuildingsUnlocked[i].Type == EmpireManager.Player.AcquiredFrom(tech))
                {
                    UnlockItem unlock = new UnlockItem();
                    unlock.building = ResourceManager.BuildingsDict[techTemplate.BuildingsUnlocked[i].Name];
                    unlock.privateName = Localizer.Token(unlock.building.NameTranslationIndex);
                    unlock.Description = Localizer.Token(unlock.building.DescriptionIndex);
                    unlock.Type = UnlockType.BUILDING;
                    numUnlocks++;
                    Unlocks.Add(unlock);
                }
			}
			for (int i = 0; i < techTemplate.HullsUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
				if (techTemplate.HullsUnlocked[i].ShipType == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.HullsUnlocked[i].ShipType == null || 
                    techTemplate.HullsUnlocked[i].ShipType == EmpireManager.Player.AcquiredFrom(tech))
				{
					UnlockItem unlock = new UnlockItem
					{
						HullUnlocked = techTemplate.HullsUnlocked[i].Name,
						privateName = techTemplate.HullsUnlocked[i].Name,
						Description = "",
						Type = UnlockType.HULL
					};
					numUnlocks++;
					Unlocks.Add(unlock);
				}
			}
			for (int i = 0; i < techTemplate.TroopsUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.TroopsUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.TroopsUnlocked[i].Type == "ALL" || 
                    techTemplate.TroopsUnlocked[i].Type == null || 
                    techTemplate.TroopsUnlocked[i].Type == EmpireManager.Player.AcquiredFrom(tech))
				{
					UnlockItem unlock = new UnlockItem();
					unlock.troop       = ResourceManager.GetTroopTemplate(techTemplate.TroopsUnlocked[i].Name);
					unlock.privateName = techTemplate.TroopsUnlocked[i].Name;
					unlock.Description = unlock.troop.Description;
                    unlock.Type        = UnlockType.TROOP;
					numUnlocks++;
					Unlocks.Add(unlock);
				}
			}
			int numColumns = numUnlocks / 2 + numUnlocks % 2;
			IconRect = new Rectangle(BaseRect.X + BaseRect.Width / 2 - 29, BaseRect.Y + BaseRect.Height / 2 - 24 - 10, 58, 49);
			if (numUnlocks <= 1)
			{
				UnlocksRect = new Rectangle(IconRect.X + IconRect.Width, IconRect.Y + IconRect.Height - 5, 35, 32);

                UnlocksRect.Y = UnlocksRect.Y - UnlocksRect.Height;
				
				Rectangle drawRect = UnlocksRect;
				drawRect.X = drawRect.X + 3;
				grid = new UnlocksGrid(Unlocks, drawRect);
			}
			else
			{
                UnlocksRect = new Rectangle(IconRect.X + IconRect.Width, IconRect.Y + IconRect.Height - 5, 13 + numColumns * 32, (numUnlocks == 1 ? 32 : 64));
				
					UnlocksRect.Y = UnlocksRect.Y - UnlocksRect.Height;
				
				Rectangle drawRect = UnlocksRect;
				drawRect.X = drawRect.X + 13;
				grid = new UnlocksGrid(Unlocks, drawRect);
			}
			UnlocksRect.X = UnlocksRect.X - 2;
			UnlocksRect.Width = UnlocksRect.Width + 4;
			UnlocksRect.Y = UnlocksRect.Y - 2;
			UnlocksRect.Height = UnlocksRect.Height + 4;
			TitleRect = new Rectangle(BaseRect.X + 8, BaseRect.Y - 15, 82, 29);
			if (GlobalStats.IsGermanOrPolish)
			{
				TitleRect.X = TitleRect.X - 5;
				TitleRect.Width = TitleRect.Width + 5;
				TreeNode titleWidth = this;
				titleWidth.TitleWidth = titleWidth.TitleWidth + 10f;
			}
			CostPos = new Vector2(65f, 70f) + new Vector2(BaseRect.X, BaseRect.Y);
			float x = CostPos.X;
			SpriteFont titleFont = TitleFont;                
			float cost = tech.TechCost;
			CostPos.X = x - titleFont.MeasureString(cost.String(1)).X;
			CostPos.X = (int)CostPos.X;
			CostPos.Y = (int)CostPos.Y - 3;
		}

        SubTexture TechIcon
        {
            get
            {
                string iconPath = tech.Tech.IconPath;
                if (iconPath == null)
                    return ResourceManager.Texture("TechIcons/" + tech.UID);
                return ResourceManager.TextureOrDefault("TechIcons/" + iconPath, "TechIcons/" + tech.UID);
            }
        }

        public void Draw(ScreenManager ScreenManager)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            if (complete)
            {
                DrawGlow(ScreenManager,tech.Tech.Secret ? Color.Green : Color.White );
            }
            switch (nodeState)
            {
                case NodeState.Normal:
                    bool active = complete || EmpireManager.Player.ResearchTopic == tech.UID || EmpireManager.Player.data.ResearchQueue.Contains(tech.UID);
                    spriteBatch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
                    spriteBatch.DrawRectangle(UnlocksRect, active ? new Color(34, 136, 200) : Color.Black);
                    grid.Draw(spriteBatch);
                    spriteBatch.Draw(active ? ResourceManager.Texture("ResearchMenu/tech_base_complete")
                                            : ResourceManager.Texture("ResearchMenu/tech_base"), BaseRect, Color.White);
                    //Added by McShooterz: Allows non root techs to use IconPath
                    spriteBatch.Draw(TechIcon, IconRect, Color.White);                    
                    spriteBatch.Draw(active ? ResourceManager.Texture("ResearchMenu/tech_base_title_complete") 
                                            : ResourceManager.Texture("ResearchMenu/tech_base_title"), TitleRect, Color.White);
                    string str1 = TitleFont.ParseText(TechName, TitleWidth);
                    string[] strArray1 = Regex.Split(str1, "\n");
                    Vector2 vector2_1 = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(str1).X / 2f, TitleRect.Y + 14 - TitleFont.MeasureString(str1).Y / 2f);
                    int num1 = 0;
                    foreach (string text in strArray1)
                    {
                        Vector2 position = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(text).X / 2f, vector2_1.Y + num1 * TitleFont.LineSpacing);
                        position = new Vector2((int)position.X, (int)position.Y);
                        spriteBatch.DrawString(TitleFont, text, position, complete ? new Color(132, 172, 208) : Color.White);
                        ++num1;
                    }
                    int num2 = (int)(progressRect.Height - EmpireManager.Player.TechProgress(tech) / EmpireManager.Player.TechCost(tech) * (double)progressRect.Height);
                    Rectangle destinationRectangle1 = progressRect;
                    destinationRectangle1.Height = num2;
                    spriteBatch.Draw(active ? ResourceManager.Texture("ResearchMenu/tech_progress") 
                                            : ResourceManager.Texture("ResearchMenu/tech_progress_inactive"), progressRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle1, Color.White);
                    break;
                case NodeState.Hover:
                    spriteBatch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
                    spriteBatch.DrawRectangle(UnlocksRect, new Color(190, 113, 25));
                    grid.Draw(spriteBatch);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_hover"), BaseRect, Color.White);
                    spriteBatch.Draw(TechIcon, IconRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_title_hover"), TitleRect, Color.White);
                    string str2 = TitleFont.ParseText(TechName, TitleWidth);
                    string[] strArray2 = Regex.Split(str2, "\n");
                    Vector2 vector2_2 = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(str2).X / 2f, TitleRect.Y + 14 - TitleFont.MeasureString(str2).Y / 2f);
                    int num3 = 0;
                    foreach (string text in strArray2)
                    {
                        Vector2 position = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(text).X / 2f, vector2_2.Y + num3 * TitleFont.LineSpacing);
                        position = new Vector2((int)position.X, (int)position.Y);
                        spriteBatch.DrawString(TitleFont, text, position, complete ? new Color(132, 172, 208) : Color.White);
                        ++num3;
                    }
                    int num4 = (int)(progressRect.Height - EmpireManager.Player.TechProgress(tech) / ResourceManager.Tech(tech.UID).ActualCost * (double)progressRect.Height);
                    Rectangle destinationRectangle2 = progressRect;
                    destinationRectangle2.Height = num4;
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress"), progressRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle2, Color.White);
                    break;
                case NodeState.Press:
                    spriteBatch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
                    spriteBatch.DrawRectangle(UnlocksRect, new Color(190, 113, 25));
                    grid.Draw(spriteBatch);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_hover"), BaseRect, Color.White);
                    spriteBatch.Draw(TechIcon, IconRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_title_hover"), TitleRect, Color.White);
                    string str3 = TitleFont.ParseText(TechName, TitleWidth);
                    string[] strArray3 = Regex.Split(str3, "\n");
                    Vector2 vector2_3 = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(str3).X / 2f, TitleRect.Y + 14 - TitleFont.MeasureString(str3).Y / 2f);
                    int num5 = 0;
                    foreach (string text in strArray3)
                    {
                        Vector2 position = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(text).X / 2f, vector2_3.Y + num5 * TitleFont.LineSpacing);
                        position = new Vector2((int)position.X, (int)position.Y);
                        spriteBatch.DrawString(TitleFont, text, position, complete ? new Color(163, 198, 236) : Color.White);
                        ++num5;
                    }
                    int num6 = (int)(progressRect.Height - EmpireManager.Player.TechProgress(tech) / EmpireManager.Player.TechCost(tech) * (double)progressRect.Height);
                    Rectangle destinationRectangle3 = progressRect;
                    destinationRectangle3.Height = num6;
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress"), progressRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle3, Color.White);
                    break;
            }
            spriteBatch.DrawString(TitleFont, ((float)(int)EmpireManager.Player.TechCost(tech)).String(1), CostPos, Color.White);
        }

		public void DrawGlow(ScreenManager ScreenManager) 
		{
            DrawGlow(ScreenManager, Color.White);
		}
	    public void DrawGlow(ScreenManager ScreenManager, Color color)
	    {
	        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
	        spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_base"), BaseRect, color);
	        spriteBatch.DrawRectangleGlow(TitleRect);
	        spriteBatch.DrawRectangleGlow(UnlocksRect);
        }

        public bool HandleInput(InputState input, ScreenManager ScreenManager, Camera2D camera)
		{
			Vector2 RectPos = camera.GetScreenSpaceFromWorldSpace(new Vector2(BaseRect.X, BaseRect.Y));
			Rectangle moddedRect = new Rectangle((int)RectPos.X, (int)RectPos.Y, BaseRect.Width, BaseRect.Height);
			Vector2 RectPos2 = camera.GetScreenSpaceFromWorldSpace(new Vector2(UnlocksRect.X, UnlocksRect.Y));
			Rectangle moddedRect2 = new Rectangle((int)RectPos2.X, (int)RectPos2.Y, UnlocksRect.Width, UnlocksRect.Height);
			Vector2 RectPos3 = camera.GetScreenSpaceFromWorldSpace(new Vector2(IconRect.X, IconRect.Y));
			Rectangle moddedRect3 = new Rectangle((int)RectPos3.X, (int)RectPos3.Y, IconRect.Width, IconRect.Height);
			if (moddedRect.HitTest(input.CursorPosition) || moddedRect2.HitTest(input.CursorPosition))
			{
				if (nodeState != NodeState.Hover)
				{
					GameAudio.MouseOver();
				}
				nodeState = NodeState.Hover;
				if (input.InGameSelect)
				{
					nodeState = NodeState.Press;
					return true;
				}
				if (input.RightMouseClick)
				{
					screen.RightClicked = true;
					ScreenManager.AddScreen(new ResearchPopup(Empire.Universe, tech.UID));
					return false;
				}
			}
			else
			{
				nodeState = NodeState.Normal;
			}
			if (!moddedRect3.HitTest(input.CursorPosition))
			{
				foreach (UnlocksGrid.GridItem gridItem in grid.GridOfUnlocks)
				{
					Vector2 RectPos4 = camera.GetScreenSpaceFromWorldSpace(new Vector2(gridItem.rect.X, gridItem.rect.Y));
					Rectangle moddedRect4 = new Rectangle((int)RectPos4.X, (int)RectPos4.Y, gridItem.rect.Width, gridItem.rect.Height);
					if (!moddedRect4.HitTest(input.CursorPosition))
					{
						continue;
					}
					string tip = string.Concat(gridItem.item.privateName, "\n\n", gridItem.item.Description);
					if (gridItem.item.HullUnlocked == null)
					{
						ToolTip.CreateTooltip(tip);
					}
					else
					{
                        ShipData unlocked = ResourceManager.Hull(gridItem.item.HullUnlocked);
                        ToolTip.CreateTooltip($"{unlocked.Name} ({Localizer.GetRole(unlocked.Role, EmpireManager.Player)})");
					}
				}
			}
			else
			{
				ToolTip.CreateTooltip($"Right Click to Expand \n\n{Localizer.Token(ResourceManager.TechTree[tech.UID].DescriptionIndex)}");
			}
			return false;
		}
	}
}