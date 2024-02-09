using System.Xml;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public class ButtonWidget : Widget
{
    public string Text
    {
        get => _textElement.Text;
        set
        {
            _textElement.Text = value;
        }
    }

    private readonly TextElement _textElement = new();
    private readonly ImageElement _imageElement = new();

    public ButtonWidget() : base()
    {
        interactive = true;
    }

    public override void Deserialize(PraxisGame game, XmlNode node)
    {
        base.Deserialize(game, node);

        if (node.Attributes?["text"] is XmlAttribute text)
        {
            Text = text.Value;
        }
    }

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);
        _imageElement.Draw(WidgetStyle, renderer, rect);
        _textElement.Draw(WidgetStyle, renderer, rect);
    }
}
