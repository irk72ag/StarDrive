using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    using static Math;

    public static class Primitives2D
    {
        static Texture2D Pixel;

        public static void BracketRectangle(this SpriteBatch spriteBatch, Rectangle rect, Color color, int bracketSize)
        {
            float x = rect.X;
            float y = rect.Y;
            float r = x + rect.Width;
            float b = y + rect.Height;
            float o = bracketSize;
            DrawLine(spriteBatch, new Vector2(x-1, y),   new Vector2(x+o-1, y), color);
            DrawLine(spriteBatch, new Vector2(x,   y+1), new Vector2(x, y+o),   color);
            DrawLine(spriteBatch, new Vector2(r-o, y),   new Vector2(r, y),     color);
            DrawLine(spriteBatch, new Vector2(r, y+1),   new Vector2(r, y+o),   color);
            DrawLine(spriteBatch, new Vector2(r-o+1, b), new Vector2(r+1, b),   color);
            DrawLine(spriteBatch, new Vector2(r, b),     new Vector2(r, b-o+1), color);
            DrawLine(spriteBatch, new Vector2(x, b),     new Vector2(x+o, b),   color);
            DrawLine(spriteBatch, new Vector2(x, b),     new Vector2(x, b-o+1), color);
        }

        // This is the [ ] selection rectangle you see when selecting planets and ships
        public static void BracketRectangle(this SpriteBatch spriteBatch, Vector2 pos, float radius, Color color)
        {
            Vector2 tl = pos + new Vector2(-(radius + 3f), -(radius + 3f));
            Vector2 bl = pos + new Vector2(-(radius + 3f), radius);
            Vector2 tr = pos + new Vector2(radius, -(radius + 3f));
            Vector2 br = pos + new Vector2(radius, radius);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_TR"), tr, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_TL"), tl, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_BR"), br, color);
            spriteBatch.Draw(ResourceManager.Texture("UI/bracket_BL"), bl, color);
        }

        static void CreateThePixel(SpriteBatch spriteBatch)
        {
            Pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Pixel.SetData(new []{ Color.White });
        }

        static bool IsIntersectingScreen(Vector2 a, Vector2 b)
        {
            return Min(a.X, b.X) < GlobalStats.XRES && 0 < Max(a.X, b.X)
                && Min(a.Y, b.Y) < GlobalStats.YRES && 0 < Max(a.Y, b.Y);
        }

        static bool IsIntersectingScreen(Vector2 pos, float radius)
        {
            return (pos.X-radius) < GlobalStats.XRES && 0 < (pos.X+radius)
                && (pos.Y-radius) < GlobalStats.YRES && 0 < (pos.Y+radius);
        }

        public static void DrawCircle(this SpriteBatch spriteBatch, Vector2 posOnScreen, float radius, Color color, float thickness = 1f)
        {
            double logarithmicReduction = Max(1.0, Log10(radius));
            int sides = (int)(radius / logarithmicReduction);

            spriteBatch.DrawCircle(posOnScreen, radius, sides, color, thickness);
        }

        /**
         * @param sides This will always be clamped within [3, 256]
         */
        public static void DrawCircle(this SpriteBatch spriteBatch, Vector2 posOnScreen, float radius, int sides, Color color, float thickness = 1f)
        {
            // intersection tests will eliminate up to 95% of all lines, leading to much faster performance
            if (!IsIntersectingScreen(posOnScreen, radius))
                return; // nothing to do here!

            sides = sides.Clamped(3, 256);
            float step = 6.28318530717959f / sides;

            var start = new Vector2(posOnScreen.X + radius, posOnScreen.Y); // 0 angle is horizontal right
            Vector2 previous = start;

            for (float theta = step; theta < 6.28318530717959f; theta += step)
            {
                var current = new Vector2(posOnScreen.X + radius * (float)Cos(theta), 
                    posOnScreen.Y + radius * (float)Sin(theta));
                DrawLine(spriteBatch, previous, current, color, thickness);
                previous = current;
            }
            DrawLine(spriteBatch, previous, start, color, thickness); // connect back to start
        }

        public static void DrawCapsule(this SpriteBatch spriteBatch, Capsule capsuleOnScreen,
                                       Color color, float thickness = 1f)
        {
            Vector2 start = capsuleOnScreen.Start;
            Vector2 end   = capsuleOnScreen.End;
            float radius  = capsuleOnScreen.Radius;
            Vector2 left = (end - start).LeftVector().Normalized() * radius;

            DrawLine(spriteBatch, start + left, end + left, color, thickness);
            DrawLine(spriteBatch, start - left, end - left, color, thickness);
            DrawCircle(spriteBatch, start, radius, color, thickness);
            DrawCircle(spriteBatch, end, radius, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawCircle(this SpriteBatch spriteBatch, float x, float y, float radius, Color color, float thickness = 1f)
            => spriteBatch.DrawCircle(new Vector2(x, y), radius, color, thickness);

        public static void DrawLine(this SpriteBatch batch, Vector2 point1, Vector2 point2, Color color, float thickness = 1f)
        {
            // intersection tests will eliminate up to 95% of all lines, leading to much faster rendering performance
            if (!IsIntersectingScreen(point1, point2))
                return;

            float distance = point1.Distance(point2);
            float angle = (float)Atan2(point2.Y - point1.Y, point2.X - point1.X);
            DrawLine(batch, point1, distance, angle, color, thickness);
        }

        static void DrawLine(this SpriteBatch batch, Vector2 point, float length, float angle, Color color, float thickness)
        {
            if (Pixel == null)
                CreateThePixel(batch);

            // some hack here - the 1px texture is rotated and scaled to proper width/height
            var scale = new Vector2(length, thickness);
            batch.Draw(Pixel, point, null, color, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness = 1f)
        {
            var tl = new Vector2(rect.X, rect.Y);
            var bl = new Vector2(rect.X, rect.Bottom);
            var tr = new Vector2(rect.Right, rect.Y);
            var br = new Vector2(rect.Right, rect.Bottom);
            DrawLine(spriteBatch, tl, tr, color, thickness); // ---- top
            DrawLine(spriteBatch, tr, br, color, thickness); //    | right
            DrawLine(spriteBatch, br, bl, color, thickness); // ____ bottom
            DrawLine(spriteBatch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch, Vector2 center, Vector2 size, float rotation, Color color, float thickness = 1f)
        {
            Vector2 halfSize = size * 0.5f;
            Vector2 tl = new Vector2(center.X - halfSize.X, center.Y - halfSize.Y).RotateAroundPoint(center, rotation);
            Vector2 tr = new Vector2(center.X + halfSize.X, center.Y - halfSize.Y).RotateAroundPoint(center, rotation);
            Vector2 br = new Vector2(center.X + halfSize.X, center.Y + halfSize.Y).RotateAroundPoint(center, rotation);
            Vector2 bl = new Vector2(center.X - halfSize.X, center.Y + halfSize.Y).RotateAroundPoint(center, rotation);
            DrawLine(spriteBatch, tl, tr, color, thickness); // ---- top
            DrawLine(spriteBatch, tr, br, color, thickness); //    | right
            DrawLine(spriteBatch, br, bl, color, thickness); // ____ bottom
            DrawLine(spriteBatch, bl, tl, color, thickness); // |    left
        }

        public static void DrawRectangleGlow(this SpriteBatch spriteBatch, Rectangle r)
        {
            r = new Rectangle(r.X - 13, r.Y - 12, r.Width + 25, r.Height + 25);
            var tl = new Rectangle(r.X, r.Y, 20, 20);
            var tr = new Rectangle(r.X + r.Width - 20, r.Y, 20, 20);
            var bl = new Rectangle(r.X, r.Y + r.Height - 20, 20, 20);
            var br = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 20, 20, 20);
            var ht = new Rectangle(tl.X + 20, tl.Y, r.Width - 40, 20);
            var hb = new Rectangle(tl.X + 20, bl.Y, r.Width - 40, 20);
            var vl = new Rectangle(tl.X, tl.Y + 20, 20, r.Height - 40);
            var vr = new Rectangle(tl.X + r.Width - 20, tl.Y + 20, 20, r.Height - 40);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_TL"), tl, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_TR"), tr, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_BL"), bl, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_container_corner_BR"), br, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_horiz_T"), ht, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_horiz_B"), hb, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_verti_L"), vl, Color.White);
            spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_verti_R"), vr, Color.White);
        }

        public static void DrawResearchLineHorizontal(this SpriteBatch spriteBatch, Vector2 leftPoint, Vector2 rightPoint, bool complete)
        {
            var r = new Rectangle((int)leftPoint.X + 5, (int)leftPoint.Y - 2, (int)Vector2.Distance(leftPoint, rightPoint) - 5, 5);
            var small = new Rectangle((int)leftPoint.X, (int)leftPoint.Y, 5, 1);
            FillRectangle(spriteBatch, small, (complete ? new Color(110, 171, 227) : new Color(194, 194, 194)));

            SubTexture texture = ResourceManager.Texture(complete
                               ? "ResearchMenu/grid_horiz_complete"
                               : "ResearchMenu/grid_horiz");
            spriteBatch.Draw(texture, r, Color.White);
        }

        public static void DrawResearchLineHorizontalGradient(this SpriteBatch spriteBatch, Vector2 left, Vector2 right, bool complete)
        {
            var r = new Rectangle((int)left.X + 5, (int)left.Y - 2, (int)Vector2.Distance(left, right) - 5, 5);
            var small = new Rectangle((int)left.X, (int)left.Y, 5, 1);
            FillRectangle(spriteBatch, small, (complete ? new Color(110, 171, 227) : new Color(194, 194, 194)));

            SubTexture texture = ResourceManager.Texture(complete
                               ? "ResearchMenu/grid_horiz_gradient_complete"
                               : "ResearchMenu/grid_horiz_gradient");
            spriteBatch.Draw(texture, r, Color.White);
        }

        public static void DrawResearchLineVertical(this SpriteBatch spriteBatch, Vector2 top, Vector2 bottom, bool complete)
        {
            if (top.Y > bottom.Y) // top must have lower Y
                Vectors.Swap(ref top, ref bottom);

            SubTexture texture = ResourceManager.Texture(complete
                               ? "ResearchMenu/grid_vert_complete"
                               : "ResearchMenu/grid_vert");
            
            var r = new Rectangle((int)top.X - 2, (int)top.Y, 5, (int)top.Distance(bottom));
            spriteBatch.Draw(texture, r, Color.White);
        }

        public static void FillRectangle(this SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            if (Pixel == null)
                CreateThePixel(spriteBatch);

            spriteBatch.Draw(Pixel, rect, color);
        }

        public static void FillRectangle(this SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color, float angle)
        {
            if (Pixel == null)
                CreateThePixel(spriteBatch);

            spriteBatch.Draw(Pixel, location, null, color, angle, Vector2.Zero, size, SpriteEffects.None, 0f);
        }
    }
}