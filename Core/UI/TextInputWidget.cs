using System.Xml;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public class TextInputWidget : Widget
{
    public string Text
    {
        get => _textWidget.Text;
        set
        {
            if (value.Contains('\n'))
            {
                _textWidget.Text = value.ReplaceLineEndings("");
            }
            else
            {
                _textWidget.Text = value;
            }

            _cursorPos = value.Length;
        }
    }

    private readonly TextWidget _textWidget = new()
    {
        inheritVisualState = true,
        tags = [ "input", "input-text" ],
        anchorMax = Vector2.One
    };

    private readonly ImageWidget _imageWidget = new()
    {
        inheritVisualState = true,
        tags = [ "input", "input-bg" ],
        anchorMax = Vector2.One
    };

    private int _cursorPos = 0;

    public TextInputWidget() : base()
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

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);

        Rectangle cursorRect = new Rectangle(rect.X, rect.Y, 1, rect.Height);
        cursorRect.X += _textWidget.WidgetStyle.Padding?.Left ?? 0;
        cursorRect.Y += _textWidget.WidgetStyle.Padding?.Top ?? 0;
        cursorRect.Height -= (_textWidget.WidgetStyle.Padding?.Top ?? 0) + (_textWidget.WidgetStyle.Padding?.Bottom ?? 0);

        renderer.DrawRect(cursorRect, _textWidget.WidgetStyle.TextColor ?? Color.White);
    }
}