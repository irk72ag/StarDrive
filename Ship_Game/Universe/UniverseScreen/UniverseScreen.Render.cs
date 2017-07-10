using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        private void RenderParticles()
        {
            this.beamflashes.SetCamera(this.view, this.projection);
            this.explosionParticles.SetCamera(this.view, this.projection);
            this.photonExplosionParticles.SetCamera(this.view, this.projection);
            this.explosionSmokeParticles.SetCamera(this.view, this.projection);
            this.projectileTrailParticles.SetCamera(this.view, this.projection);
            this.fireTrailParticles.SetCamera(this.view, this.projection);
            this.smokePlumeParticles.SetCamera(this.view, this.projection);
            this.fireParticles.SetCamera(this.view, this.projection);
            this.engineTrailParticles.SetCamera(this.view, this.projection);
            this.flameParticles.SetCamera(this.view, this.projection);
            this.sparks.SetCamera(this.view, this.projection);
            this.lightning.SetCamera(this.view, this.projection);
            this.flash.SetCamera(this.view, this.projection);
            this.star_particles.SetCamera(this.view, this.projection);
            this.neb_particles.SetCamera(this.view, this.projection);
        }

        private void RenderBackdrop()
        {
            bg.Draw(this, starfield);
            bg3d.Draw();

            ClickableShipsList.Clear();
            ScreenManager.SpriteBatch.Begin();
            var rect = new Rectangle(0, 0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);

            using (player.KnownShips.AcquireReadLock())
            {
                for (int i = 0; i < player.KnownShips.Count; i++)
                {
                    Ship ship = player.KnownShips[i];
                    if (ship != null && ship.Active &&
                        (viewState != UnivScreenState.GalaxyView || !ship.IsPlatform))
                    {
                        ProjectToScreenCoords(ship.Position, ship.Radius,
                            out Vector2 screenPos, out float screenRadius);

                        if (rect.HitTest(screenPos))
                        {
                            if (screenRadius < 7.0f) screenRadius = 7f;
                            ship.ScreenRadius = screenRadius;
                            ship.ScreenPosition = screenPos;
                            ClickableShipsList.Add(new ClickableShip
                            {
                                Radius      = screenRadius,
                                ScreenPos   = screenPos,
                                shipToClick = ship
                            });
                        }
                        else
                            ship.ScreenPosition = new Vector2(-1f, -1f);
                    }
                }
            }

            lock (GlobalStats.ClickableSystemsLock)
            {
                ClickPlanetList.Clear();
                ClickableSystems.Clear();
            }
            Texture2D Glow_Terran = ResourceManager.TextureDict["PlanetGlows/Glow_Terran"];
            Texture2D Glow_Red    = ResourceManager.TextureDict["PlanetGlows/Glow_Red"];
            Texture2D Glow_White  = ResourceManager.TextureDict["PlanetGlows/Glow_White"];
            Texture2D Glow_Aqua   = ResourceManager.TextureDict["PlanetGlows/Glow_Aqua"];
            Texture2D Glow_Orange = ResourceManager.TextureDict["PlanetGlows/Glow_Orange"];
            Texture2D sunTexture  = null;
            string sunPath = string.Empty;
            for (int index = 0; index < SolarSystemList.Count; index++)
            {
                SolarSystem solarSystem = SolarSystemList[index];
                Vector3 systemV3 = solarSystem.Position.ToVec3();
                if (Frustum.Contains(new BoundingSphere(systemV3, 100000f)) != ContainmentType.Disjoint)
                {
                    ProjectToScreenCoords(solarSystem.Position, 4500f, out Vector2 sysScreenPos,
                        out float sysScreenPosDisToRight);

                    lock (GlobalStats.ClickableSystemsLock)
                        ClickableSystems.Add(new UniverseScreen.ClickableSystem()
                        {
                            Radius = sysScreenPosDisToRight < 8f ? 8f : sysScreenPosDisToRight,
                            ScreenPos = sysScreenPos,
                            systemToClick = solarSystem
                        });
                    if (viewState <= UniverseScreen.UnivScreenState.SectorView)
                    {
                        for (int i = 0; i < solarSystem.PlanetList.Count; i++)
                        {
                            Planet planet = solarSystem.PlanetList[i];
                            if (solarSystem.Explored(EmpireManager.Player))
                            {
                                this.ProjectToScreenCoords(planet.Center, 2500f, planet.SO.WorldBoundingSphere.Radius,
                                    out Vector2 planetScreenPos, out float planetScreenRadius);
                                float scale = planetScreenRadius / 115f;

                                if (planet.planetType == 1 || planet.planetType == 11 ||
                                    (planet.planetType == 13 || planet.planetType == 21) ||
                                    (planet.planetType == 22 || planet.planetType == 25 ||
                                     (planet.planetType == 27 || planet.planetType == 29)))
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Terran, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                else if (planet.planetType == 5 || planet.planetType == 7 ||
                                         (planet.planetType == 8 || planet.planetType == 9) || planet.planetType == 23)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Red, planetScreenPos, new Rectangle?(),
                                        Color.White, 0.0f, new Vector2(128f, 128f), scale, SpriteEffects.None, 1f);
                                else if (planet.planetType == 17)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_White, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                else if (planet.planetType == 19)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Aqua, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                else if (planet.planetType == 14 || planet.planetType == 18)
                                    this.ScreenManager.SpriteBatch.Draw(Glow_Orange, planetScreenPos,
                                        new Rectangle?(), Color.White, 0.0f, new Vector2(128f, 128f), scale,
                                        SpriteEffects.None, 1f);
                                lock (GlobalStats.ClickableSystemsLock)
                                    this.ClickPlanetList.Add(new UniverseScreen.ClickablePlanets()
                                    {
                                        ScreenPos = planetScreenPos,
                                        Radius = planetScreenRadius < 8f ? 8f : planetScreenRadius,
                                        planetToClick = planet
                                    });
                            }
                        }
                    }
                    if (viewState < UniverseScreen.UnivScreenState.GalaxyView)
                    {
                        if (solarSystem.SunPath != sunPath)
                            sunTexture = ResourceManager.Texture("Suns/" + solarSystem.SunPath);
                        DrawTransparentModel(SunModel,
                            Matrix.CreateRotationZ(Zrotate) *
                            Matrix.CreateTranslation(solarSystem.Position.ToVec3()), sunTexture, 10.0f);
                        DrawTransparentModel(SunModel,
                            Matrix.CreateRotationZ((float) (-Zrotate / 2.0)) *
                            Matrix.CreateTranslation(solarSystem.Position.ToVec3()), sunTexture, 10.0f);
                        if (solarSystem.Explored(EmpireManager.Player))
                        {
                            for (int i = 0; i < solarSystem.PlanetList.Count; i++)
                            {
                                Planet planet = solarSystem.PlanetList[i];
                                Vector2 planetScreenPos = ProjectToScreenPosition(planet.Center, 2500f);
                                float planetOrbitRadius = sysScreenPos.Distance(planetScreenPos);
                                if (this.viewState > UniverseScreen.UnivScreenState.ShipView)
                                {
                                    DrawCircle(sysScreenPos, planetOrbitRadius, 100,
                                        new Color((byte) 50, (byte) 50, (byte) 50, (byte) 90), 3f);
                                    if (planet.Owner == null)
                                        this.DrawCircle(sysScreenPos, planetOrbitRadius, 100,
                                            new Color((byte) 50, (byte) 50, (byte) 50, (byte) 90), 3f);
                                    else
                                        this.DrawCircle(sysScreenPos, planetOrbitRadius, 100,
                                            new Color(planet.Owner.EmpireColor.R, planet.Owner.EmpireColor.G,
                                                planet.Owner.EmpireColor.B, (byte) 100), 3f);
                                }
                            }
                        }
                    }
                }
            }
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.AlphaBlendEnable = true;
            renderState.AlphaBlendOperation = BlendFunction.Add;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            renderState.DepthBufferWriteEnable = true;
            ScreenManager.SpriteBatch.End();
        }

        private void RenderGalaxyBackdrop()
        {
            this.bg.DrawGalaxyBackdrop(this, this.starfield);
            this.ScreenManager.SpriteBatch.Begin();
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = this.Viewport.Project(
                    new Vector3((float) ((double) index * (double) this.UniverseSize / 40.0), 0.0f, 0.0f), this.projection,
                    this.view, Matrix.Identity);
                Vector3 vector3_2 = this.Viewport.Project(
                    new Vector3((float) ((double) index * (double) this.UniverseSize / 40.0), this.UniverseSize, 0.0f),
                    this.projection, this.view, Matrix.Identity);
                this.ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_1.X, vector3_1.Y),
                    new Vector2(vector3_2.X, vector3_2.Y), new Color((byte) 211, (byte) 211, (byte) 211, (byte) 70));
            }
            for (int index = 0; index < 41; ++index)
            {
                Vector3 vector3_1 = this.Viewport.Project(
                    new Vector3(0.0f, (float) ((double) index * (double) this.UniverseSize / 40.0), 40f), this.projection,
                    this.view, Matrix.Identity);
                Vector3 vector3_2 = this.Viewport.Project(
                    new Vector3(this.UniverseSize, (float) ((double) index * (double) this.UniverseSize / 40.0), 0.0f),
                    this.projection, this.view, Matrix.Identity);
                this.ScreenManager.SpriteBatch.DrawLine(new Vector2(vector3_1.X, vector3_1.Y),
                    new Vector2(vector3_2.X, vector3_2.Y), new Color((byte) 211, (byte) 211, (byte) 211, (byte) 70));
            }
            this.ScreenManager.SpriteBatch.End();
        }

        private void RenderOverFog(GameTime gameTime)
        {
            Vector3 vector3_1 =
                Viewport.Project(Vector3.Zero, projection, view,
                    Matrix.Identity);
            Vector3 vector3_2 = Viewport.Project(
                new Vector3(UniverseSize, UniverseSize, 0.0f), projection, view, Matrix.Identity);
            Viewport.Project(new Vector3(UniverseSize / 2f, UniverseSize / 2f, 0.0f),
                projection, view, Matrix.Identity);
            Rectangle rectangle1 = new Rectangle((int) vector3_1.X, (int) vector3_1.Y,
                (int) vector3_2.X - (int) vector3_1.X, (int) vector3_2.Y - (int) vector3_1.Y);
            if (viewState >= UniverseScreen.UnivScreenState.SectorView)
            {
                float num = (float) ((double) byte.MaxValue * (double) CamHeight / 9000000.0);
                if ((double) num > (double) byte.MaxValue)
                    num = (float) byte.MaxValue;
                Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte) num);
                ScreenManager.SpriteBatch.End();
                ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
                Rectangle rectangle2 = new Rectangle(rectangle1.X, rectangle1.Y, rectangle1.Width / 2,
                    rectangle1.Height / 2);
                ScreenManager.SpriteBatch.End();
                ScreenManager.SpriteBatch.Begin();
            }
            foreach (SolarSystem solarSystem in UniverseScreen.SolarSystemList)
            {
                if (viewState >= UniverseScreen.UnivScreenState.SectorView)
                {
                    Vector3 vector3_3 = new Vector3(solarSystem.Position, 0.0f);
                    if (Frustum.Contains(vector3_3) != ContainmentType.Disjoint)
                    {
                        Vector3 vector3_4 =
                            Viewport.Project(vector3_3, projection, view,
                                Matrix.Identity);
                        Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
                        Vector3 vector3_5 =
                            Viewport.Project(
                                new Vector3(solarSystem.Position.PointOnCircle(90f, 25000f), 0.0f), projection,
                                view, Matrix.Identity);
                        float num2 = Vector2.Distance(new Vector2(vector3_5.X, vector3_5.Y), position);
                        Vector2 vector2 = new Vector2(position.X, position.Y);
                        if ((solarSystem.Explored(player) || Debug) && SelectedSystem != solarSystem)
                        {
                            if (Debug)
                            {
                                solarSystem.ExploredDict[player] = true;
                                foreach (Planet planet in solarSystem.PlanetList)
                                    planet.ExploredDict[player] = true;
                            }
                            Vector3 vector3_6 =
                                Viewport.Project(
                                    new Vector3(
                                        new Vector2(100000f * UniverseScreen.GameScaleStatic, 0.0f) +
                                        solarSystem.Position, 0.0f), projection, view, Matrix.Identity);
                            float radius = Vector2.Distance(new Vector2(vector3_6.X, vector3_6.Y), position);
                            if (viewState == UniverseScreen.UnivScreenState.SectorView)
                            {
                                vector2.Y += radius;
                                DrawCircle(new Vector2(vector3_4.X, vector3_4.Y), radius, 100,
                                    new Color((byte) 50, (byte) 50, (byte) 50, (byte) 90), 1f);
                            }
                            else
                                vector2.Y += num2;
                            vector2.X -= SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X / 2f;
                            Vector2 pos = Input.CursorPosition;

                            Array<Empire> owners = new Array<Empire>();
                            bool wellKnown = false;
                            
                            foreach (Empire e in solarSystem.OwnerList)
                            {
                                EmpireManager.Player.TryGetRelations(e, out Relationship ssRel);
                                wellKnown = Debug || EmpireManager.Player == e || ssRel.Treaty_Alliance;                                
                                if (wellKnown) break;
                                if (ssRel.Known) // (ssRel.Treaty_Alliance || ssRel.Treaty_Trade || ssRel.Treaty_OpenBorders))
                                    owners.Add(e);
                            }
     
                            if(wellKnown)
                            {
                                owners = solarSystem.OwnerList.ToArrayList();
                            }
                            
                            if (owners.Count == 0)
                            {
                                if (SelectedSystem != solarSystem ||
                                    viewState < UniverseScreen.UnivScreenState.GalaxyView)
                                    ScreenManager.SpriteBatch.DrawString(SystemInfoUIElement.SysFont,
                                        solarSystem.Name, vector2, Color.Gray);
                                int num3 = 0;
                                --vector2.Y;
                                vector2.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X + 6f;
                                bool flag = false;
                                foreach (Planet planet in solarSystem.PlanetList)
                                {
                                    if (planet.ExploredDict[player])
                                    {
                                        for (int index = 0; index < planet.BuildingList.Count; ++index)
                                        {
                                            if (!string.IsNullOrEmpty(planet.BuildingList[index].EventTriggerUID))
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (flag)
                                            break;
                                    }
                                }
                                if (flag)
                                {
                                    vector2.Y -= 2f;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                        (byte) (Math.Abs((float) Math.Sin(gameTime.TotalGameTime.TotalSeconds)) *
                                                (float) byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y, 15, 15);
                                    ScreenManager.SpriteBatch.Draw(
                                        ResourceManager.TextureDict["UI/icon_anomaly_small"], rectangle2, color);
                                    if (rectangle2.HitTest(pos))
                                        ToolTip.CreateTooltip(138);
                                    ++num3;
                                }
                                TimeSpan totalGameTime;
                                if (solarSystem.CombatInSystem)
                                {
                                    vector2.X += (float) (num3 * 20);
                                    vector2.Y -= 2f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                        (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                                (float) byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    ScreenManager.SpriteBatch.Draw(
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"], rectangle2, color);
                                    if (rectangle2.HitTest(pos))
                                        ToolTip.CreateTooltip(122);
                                    ++num3;
                                }
                                if (solarSystem.DangerTimer > 0f)
                                {
                                    if (num3 == 1 || num3 == 2)
                                        vector2.X += 20f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                        (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                                (float) byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    ScreenManager.SpriteBatch.Draw(
                                        ResourceManager.TextureDict["Ground_UI/EnemyHere"], rectangle2, color);
                                    if (rectangle2.HitTest(pos))
                                        ToolTip.CreateTooltip(123);
                                }
                            }
                            else
                            {
                                int num3 = 0;
                                if (owners.Count == 1)
                                {
                                    if (SelectedSystem != solarSystem ||
                                        viewState < UnivScreenState.GalaxyView)
                                        HelperFunctions.DrawDropShadowText(ScreenManager, solarSystem.Name,
                                            vector2, SystemInfoUIElement.SysFont,
                                            owners.ToList()[0].EmpireColor);
                                }
                                else if (SelectedSystem != solarSystem ||
                                         viewState < UnivScreenState.GalaxyView)
                                {
                                    Vector2 Pos = vector2;
                                    int length = solarSystem.Name.Length;
                                    int num4 = length / owners.Count;
                                    int index1 = 0;
                                    for (int index2 = 0; index2 < length; ++index2)
                                    {
                                        if (index2 + 1 > num4 + num4 * index1)
                                            ++index1;
                                        HelperFunctions.DrawDropShadowText(ScreenManager,
                                            solarSystem.Name[index2].ToString(), Pos, SystemInfoUIElement.SysFont,
                                            owners.Count > index1
                                                ? owners.ToList()[index1].EmpireColor
                                                : (owners).Last()
                                                    .EmpireColor);
                                        Pos.X += SystemInfoUIElement.SysFont
                                            .MeasureString(solarSystem.Name[index2].ToString())
                                            .X;
                                    }
                                }
                                --vector2.Y;
                                vector2.X += SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X + 6f;
                                bool flag = false;
                                foreach (Planet planet in solarSystem.PlanetList)
                                {
                                    if (planet.ExploredDict[player])
                                    {
                                        for (int index = 0; index < planet.BuildingList.Count; ++index)
                                        {
                                            if (!string.IsNullOrEmpty(planet.BuildingList[index].EventTriggerUID))
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (flag)
                                            break;
                                    }
                                }
                                if (flag)
                                {
                                    vector2.Y -= 2f;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                        (byte) (Math.Abs((float) Math.Sin(gameTime.TotalGameTime.TotalSeconds)) *
                                                (float) byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y, 15, 15);
                                    ScreenManager.SpriteBatch.Draw(
                                        ResourceManager.TextureDict["UI/icon_anomaly_small"], rectangle2, color);
                                    if (rectangle2.HitTest(pos))
                                        ToolTip.CreateTooltip(138);
                                    ++num3;
                                }
                                TimeSpan totalGameTime;
                                if (solarSystem.CombatInSystem)
                                {
                                    vector2.X += (float) (num3 * 20);
                                    vector2.Y -= 2f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                        (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                                (float) byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    ScreenManager.SpriteBatch.Draw(
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"], rectangle2, color);
                                    if (rectangle2.HitTest(pos))
                                        ToolTip.CreateTooltip(122);
                                    ++num3;
                                }
                                if ((double) solarSystem.DangerTimer > 0.0)
                                {
                                    if (num3 == 1 || num3 == 2)
                                        vector2.X += 20f;
                                    totalGameTime = gameTime.TotalGameTime;
                                    Color color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                        (byte) (Math.Abs((float) Math.Sin(totalGameTime.TotalSeconds)) *
                                                (float) byte.MaxValue));
                                    Rectangle rectangle2 = new Rectangle((int) vector2.X, (int) vector2.Y,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width,
                                        ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
                                    ScreenManager.SpriteBatch.Draw(
                                        ResourceManager.TextureDict["Ground_UI/EnemyHere"], rectangle2, color);
                                    if (rectangle2.HitTest(pos))
                                        ToolTip.CreateTooltip(123);
                                }
                            }
                        }
                        else
                            vector2.X -= SystemInfoUIElement.SysFont.MeasureString(solarSystem.Name).X / 2f;
                    }
                }
                if (viewState >= UniverseScreen.UnivScreenState.GalaxyView)
                {
                    float scale = 0.05f;
                    Vector3 vector3_3 = new Vector3(solarSystem.Position, 0.0f);
                    Vector3 vector3_4 =
                        Viewport.Project(vector3_3, projection, view,
                            Matrix.Identity);
                    Vector2 position = new Vector2(vector3_4.X, vector3_4.Y);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath],
                        position, new Rectangle?(), Color.White, Zrotate,
                        new Vector2((float) (ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Width / 2),
                            (float) (ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Height / 2)), scale,
                        SpriteEffects.None, 0.9f);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Suns/" + solarSystem.SunPath],
                        position, new Rectangle?(), Color.White, (float) (-(double) Zrotate / 2.0),
                        new Vector2((float) (ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Width / 2),
                            (float) (ResourceManager.TextureDict["Suns/" + solarSystem.SunPath].Height / 2)), scale,
                        SpriteEffects.None, 0.9f);
                }
            }
        }

        private void RenderThrusters()
        {
            if (viewState > UnivScreenState.ShipView)
                return;
            for (int i = 0; i < player.KnownShips.Count; ++i)
            {
                Ship ship = player.KnownShips[i];
                if (ship != null && ship.InFrustum && ship.inSensorRange)
                {
                    ship.RenderThrusters(ref view, ref projection);
                }
            }
        }

        public void Render(GameTime gameTime)
        {
            if (Frustum == null)
                Frustum = new BoundingFrustum(view * projection);
            else
                Frustum.Matrix = view * projection;

            ScreenManager.BeginFrameRendering(gameTime, ref view, ref projection);

            RenderBackdrop();
            ScreenManager.SpriteBatch.Begin();
            if (DefiningAO && Input.LeftMouseDown)
            {
                DrawRectangleProjected(AORect, Color.Orange);
            }
            if (DefiningAO && SelectedShip != null)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Localizer.Token(1411),
                    new Vector2((float) SelectedStuffRect.X,
                        (float) (SelectedStuffRect.Y - Fonts.Pirulen16.LineSpacing - 2)), Color.White);
                for (int index = 0; index < this.SelectedShip.AreaOfOperation.Count; index++)
                {
                    Rectangle rectangle = this.SelectedShip.AreaOfOperation[index];
                    this.DrawRectangleProjected(rectangle, Color.Red, new Color(Color.Red, 50));
                }
            }
            else
                DefiningAO = false;
            float num = (float) (150.0 * SelectedSomethingTimer / 3f);
            if (num < 0f)
                num = 0.0f;
            byte alpha = (byte) num;
            if (alpha > 0)
            {
                if (SelectedShip != null)
                {
                    DrawShipLines(SelectedShip, alpha);
                }
                else if (SelectedShipList.Count > 0)
                {
                    for (int index1 = 0; index1 < this.SelectedShipList.Count; ++index1)
                    {
                        try
                        {
                            Ship ship = this.SelectedShipList[index1];
                            DrawShipLines(ship, alpha);
                        }
                        catch
                        {
                            Log.Warning("DrawShipLines Blew Up");
                        }
                    }
                }
            }
            this.ScreenManager.SpriteBatch.End();
            this.DrawBombs();
            ScreenManager.RenderSceneObjects();

            if (viewState < UnivScreenState.SectorView)
            {
                using (player.KnownShips.AcquireReadLock())
                    for (int j = player.KnownShips.Count - 1; j >= 0; j--)
                    {
                        Ship ship = player.KnownShips[j];
                        if (!ship.InFrustum) continue;

                        var renderProj = ship.Projectiles;
                        for (int i = renderProj.Count - 1; i >= 0; i--)
                        {
                            //I am thinking this is very bad but im not sure. is it faster than a lock? whats the right way to handle this.
                            Projectile projectile = renderProj[i];
                            if (projectile.Weapon.IsRepairDrone && projectile.DroneAI != null)
                            {
                                for (int k = 0; k < projectile.DroneAI.Beams.Count; ++k)
                                    projectile.DroneAI.Beams[k].Draw(ScreenManager);
                            }
                        }

                        for (int i = ship.Beams.Count - 1; i >= 0; --i) // regular FOR to mitigate multi-threading issues
                        {
                            Beam beam = ship.Beams[i];
                            if (beam.Source.InRadius(beam.ActualHitDestination, beam.Range + 10.0f))
                                beam.Draw(ScreenManager);
                            else
                                beam.Die(null, true);
                        }

                    }
            }

            var renderState = ScreenManager.GraphicsDevice.RenderState;
            renderState.DepthBufferWriteEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            foreach (Anomaly anomaly in anomalyManager.AnomaliesList)
                anomaly.Draw();
            if (viewState < UnivScreenState.SectorView)
            {
                for (int i = 0; i < SolarSystemList.Count; i++)
                {
                    SolarSystem solarSystem = SolarSystemList[i];
                    if (!solarSystem.Explored(player)) continue;

                    for (int j = 0; j < solarSystem.PlanetList.Count; j++)
                    {
                        Planet p = solarSystem.PlanetList[j];
                        if (Frustum.Contains(p.SO.WorldBoundingSphere) != ContainmentType.Disjoint)
                        {
                            if (p.hasEarthLikeClouds)
                            {
                                DrawClouds(xnaPlanetModel, p.cloudMatrix, view, projection, p);
                                DrawAtmo(xnaPlanetModel, p.cloudMatrix, view, projection, p);
                            }
                            if (p.hasRings)
                                DrawRings(p.RingWorld, view, projection, p.scale);
                        }
                    }
                }
            }
            renderState.AlphaBlendEnable = true;
            renderState.SourceBlend = Blend.SourceAlpha;
            renderState.DestinationBlend = Blend.One;
            renderState.DepthBufferWriteEnable = false;
            renderState.CullMode = CullMode.None;
            if (viewState < UnivScreenState.SectorView)
            {
                RenderThrusters();
                RenderParticles();

                FTLManager.DrawFTLModels(this);
                lock (GlobalStats.ExplosionLocker)
                {
                    for (int i = 0; i < MuzzleFlashManager.FlashList.Count; i++)
                    {
                        MuzzleFlash f = MuzzleFlashManager.FlashList[i];
                        DrawTransparentModel(MuzzleFlashManager.flashModel, f.WorldMatrix, MuzzleFlashManager.FlashTexture, f.scale);
                    }
                    MuzzleFlashManager.FlashList.ApplyPendingRemovals();
                }
                beamflashes.Draw(gameTime);
                explosionParticles.Draw(gameTime);
                photonExplosionParticles.Draw(gameTime);
                explosionSmokeParticles.Draw(gameTime);
                projectileTrailParticles.Draw(gameTime);
                fireTrailParticles.Draw(gameTime);
                smokePlumeParticles.Draw(gameTime);
                fireParticles.Draw(gameTime);
                engineTrailParticles.Draw(gameTime);
                star_particles.Draw(gameTime);
                neb_particles.Draw(gameTime);
                flameParticles.Draw(gameTime);
                sparks.Draw(gameTime);
                lightning.Draw(gameTime);
                flash.Draw(gameTime);
            }
            if (!Paused) // Particle pools need to be updated
            {
                beamflashes.Update(gameTime);
                explosionParticles.Update(gameTime);
                photonExplosionParticles.Update(gameTime);
                explosionSmokeParticles.Update(gameTime);
                projectileTrailParticles.Update(gameTime);
                fireTrailParticles.Update(gameTime);
                smokePlumeParticles.Update(gameTime);
                fireParticles.Update(gameTime);
                engineTrailParticles.Update(gameTime);
                star_particles.Update(gameTime);
                neb_particles.Update(gameTime);
                flameParticles.Update(gameTime);
                sparks.Update(gameTime);
                lightning.Update(gameTime);
                flash.Update(gameTime);
            }
            ScreenManager.EndFrameRendering();
            if (viewState < UnivScreenState.SectorView)
                DrawShields();
            renderState.DepthBufferWriteEnable = true;
        }
    }
}