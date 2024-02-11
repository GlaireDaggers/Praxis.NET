using System.Xml;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public class ButtonWidget : Widget
{
    public string Text
    {
        get => _textWidget.Text;
        set
        {
            _textWidget.Text = value;
        }
    }

    private readonly TextWidget _textWidget = new()
    {
        inheritVisualState = true,
        tags = [ "button", "button-text" ],
        anchorMax = Vector2.One
    };

    private readonly ImageWidget _imageWidget = new()
    {
        inheritVisualState = true,
        tags = [ "button", "button-bg" ],
        anchorMax = Vector2.One
    };

    public ButtonWidget() : base()
    {
        tags = [ "input" ];
        AddWidget(_imageWidget);
        AddWidget(_textWidget);
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
        _textWidget.UpdateStyle();
        _imageWidget.UpdateStyle();
    }
}
