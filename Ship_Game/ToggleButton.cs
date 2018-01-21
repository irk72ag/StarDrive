using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class ToggleButton : UIElementV2
    {
        public object ReferenceObject;

        public string Action = "";

        public bool Active;

        public bool Hover;

        public int WhichToolTip;

        public bool HasToolTip;

        public Color BaseColor = Color.White;

        private bool Pressed;

        private string HoverText;

        private readonly Texture2D PressTexture;
        private readonly Texture2D HoverTexture;
        private readonly Texture2D ActiveTexture;
        private readonly Texture2D InactiveTexture;
        private readonly Texture2D IconTexture;
        private readonly Vector2 WordPos;
        private readonly string IconPath;
        private readonly Texture2D IconActive;
        private readonly Rectangle IconRect;

        public delegate void ClickHandler(ToggleButton button);
        public event ClickHandler OnClick;

        public ToggleButton(Rectangle r, string activePath, string inactivePath, string hoverPath, string pressPath, string iconPath, UIElementV2 container = null) : base(container, r)
        {           
            Rect            = r;
            PressTexture    = ResourceManager.Texture(pressPath);
            HoverTexture    = ResourceManager.Texture(hoverPath);
            ActiveTexture   = ResourceManager.Texture(activePath);
            InactiveTexture = ResourceManager.Texture(inactivePath);                        
            IconTexture     = ResourceManager.Texture(iconPath,"");
            IconActive      = ResourceManager.Texture(string.Concat(iconPath, "_active"), "");
            

            if (IconTexture == null)
            {
                IconPath = iconPath;
                WordPos = new Vector2(Rect.X + 12 - Fonts.Arial12Bold.MeasureString(IconPath).X / 2f, Rect.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2);             
            }
            else
                IconRect = new Rectangle(Rect.X + Rect.Width / 2 - IconTexture.Width / 2
                    , Rect.Y + Rect.Height / 2 - IconTexture.Height / 2
                    , IconTexture.Width, IconTexture.Height);
        }

        //hack... until this is all straightend out to allow override of base draw.
        public void Draw(ScreenManager screenManager)            => Draw(screenManager.SpriteBatch);
        public void DrawIconResized(ScreenManager screenManager) => Draw(screenManager.SpriteBatch, true);
        public override void Draw(SpriteBatch spriteBatch)       => Draw(spriteBatch, false);
        

        public void Draw(SpriteBatch spriteBatch, bool resizeIcon, ScreenManager screenManager = null)
        {
            Rectangle iconRect = IconActive == null ? IconRect : Rect;
            
            if (Pressed)
                spriteBatch.Draw(PressTexture, Rect, Color.White);
            else if (Hover)
            {
                spriteBatch.Draw(HoverTexture, Rect, Color.White);                
            }
            else if (Active)
                spriteBatch.Draw(ActiveTexture, Rect, Color.White);
            else if (!Active)
                spriteBatch.Draw(InactiveTexture, Rect, Color.White);
            if (IconTexture == null)
            {
                if (Active)
                {
                    spriteBatch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, Color.White);
                    return;
                }

                spriteBatch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, Color.Gray);
            }
            else
                spriteBatch.Draw(IconActive ?? IconTexture, iconRect, Color.White);            
        }

        public override void PerformLegacyLayout(Vector2 pos)
        {
            Pos = pos;
        }

        public override bool HandleInput(InputState input)
        {
            Pressed = false;
            if (!Rect.HitTest(input.CursorPosition))
            {
                if (Hover)
                {
                    if (ToolTip.TipTimer > 3)
                    {
                        ToolTip.LastWhich = 0;
                        ToolTip.TextLast = string.Empty;
                    }

                    ToolTip.TipTimer = 0;                    
                }
                Hover = false;                
                return false;
            }
            if (!Hover)
            {
                GameAudio.MiniMapMouseOver();
                if (WhichToolTip != 0)
                    ToolTip.CreateTooltip(WhichToolTip);
            }
            Hover = true;
            
            if (input.LeftMouseClick)
            {
                OnClick?.Invoke(this);
                Pressed = true;
            }
            return input.InGameSelect;
        }
    }
}