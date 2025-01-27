﻿using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    // A simplified dummy game setup
    // for minimal testing
    public class GameDummy : GameBase
    {
        public SpriteBatch Batch => ScreenManager.SpriteBatch;

        public GameDummy(int width = 800, int height = 600, bool show = false)
        {
            GameAudio.DisableAudio(true);
            GraphicsSettings settings = GraphicsSettings.FromGlobalStats();
            settings.Width  = width;
            settings.Height = height;
            settings.Mode = WindowMode.Borderless;
            ApplyGraphics(settings);
            if (show) Form.Visible = true;
            IsMouseVisible = true;
        }

        public void Create()
        {
            var manager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
            manager?.CreateDevice();
            ScreenManager = new ScreenManager(this, Graphics);
            base.Initialize();
        }

        protected override void Update(float deltaTime)
        {
            UpdateGame(deltaTime);
        }

        protected override void Draw()
        {
            ScreenManager.Draw();
        }
    }
}
