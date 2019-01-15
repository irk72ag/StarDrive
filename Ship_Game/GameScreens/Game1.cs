using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Point = System.Drawing.Point;

namespace Ship_Game
{
    // This class is created only once during Program start
    public sealed class Game1 : Game
    {
        public GraphicsDeviceManager Graphics;
        public static Game1 Instance;
        public ScreenManager ScreenManager;
        public Viewport Viewport { get; private set; }
        public bool IsLoaded  { get; private set; }
        public bool IsExiting { get; private set; }

        public new GameContentManager Content { get; }
        public static GameContentManager GameContent => Instance?.Content;

        // This is equivalent to PresentationParameters.BackBufferWidth
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }
        public Vector2 ScreenArea { get; private set; }
        public Vector2 ScreenCenter { get; private set; }

        public float DeltaTime { get; private set; }
        public GameTime GameTime;

        public Game1()
        {
            // need to set base Content, to ensure proper content disposal
            base.Content = Content = new GameContentManager(Services, "Game");

            GlobalStats.LoadConfig();

        #if STEAM
            if (SteamManager.SteamInitialize())
            {
                SteamManager.RequestCurrentStats();
                if (SteamManager.SetAchievement("Thanks"))
                    SteamManager.SaveAllStatAndAchievementChanges();
            }
        #endif

            Exiting += GameExiting;

            string appData = Dir.StarDriveAppData;
            Directory.CreateDirectory(appData + "/Saved Games");
            Directory.CreateDirectory(appData + "/Saved Races");  // for saving custom races
            Directory.CreateDirectory(appData + "/Saved Setups"); // for saving new game setups
            Directory.CreateDirectory(appData + "/Fleet Designs");
            Directory.CreateDirectory(appData + "/Saved Designs");
            Directory.CreateDirectory(appData + "/WIP"); // huh????? @todo What's this for? CG:unfinished designs
            Directory.CreateDirectory(appData + "/Saved Games/Headers");
            Directory.CreateDirectory(appData + "/Saved Games/Fog Maps");

            Graphics = new GraphicsDeviceManager(this)
            {
                MinimumPixelShaderProfile = ShaderProfile.PS_2_0,
                MinimumVertexShaderProfile = ShaderProfile.VS_2_0,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferMultiSampling = false
            };
            Graphics.PreparingDeviceSettings += PrepareDeviceSettings;

            GraphicsSettings settings = GraphicsSettings.FromGlobalStats();
            DisplayMode currentMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            // check if resolution from graphics settings is ok:
            if (currentMode.Width < settings.Width || currentMode.Height < settings.Height)
            {
                settings.Width  = currentMode.Width;
                settings.Height = currentMode.Height;
            }

            ApplyGraphics(ref settings);

            var cursor = new Bitmap("Content/Cursors/Cursor.png", true);
            System.Drawing.Graphics.FromImage(cursor);
            Control.FromHandle(Window.Handle).Cursor = new Cursor(cursor.GetHicon());
            IsMouseVisible = true;
        }
        public void SetSteamAchievement(string name)
        {
            if (SteamManager.SteamInitialize())
            {
                if (SteamManager.SetAchievement(name))
                    SteamManager.SaveAllStatAndAchievementChanges();
            }
            else
            { Log.Warning("Steam not initialized"); }
        }
        private void GameExiting(object sender, EventArgs e)
        {
            IsExiting = true;
            ScreenManager.ExitAll(clear3DObjects:true);
        }

        protected override void Initialize()
        {
            Window.Title = "StarDrive BlackBox";
            ResourceManager.ScreenManager = ScreenManager = new ScreenManager(this, Graphics);
            GameAudio.Initialize("Content/Audio/ShipGameProject.xgs", "Content/Audio/Wave Bank.xwb", "Content/Audio/Sound Bank.xsb");

            Instance = this;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (IsLoaded)
                return;

            GraphicsSettings defaults = GraphicsSettings.FromGlobalStats();
            UpdateRendererPreferences(ref defaults);

            ScreenManager.LoadContent();
            Fonts.LoadContent(Content);
            ScreenManager.AddScreen(new GameLoadingScreen());
            IsLoaded = true;
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            GameTime = gameTime;
            DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GameAudio.Update();
            ScreenManager.Update(gameTime);
            base.Update(gameTime);

            if (ScreenManager.ScreenCount == 0)
                Instance.Exit();
        }


