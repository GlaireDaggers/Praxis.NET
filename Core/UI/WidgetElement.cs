using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

/// <summary>
/// Base class for a renderable element of a widget (text, images, etc)
/// </summary>
public abstract class WidgetElement
{
    public Color tint = Color.White;
    
    public abstract void Draw(Style style, UIRenderer renderer, Rectangle rect);
}

public class TextElement : WidgetElement
{
    public string Text
    {
        get => _rtl.Text;
        set
        {
            _rtl.Text = value;
        }
    }

    private readonly RichTextLayout _rtl = new();

    public override void Draw(Style style, UIRenderer renderer, Rectangle rect)
    {
        if (style.Font != null)
        {
            rect.X += style.Padding?.Left ?? 0;
            rect.Y += style.Padding?.Top ?? 0;
            rect.Width -= style.Padding?.Left ?? 0 + style.Padding?.Right ?? 0;
            rect.Height -= style.Padding?.Top ?? 0 + style.Padding?.Bottom ?? 0;

            _rtl.Font = style.Font.Value.Value.GetFont(style.Size ?? 16);
            _rtl.Width = style.WordWrap ?? true ? rect.Width : null;
            int textHeight = GetHeight();
            Vector2 pos = new Vector2(rect.X, rect.Y);
            switch (style.HorizontalAlign)
            {
                case TextHorizontalAlignment.Center:
                    pos.X += rect.Width / 2;
                    break;
                case TextHorizontalAlignment.Right:
                    pos.X += rect.Width;
                    break;
            }
            switch (style.VerticalAlign)
            {
                case TextVerticalAlignment.Center:
                    pos.Y += (rect.Height - textHeight) / 2;
                    break;
                case TextVerticalAlignment.Bottom:
                    pos.Y += rect.Height - textHeight;
                    break;
            }
            renderer.DrawString(_rtl, pos, MathUtils.Multiply(style.TextColor ?? Color.White, tint), style.HorizontalAlign ?? TextHorizontalAlignment.Left);
        }
    }

    private int GetHeight()
    {
        int height = 0;

        foreach (var line in _rtl.Lines)
        {
            height += line.Size.Y + _rtl.VerticalSpacing;
        }

        return height;
    }
}

public class ImageElement : WidgetElement
{
    public FillType fillType = FillType.None;
    public float fillAmount = 1f;

    public override void Draw(Style style, UIRenderer renderer, Rectangle rect)
    {
        if (style.Image != null)
        {
            rect.X += style.Padding?.Left ?? 0;
            rect.Y += style.Padding?.Top ?? 0;
            rect.Width -= style.Padding?.Left ?? 0 + style.Padding?.Right ?? 0;
            rect.Height -= style.Padding?.Top ?? 0 + style.Padding?.Bottom ?? 0;

            Texture2D img = style.Image.Value.Value;
            Rectangle srcRect = style.SourceRectangle ?? new Rectangle(0, 0, img.Width, img.Height);

            Color col = MathUtils.Multiply(style.ImageColor ?? Color.White, tint);

            switch (fillType)
            {
                case FillType.HorizontalLeft: {
                    Rectangle fillRect = rect;
                    fillRect.Width = (int)(fillRect.Width * fillAmount);
                    renderer.PushClipRect(fillRect);
                    break;
                }
                case FillType.HorizontalRight: {
                    Rectangle fillRect = rect;
                    fillRect.Width = (int)(fillRect.Width * fillAmount);
                    fillRect.X += rect.Width - fillRect.Width;
                    renderer.PushClipRect(fillRect);
                    break;
                }
                case FillType.VerticalTop: {
                    Rectangle fillRect = rect;
                    fillRect.Height = (int)(fillRect.Height * fillAmount);
                    renderer.PushClipRect(fillRect);
                    break;
                }
                case FillType.VerticalBottom: {
                    Rectangle fillRect = rect;
                    fillRect.Height = (int)(fillRect.Height * fillAmount);
                    fillRect.Y += rect.Height - fillRect.Height;
                    renderer.PushClipRect(fillRect);
                    break;
                }
            }

            if (style.Slices is Margins s)
            {
                int srcCenterW = srcRect.Width - (s.Left + s.Right);
                int srcCenterH = srcRect.Height - (s.Top + s.Bottom);

                int dstCenterW = rect.Width - (s.Left + s.Right);
                int dstCenterH = rect.Height - (s.Top + s.Bottom);

                // top left
                renderer.Draw(img, new Rectangle(rect.X, rect.Y, s.Left, s.Top),
                    new Rectangle(srcRect.X, srcRect.Y, s.Left, s.Top), col);

                // top edge
                renderer.Draw(img, new Rectangle(rect.X + s.Left, rect.Y, dstCenterW, s.Top),
                    new Rectangle(srcRect.X + s.Left, srcRect.Y, srcCenterW, s.Top), col);

                // top right
                renderer.Draw(img, new Rectangle(rect.Right - s.Right, rect.Y, s.Right, s.Top),
                    new Rectangle(srcRect.Right - s.Right, srcRect.Y, s.Right, s.Top), col);

                // left edge
                renderer.Draw(img, new Rectangle(rect.X, rect.Y + s.Top, s.Left, dstCenterH),
                    new Rectangle(srcRect.X, srcRect.Y + s.Top, s.Left, srcCenterH), col);

                // center
                renderer.Draw(img, new Rectangle(rect.X + s.Left, rect.Y + s.Top, dstCenterW, dstCenterH),
                    new Rectangle(srcRect.X + s.Left, srcRect.Y + s.Top, srcCenterW, srcCenterH), col);

                // right edge
                renderer.Draw(img, new Rectangle(rect.Right - s.Right, rect.Y + s.Top, s.Right, dstCenterH),
                    new Rectangle(srcRect.Right - s.Right, srcRect.Y + s.Top, s.Right, srcCenterH), col);

                // bottom left
                renderer.Draw(img, new Rectangle(rect.X, rect.Bottom - s.Bottom, s.Left, s.Bottom),
                    new Rectangle(srcRect.X, srcRect.Bottom - s.Bottom, s.Left, s.Bottom), col);

                // bottom edge
                renderer.Draw(img, new Rectangle(rect.X + s.Left, rect.Bottom - s.Bottom, dstCenterW, s.Bottom),
                    new Rectangle(srcRect.X + s.Left, srcRect.Bottom - s.Bottom, srcCenterW, s.Bottom), col);

                // bottom right
                renderer.Draw(img, new Rectangle(rect.Right - s.Right, rect.Bottom - s.Bottom, s.Right, s.Bottom),
                    new Rectangle(srcRect.Right - s.Right, srcRect.Bottom - s.Bottom, s.Right, s.Bottom), col);
            }
            else
            {
                renderer.Draw(img, rect, style.SourceRectangle, col);
            }

            if (fillType != FillType.None)
            {
                renderer.PopClipRect();
            }
        }
    }
}