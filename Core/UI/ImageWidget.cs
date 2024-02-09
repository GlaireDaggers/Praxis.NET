using System.Xml;
using Microsoft.Xna.Framework;

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
    public FillType FillType
    {
        get => _imageElement.fillType;
        set
        {
            _imageElement.fillType = value;
        }
    }

    public float FillAmount
    {
        get => _imageElement.fillAmount;
        set
        {
            _imageElement.fillAmount = value;
        }
    }

    public Color Tint
    {
        get => _imageElement.tint;
        set
        {
            _imageElement.tint = value;
        }
    }

    private readonly ImageElement _imageElement = new();

    public override void Deserialize(PraxisGame game, XmlNode node)
    {
        base.Deserialize(game, node);

        if (node.Attributes?["fillType"] is XmlAttribute fillType)
        {
            switch (fillType.Value)
            {
                case "none":
                    FillType = FillType.None;
                    break;
                case "horizontalLeft":
                    FillType = FillType.HorizontalLeft;
                    break;
                case "horizontalRight":
                    FillType = FillType.HorizontalRight;
                    break;
                case "verticalTop":
                    FillType = FillType.VerticalTop;
                    break;
                case "verticalBottom":
                    FillType = FillType.VerticalBottom;
                    break;
                default:
                    Console.WriteLine("Unreognized fill type: " + fillType.Value);
                    break;
            }
        }

        if (node.Attributes?["fillAmount"] is XmlAttribute fillAmount)
        {
            FillAmount = float.Parse(fillAmount.Value);
        }

        if (node.Attributes?["tint"] is XmlAttribute tint)
        {
            Tint = JsonColorConverter.Parse(tint.Value);
        }
    }

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        base.Draw(renderer, rect);
        _imageElement.Draw(WidgetStyle, renderer, rect);
    }
}
