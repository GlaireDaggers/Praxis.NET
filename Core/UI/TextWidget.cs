using System.Xml;
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
        get => _textElement.Text;
        set
        {
            _textElement.Text = value;
        }
    }

    public Color Tint
    {
        get => _textElement.tint;
        set
        {
            _textElement.tint = value;
        }
    }

    private readonly TextElement _textElement = new();

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);
        _textElement.Draw(WidgetStyle, renderer, rect);
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
            Tint = JsonColorConverter.Parse(tint.Value);
        }
    }
}
