using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

public enum FillType
{
    None,
    HorizontalLeft,
    HorizontalRight,
    VerticalTop,
    VerticalBottom
}

public class ImageWidget : Widget
{
    public FillType fillType = FillType.None;
    public float fillAmount = 1f;
    public Color tint = Color.White;

    

    public override void Deserialize(PraxisGame game, XmlNode node)
    {
        base.Deserialize(game, node);

        if (node.Attributes?["fillType"] is XmlAttribute fillType)
        {
            switch (fillType.Value)
            {
                case "none":
                    this.fillType = FillType.None;
                    break;
                case "horizontalLeft":
                    this.fillType = FillType.HorizontalLeft;
                    break;
                case "horizontalRight":
                    this.fillType = FillType.HorizontalRight;
                    break;
                case "verticalTop":
                    this.fillType = FillType.VerticalTop;
                    break;
                case "verticalBottom":
                    this.fillType = FillType.VerticalBottom;
                    break;
                default:
                    Console.WriteLine("Unreognized fill type: " + fillType.Value);
                    break;
            }
        }

        if (node.Attributes?["fillAmount"] is XmlAttribute fillAmount)
        {
            this.fillAmount = float.Parse(fillAmount.Value);
        }

        if (node.Attributes?["tint"] is XmlAttribute tint)
        {
            this.tint = JsonColorConverter.Parse(tint.Value);
        }
    }

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);

        if (WidgetStyle.Image != null)
        {
            rect.X += WidgetStyle.Padding?.Left ?? 0;
            rect.Y += WidgetStyle.Padding?.Top ?? 0;
            rect.Width -= WidgetStyle.Padding?.Left ?? 0 + WidgetStyle.Padding?.Right ?? 0;
            rect.Height -= WidgetStyle.Padding?.Top ?? 0 + WidgetStyle.Padding?.Bottom ?? 0;

            Texture2D img = WidgetStyle.Image.Value.Value;
            Rectangle srcRect = WidgetStyle.SourceRectangle ?? new Rectangle(0, 0, img.Width, img.Height);

            Color col = MathUtils.Multiply(WidgetStyle.ImageColor ?? Color.White, tint);

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

            if (WidgetStyle.Slices is Margins s)
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
                renderer.Draw(img, rect, WidgetStyle.SourceRectangle, col);
            }

            if (fillType != FillType.None)
            {
                renderer.PopClipRect();
            }
        }
    }
}
