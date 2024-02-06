using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

public class SpriteWidget : Widget
{
    public Color tint = Color.White;
    public RuntimeResource<Texture2D>? image;
    public Rectangle? sourceRectangle;

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);

        if (image != null)
        {
            renderer.Draw(image.Value.Value, rect, sourceRectangle, tint);
        }
    }
}
