using FontStashSharp.RichText;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public class TextWidget : Widget
{
    public string Text
    {
        get => _rtl.Text;
        set
        {
            _rtl.Text = value;
        }
    }

    public Color tint = Color.White;
    public RuntimeResource<Font>? font;
    public int size = 16;
    public bool wordWrap = true;
    public TextHorizontalAlignment alignment = TextHorizontalAlignment.Left;

    private RichTextLayout _rtl = new RichTextLayout();

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);

        if (font != null)
        {
            _rtl.Font = font.Value.Value.GetFont(size);
            _rtl.Width = wordWrap ? rect.Width : null;
            Vector2 pos = new Vector2(rect.X, rect.Y);
            switch (alignment)
            {
                case TextHorizontalAlignment.Center:
                    pos.X += rect.Width / 2;
                    break;
                case TextHorizontalAlignment.Right:
                    pos.X += rect.Width;
                    break;
            }
            renderer.DrawString(_rtl, pos, tint, alignment);
        }
    }
}
