using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

/// <summary>
/// Container of style properties used to draw a control
/// </summary>
public class Style
{
    public Margins? Padding { get; set; }
    public Color? ImageColor { get; set; }
    public Color? TextColor { get; set; }
    public RuntimeResource<Font>? Font { get; set; }
    public int? Size { get; set; }
    public bool? WordWrap { get; set; }
    public TextHorizontalAlignment? HorizontalAlign { get; set; }
    public TextVerticalAlignment? VerticalAlign { get; set; }
    public RuntimeResource<Texture2D>? Image { get; set; }
    public Rectangle? SourceRectangle { get; set; }
    public Margins? Slices { get; set; }

    public void Apply(Style other)
    {
        Padding = other.Padding ?? Padding;
        ImageColor = other.ImageColor ?? ImageColor;
        TextColor = other.TextColor ?? TextColor;
        Font = other.Font ?? Font;
        Size = other.Size ?? Size;
        WordWrap = other.WordWrap ?? WordWrap;
        HorizontalAlign = other.HorizontalAlign ?? HorizontalAlign;
        VerticalAlign = other.VerticalAlign ?? VerticalAlign;
        Image = other.Image ?? Image;
        SourceRectangle = other.SourceRectangle ?? SourceRectangle;
        Slices = other.Slices ?? Slices;
    }

    internal void Parse(PraxisGame game, List<KeyValuePair<string, string>> attributes)
    {
        foreach (var attr in attributes)
        {
            switch (attr.Key)
            {
                case "padding":
                    Padding = JsonMarginConverter.Parse(attr.Value);
                    break;
                case "imageColor":
                    ImageColor = JsonColorConverter.Parse(attr.Value);
                    break;
                case "textColor":
                    TextColor = JsonColorConverter.Parse(attr.Value);
                    break;
                case "font":
                    Font = game.Resources.Load<Font>(attr.Value);
                    break;
                case "size":
                    Size = int.Parse(attr.Value);
                    break;
                case "wordWrap":
                    WordWrap = bool.Parse(attr.Value);
                    break;
                case "horizontalAlign":
                    switch (attr.Value)
                    {
                        case "left":
                            HorizontalAlign = TextHorizontalAlignment.Left;
                            break;
                        case "center":
                            HorizontalAlign = TextHorizontalAlignment.Center;
                            break;
                        case "right":
                            HorizontalAlign = TextHorizontalAlignment.Right;
                            break;
                        default:
                            Console.WriteLine("Unrecognized horizontal align: " + attr.Value);
                            break;
                    }
                    break;
                case "verticalAlign":
                    switch (attr.Value)
                    {
                        case "top":
                            VerticalAlign = TextVerticalAlignment.Top;
                            break;
                        case "center":
                            VerticalAlign = TextVerticalAlignment.Center;
                            break;
                        case "bottom":
                            VerticalAlign = TextVerticalAlignment.Bottom;
                            break;
                        default:
                            Console.WriteLine("Unrecognized vertical align: " + attr.Value);
                            break;
                    }
                    break;
                case "image":
                    Image = game.Resources.Load<Texture2D>(attr.Value);
                    break;
                case "sourceRectangle":
                    SourceRectangle = JsonRectConverter.Parse(attr.Value);
                    break;
                case "slices":
                    Slices = JsonMarginConverter.Parse(attr.Value);
                    break;
                default:
                    Console.WriteLine("Unrecognized style attribute: " + attr.Key);
                    break;
            }
        }
    }
}