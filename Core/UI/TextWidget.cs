using Microsoft.Xna.Framework;

namespace Praxis.Core;

public class TextWidget : Widget
{
    public Color tint = Color.White;
    public RuntimeResource<Font>? font;
    public int size = 16;
    public string text = "";

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);

        if (font != null && !string.IsNullOrEmpty(text))
        {
            renderer.DrawString(font.Value.Value, size, text, new Vector2(rect.X, rect.Y), tint);
        }
    }
}
