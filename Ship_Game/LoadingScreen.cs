using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	internal sealed class LoadingScreen : GameScreen
	{
		private readonly GameScreen[] ScreensToLoad;
		private readonly bool LoadingIsSlow;
		private bool OtherScreensAreGone;

		private LoadingScreen(bool loadingIsSlow, GameScreen[] screensToLoad)
		{
			ScreensToLoad = screensToLoad;
            LoadingIsSlow = loadingIsSlow;
			TransitionOnTime = TimeSpan.FromSeconds(0);
			TransitionOffTime = TimeSpan.FromSeconds(0);
		}

		public override void Draw(GameTime gameTime)
		{
			if (ScreenState == ScreenState.Active && ScreenManager.ScreenCount == 1)
			{
				OtherScreensAreGone = true;
			}
			if (LoadingIsSlow)
			{
				SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
				Viewport viewport       = ScreenManager.GraphicsDevice.Viewport;
				Vector2 viewportSize    = new Vector2(viewport.Width, viewport.Height);
				Vector2 textSize        = Fonts.Arial12Bold.MeasureString("Loading...");
				Vector2 textPosition    = (viewportSize - textSize) / 2f;
				Color color             = new Color(255, 255, 255, TransitionAlpha);
				spriteBatch.Begin();
				spriteBatch.DrawString(Fonts.Arial12Bold, "Loading...", textPosition, color);
				spriteBatch.End();
			}
		}

		public static void Load(ScreenManager screenManager, bool loadingIsSlow, params GameScreen[] screensToLoad)
		{
            screenManager.ExitAll();
			screenManager.AddScreen(new LoadingScreen(loadingIsSlow, screensToLoad));
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		    if (!OtherScreensAreGone)
                return;

		    ScreenManager.RemoveScreen(this);
		    foreach (GameScreen screen in ScreensToLoad)
		    {
		        if (screen != null) ScreenManager.AddScreen(screen);
		    }
		    Game1.Instance.ResetElapsedTime();
		}
	}
}