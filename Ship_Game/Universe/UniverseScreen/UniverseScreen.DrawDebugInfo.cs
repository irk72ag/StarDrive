using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        // empire perf indicators
        readonly AggregatePerfTimer PreEmpirePerf   = new AggregatePerfTimer();
        readonly AggregatePerfTimer EmpireInfluPerf = new AggregatePerfTimer();
                        
        public readonly AggregatePerfTimer ResetBordersPerf  = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ScanInfluencePerf = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ThreatMatrixPerf  = new AggregatePerfTimer();

        readonly AggregatePerfTimer EmpireUpdatePerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer EmpireMiscPerf   = new AggregatePerfTimer();
        readonly AggregatePerfTimer PostEmpirePerf   = new AggregatePerfTimer();

        readonly AggregatePerfTimer TurnTimePerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer ProcessSimTurnsPerf = new AggregatePerfTimer();
        
        readonly AggregatePerfTimer DrawGroupTotalPerf = new AggregatePerfTimer();

        readonly AggregatePerfTimer RenderGroupTotalPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer BeginSunburnPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer BackdropPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer SunburnDrawPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawPlanetsPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawShieldsPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawParticles = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawExplosionsPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer EndSunburnPerf = new AggregatePerfTimer();
        
        readonly AggregatePerfTimer OverlaysGroupTotalPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawFogInfluence = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawBorders = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawFogOfWar = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawOverFog = new AggregatePerfTimer();

        
        readonly AggregatePerfTimer IconsGroupTotalPerf = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawProj = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawShips = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawIcons = new AggregatePerfTimer();
        readonly AggregatePerfTimer DrawUI = new AggregatePerfTimer();

        ScrollList2<DebugStatItem> DebugStats;

        void HideDebugGameInfo()
        {
            if (DebugStats != null)
            {
                DebugStats.RemoveFromParent();
                DebugStats = null;
            }
        }

        void ShowDebugGameInfo()
        {
            if (DebugStats == null)
            {
                DebugStats = Add(new ScrollList2<DebugStatItem>(220f, 40f, 400f, 600f, 20));
                DebugStats.EnableItemEvents = true;

                var uObjects = UState.Objects;
                DebugStats.AddItem(new DebugStatItem("Time",
                    () => $"real {GameBase.Base.TotalElapsed:0.00}s   sim.time {CurrentSimTime:0.00}s/{TargetSimTime:0.00}s  lag:{(TargetSimTime - CurrentSimTime) * 1000:0.0}ms"));
                DebugStats.AddItem(new DebugStatItem("Ships", () => uObjects.Ships.Count.ToString()));
                DebugStats.AddItem(new DebugStatItem("Proj", () => uObjects.Projectiles.Count.ToString()));
                DebugStats.AddItem(new DebugStatItem("DyLights", () => ScreenManager.ActiveDynamicLights.ToString()));
                DebugStats.AddItem(new DebugStatItem("Perf", () => "avg-sample  max-sample  total/sec"));

                var sim = DebugStats.AddItem(new DebugStatItem("Sim", ProcessSimTurnsPerf, true));
                sim.AddSubItem(new DebugStatItem("FPS", () => $"actual:{ActualSimFPS}  target:{CurrentSimFPS}"));
                sim.AddSubItem(new DebugStatItem("NumTurns", () => ProcessSimTurnsPerf.MeasuredSamples.ToString()));

                var turn = DebugStats.AddItem(new DebugStatItem("Turn", TurnTimePerf, true));
                turn.AddSubItem(new DebugStatItem("PreEmp", PreEmpirePerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("Empire", EmpireUpdatePerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("Influence", EmpireInfluPerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem(" ResetBorders", ResetBordersPerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem(" ScanInfluence", ScanInfluencePerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem(" ThreatMatrix", ThreatMatrixPerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("Objects", uObjects.TotalTime, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("Misc", EmpireMiscPerf, TurnTimePerf));
                turn.AddSubItem(new DebugStatItem("PostEmp", PostEmpirePerf, TurnTimePerf));

                var objects = DebugStats.AddItem(new DebugStatItem("Objects", uObjects.TotalTime, true));
                objects.AddSubItem(new DebugStatItem("List", uObjects.ListTime, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Systems", uObjects.SysPerf, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Ships", uObjects.ShipsPerf, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Projectiles", uObjects.ProjPerf, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Sensors", uObjects.SensorPerf, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Sensors", () => $"current:{uObjects.Scans} per/s:{uObjects.ScansPerSec}"));
                objects.AddSubItem(new DebugStatItem("Visibility", uObjects.VisPerf, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Spatial", UState.Spatial.UpdateTime, uObjects.TotalTime));
                objects.AddSubItem(new DebugStatItem("Collide", UState.Spatial.CollisionTime, uObjects.TotalTime));

                DebugStats.AddItem(new DebugStatItem("TotalDraw", DrawGroupTotalPerf, true));

                var render = DebugStats.AddItem(new DebugStatItem("Render", RenderGroupTotalPerf, true));
                render.AddSubItem(new DebugStatItem("Sunburn.Begin", BeginSunburnPerf, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Backdrop", BackdropPerf, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Sunburn.Draw", SunburnDrawPerf, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Planets", DrawPlanetsPerf, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Shields", DrawShieldsPerf, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Particles", DrawParticles, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Explosions", DrawShieldsPerf, RenderGroupTotalPerf));
                render.AddSubItem(new DebugStatItem("Sunburn.End", EndSunburnPerf, RenderGroupTotalPerf));

                var overlays = DebugStats.AddItem(new DebugStatItem("Overlays", OverlaysGroupTotalPerf, true));
                overlays.AddSubItem(new DebugStatItem("Influence", DrawFogInfluence, OverlaysGroupTotalPerf));
                overlays.AddSubItem(new DebugStatItem("Borders", DrawBorders, OverlaysGroupTotalPerf));
                overlays.AddSubItem(new DebugStatItem("FogOfWar", DrawFogOfWar, OverlaysGroupTotalPerf));
                overlays.AddSubItem(new DebugStatItem("OverFog", DrawOverFog, OverlaysGroupTotalPerf));

                var icons = DebugStats.AddItem(new DebugStatItem("Icons", IconsGroupTotalPerf, true));
                icons.AddSubItem(new DebugStatItem("Projectiles", DrawProj, IconsGroupTotalPerf));
                icons.AddSubItem(new DebugStatItem("ShipOveray", DrawShips, IconsGroupTotalPerf));
                icons.AddSubItem(new DebugStatItem("Icons", DrawIcons, IconsGroupTotalPerf));
                icons.AddSubItem(new DebugStatItem("UI", DrawUI, IconsGroupTotalPerf));
                DebugStats.Update(0f);
            }
        }

        class DebugStatItem : ScrollListItem<DebugStatItem>
        {
            string Title;
            const float TitleOffset = 100;
            readonly AggregatePerfTimer ThisTime;
            readonly AggregatePerfTimer MasterTime;
            readonly Func<string> DynamicText;
            string GetText(UILabel label)
            {
                 if (MasterTime != null)
                     return ThisTime.String(MasterTime);
                 if (ThisTime != null)
                     return ThisTime.ToString();
                 if (DynamicText != null)
                     return DynamicText();
                 return "";
            }
            public override int ItemHeight => IsHeader ? 40 : 16;
            public DebugStatItem(string title, AggregatePerfTimer perfTimer, bool isHeader)
                : base(null)
            {
                Title = title;
                HeaderMaxWidth = 800;
                ThisTime = perfTimer;
                Init(title, 0, TitleOffset);
            }
            public DebugStatItem(string title, Func<string> dynamicText)
            {
                DynamicText = dynamicText;
                Init(title, 0, TitleOffset);
            }
            public DebugStatItem(string title, AggregatePerfTimer perfTimer, AggregatePerfTimer master)
            {
                ThisTime = perfTimer;
                MasterTime = master;
                Init(title, 10, TitleOffset);
            }
            void Init(string title, float titleX, float valueX)
            {
                Title = title;
                UILabel lblTitle = Add(new UILabel(title));
                UILabel lblValue = Add(new UILabel(GetText));
                lblTitle.SetLocalPos(titleX, 0);
                lblValue.SetLocalPos(valueX, 0);
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                if (IsHeader)
                {
                    // dark blueish transparent background for Headers
                    var edgeColor = new Color(75, 99, 125, 100);
                    Color bkgColor = Hovered ? edgeColor : new Color(35, 59, 85, 50);
                    new Selector(Rect, bkgColor, edgeColor).Draw(batch, elapsed);
                }
                base.Draw(batch, elapsed);
            }
        }
    }
}