        protected override void Draw(GameTime gameTime)
        {
            if (GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
                return;

            GraphicsDevice.Clear(Color.Black);
            ScreenManager.Draw(gameTime);
            base.Draw(gameTime);
        }



        private void UpdateRendererPreferences(ref GraphicsSettings settings)
        {
            ScreenManager?.UpdatePreferences(new LightingSystemPreferences
            {
                ShadowQuality   = settings.ShadowQuality,
                MaxAnisotropy   = settings.MaxAnisotropy,
                ShadowDetail    = (DetailPreference) settings.ShadowDetail,
                EffectDetail    = (DetailPreference) settings.EffectDetail,
                TextureQuality  = (DetailPreference) settings.TextureQuality,
                TextureSampling = (SamplingPreference) settings.TextureSampling
            });
        }


        private void ApplySettings(ref GraphicsSettings settings)
        {
            Graphics.ApplyChanges();

            PresentationParameters p = GraphicsDevice.PresentationParameters;
            ScreenWidth  = p.BackBufferWidth;
            ScreenHeight = p.BackBufferHeight;
            ScreenArea   = new Vector2(ScreenWidth, ScreenHeight);
            ScreenCenter = new Vector2(ScreenWidth * 0.5f, ScreenHeight * 0.5f);
            Viewport     = GraphicsDevice.Viewport;

            UpdateRendererPreferences(ref settings);
            ScreenManager?.UpdateViewports();
        }


        private static void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            GraphicsAdapter a = e.GraphicsDeviceInformation.Adapter;
            PresentationParameters p = e.GraphicsDeviceInformation.PresentationParameters;

            var samples = (MultiSampleType)GlobalStats.AntiAlias;
            if (a.CheckDeviceMultiSampleType(DeviceType.Hardware, a.CurrentDisplayMode.Format,
                                             false, samples, out int quality))
            {
                p.MultiSampleQuality = (quality == 1 ? 0 : 1);
                p.MultiSampleType    = samples;
            }
            else
            {
                p.MultiSampleType    = MultiSampleType.None;
                p.MultiSampleQuality = 0;
            }

            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        }

        public void ApplyGraphics(ref GraphicsSettings settings)
        {
            if (settings.Width <= 0 || settings.Height <= 0)
            {
                settings.Width  = 800;
                settings.Height = 600;
            }
            var form = (Form)Control.FromHandle(Window.Handle);
            if (Debugger.IsAttached && settings.Mode == WindowMode.Fullscreen)
                settings.Mode = WindowMode.Borderless;

            Graphics.PreferredBackBufferWidth = settings.Width;
            Graphics.PreferredBackBufferHeight = settings.Height;
            Graphics.SynchronizeWithVerticalRetrace = true;

            switch (settings.Mode)
            {
                case WindowMode.Windowed:   form.FormBorderStyle = FormBorderStyle.Fixed3D; break;
                case WindowMode.Borderless: form.FormBorderStyle = FormBorderStyle.None;    break;
            }
            if (settings.Mode != WindowMode.Fullscreen && Graphics.IsFullScreen ||
                settings.Mode == WindowMode.Fullscreen && !Graphics.IsFullScreen)
            {
                Graphics.ToggleFullScreen();
            }

            ApplySettings(ref settings);

            if (settings.Mode != WindowMode.Fullscreen) // set to screen center
            {
                form.WindowState = FormWindowState.Normal;
                form.ClientSize = new Size(settings.Width, settings.Height);

                // set form to the center of the primary screen
                var size = Screen.PrimaryScreen.Bounds.Size;
                form.Location = new Point(
                    size.Width / 2 - settings.Width / 2,
                    size.Height / 2 - settings.Height / 2);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            GameAudio.Destroy();
            Instance = null;
            Log.Info("Game Instance Disposed");
        }
    }
}