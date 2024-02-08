﻿using System.Xml;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public enum TextVerticalAlignment
{
    Top,
    Center,
    Bottom,
}

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

    private readonly RichTextLayout _rtl = new RichTextLayout();

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);

        if (WidgetStyle.Font != null)
        {
            rect.X += WidgetStyle.Padding?.Left ?? 0;
            rect.Y += WidgetStyle.Padding?.Top ?? 0;
            rect.Width -= WidgetStyle.Padding?.Left ?? 0 + WidgetStyle.Padding?.Right ?? 0;
            rect.Height -= WidgetStyle.Padding?.Top ?? 0 + WidgetStyle.Padding?.Bottom ?? 0;

            _rtl.Font = WidgetStyle.Font.Value.Value.GetFont(WidgetStyle.Size ?? 16);
            _rtl.Width = WidgetStyle.WordWrap ?? true ? rect.Width : null;
            int textHeight = GetHeight();
            Vector2 pos = new Vector2(rect.X, rect.Y);
            switch (WidgetStyle.HorizontalAlign)
            {
                case TextHorizontalAlignment.Center:
                    pos.X += rect.Width / 2;
                    break;
                case TextHorizontalAlignment.Right:
                    pos.X += rect.Width;
                    break;
            }
            switch (WidgetStyle.VerticalAlign)
            {
                case TextVerticalAlignment.Center:
                    pos.Y += (rect.Height - textHeight) / 2;
                    break;
                case TextVerticalAlignment.Bottom:
                    pos.Y += rect.Height - textHeight;
                    break;
            }
            renderer.DrawString(_rtl, pos, MathUtils.Multiply(WidgetStyle.TextColor ?? Color.White, tint), WidgetStyle.HorizontalAlign ?? TextHorizontalAlignment.Left);
        }
    }

    public override void Deserialize(PraxisGame game, XmlNode node)
    {
        base.Deserialize(game, node);

        if (node.Attributes?["text"] is XmlAttribute text)
        {
            Text = text.Value;
        }

        if (node.Attributes?["tint"] is XmlAttribute tint)
        {
            this.tint = JsonColorConverter.Parse(tint.Value);
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