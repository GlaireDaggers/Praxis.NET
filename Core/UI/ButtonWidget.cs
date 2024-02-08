using System.Xml;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public class ButtonWidget : Widget
{
    public string Text
    {
        get => _text.Text;
        set
        {
            _text.Text = value;
        }
    }

    private TextWidget _text;
    private ImageWidget _image;

    public ButtonWidget() : base()
    {
        _text = new TextWidget()
        {
            anchorMin = Vector2.Zero,
            anchorMax = Vector2.One
        };
        _image = new ImageWidget()
        {
            anchorMin = Vector2.Zero,
            anchorMax = Vector2.One
        };

        AddWidget(_image);
        AddWidget(_text);

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

    protected override void OnStyleUpdated()
    {
        base.OnStyleUpdated();
        _text.WidgetStyle.Apply(WidgetStyle);
        _image.WidgetStyle.Apply(WidgetStyle);
    }
}
