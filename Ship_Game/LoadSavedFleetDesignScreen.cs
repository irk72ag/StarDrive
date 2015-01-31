using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class LoadSavedFleetDesignScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		//private UniverseScreen screen;

		private FleetDesignScreen parentScreen;

		private List<UIButton> Buttons = new List<UIButton>();

		//private Submenu subSave;

		private Rectangle Window;

		private Menu1 SaveMenu;

		private Submenu NameSave;

		private Submenu AllSaves;

		private Vector2 TitlePosition;

		private Vector2 EnternamePos;

		private UITextEntry EnterNameArea;

		private ScrollList SavesSL;

		private UIButton Save;

		private Selector selector;

		private FileInfo activeFile;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float transitionElapsedTime;

		public LoadSavedFleetDesignScreen()
		{
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public LoadSavedFleetDesignScreen(FleetDesignScreen caller)
		{
			this.parentScreen = caller;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.SaveMenu.Draw();
			this.NameSave.Draw();
			this.AllSaves.Draw();
			Vector2 bCursor = new Vector2((float)(this.AllSaves.Menu.X + 20), (float)(this.AllSaves.Menu.Y + 20));
			for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.SavesSL.Entries[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (e.clickRectHover != 0)
				{
					bCursor.Y = (float)e.clickRect.Y;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/Wisp"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Path.GetFileNameWithoutExtension((e.item as FileInfo).Name), tCursor, Color.White);
					if (e.clickRect.Y == 0)
					{
					}
				}
				else
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/Wisp"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Path.GetFileNameWithoutExtension((e.item as FileInfo).Name), tCursor, Color.White);
					if (e.clickRect.Y == 0)
					{
					}
				}
			}
			this.SavesSL.Draw(base.ScreenManager.SpriteBatch);
			this.EnterNameArea.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : Color.Orange));
			foreach (UIButton b in this.Buttons)
			{
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}



		public override void HandleInput(InputState input)
		{
			this.selector = null;
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
					string text = b.Text;
					if (text == null || !(text == "Load"))
					{
						continue;
					}
					if (this.activeFile != null)
					{
						/*if (this.screen != null)  //always false
						{
							this.screen.ExitScreen();
						}*/
						XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
						FleetDesign data = (FleetDesign)serializer1.Deserialize(this.activeFile.OpenRead());
						this.parentScreen.LoadData(data);
					}
					else
					{
						AudioManager.PlayCue("UI_Misc20");
					}
					this.ExitScreen();
				}
			}
			foreach (ScrollList.Entry e in this.SavesSL.Entries)
			{
				if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					if (e.clickRectHover == 0)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
					this.activeFile = e.item as FileInfo;
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.EnterNameArea.Text = Path.GetFileNameWithoutExtension(this.activeFile.Name);
				}
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 350, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 700, 600);
			this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
			Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
			this.NameSave = new Submenu(base.ScreenManager, sub);
			this.NameSave.AddTab("Load Saved Game...");
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
			Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
			this.AllSaves = new Submenu(base.ScreenManager, scrollList);
			this.AllSaves.AddTab("Saved Fleets");
			this.SavesSL = new ScrollList(this.AllSaves);
            FileInfo[] filesFromDirectory;
			

            if (GlobalStats.ActiveMod != null && Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/FleetDesigns")))
            {
                filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/FleetDesigns"));
            }
            else
            {
                filesFromDirectory = HelperFunctions.GetFilesFromDirectory("Content/FleetDesigns");
            }

			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				bool OK = true;
				XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
				foreach (FleetDataNode node in ((FleetDesign)serializer1.Deserialize(FI.OpenRead())).Data)
				{
					if (EmpireManager.GetEmpireByName(this.parentScreen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(node.ShipName))
					{
						continue;
					}
					OK = false;
					break;
				}
				if (OK)
				{
					this.SavesSL.AddItem(FI);
				}
			}

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileInfo[] fileInfoArray = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Fleet Designs"));
			for (int j = 0; j < (int)fileInfoArray.Length; j++)
			{
				FileInfo FI = fileInfoArray[j];
				bool OK = true;
				XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
				foreach (FleetDataNode node in ((FleetDesign)serializer1.Deserialize(FI.OpenRead())).Data)
				{
					if (EmpireManager.GetEmpireByName(this.parentScreen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(node.ShipName))
					{
						continue;
					}
					OK = false;
					break;
				}
				if (OK)
				{
					this.SavesSL.AddItem(FI);
				}
			}
			this.EnternamePos = this.TitlePosition;
			this.EnterNameArea = new UITextEntry();
			
				this.EnterNameArea.Text = "";
                this.EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);
			
			this.Save = new UIButton()
			{
				Rect = new Rectangle(sub.X + sub.Width - 88, this.EnterNameArea.ClickableArea.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = "Load"
			};
			this.Buttons.Add(this.Save);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
			base.LoadContent();
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}