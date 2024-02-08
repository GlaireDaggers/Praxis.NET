using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Praxis.Core;

/// <summary>
/// Container of style information which can be used to render UI controls
/// </summary>
public partial class Stylesheet
{
    private const string CSS_GROUPS = @"(?<selector>(?:(?:[^,{]+),?)*?)\{(?:(?<name>[^}:]+):?(?<value>[^};]+);?)*?\}";
    private const string CSS_COMMENTS = @"(?<!"")\/\*.+?\*\/(?!"")";
    private const string CSS_SELECTOR = @"(\*|[.#:]?[a-zA-Z0-9-_]*)";

    private static readonly Regex groupRegex = CssGroupRegex();
    private static readonly Regex commentRegex = CssCommentRegex();
    private static readonly Regex selectorRegex = CssSelectorRegex();

    [JsonPropertyName("styles")]
    public List<KeyValuePair<IStyleSelector, Style>> Styles { get; set; } = [];

    public void Apply(Widget widget)
    {
        foreach (var style in Styles)
        {
            if (style.Key.MatchWidget(widget))
            {
                widget.WidgetStyle.Apply(style.Value);
            }
        }
    }

    public void Read(PraxisGame game, string css)
    {
        if (!string.IsNullOrEmpty(css))
        {
            // remove comments, then parse remaining text
            MatchCollection matchList = groupRegex.Matches(commentRegex.Replace(css, string.Empty));

            foreach (Match item in matchList.Cast<Match>())
            {
                if (item != null && item.Groups != null && 
                    item.Groups["selector"] != null && 
                    item.Groups["selector"].Captures != null && 
                    item.Groups["selector"].Captures[0] != null && 
                    !string.IsNullOrEmpty(item.Groups["selector"].Value))
                {
                    List<IStyleSelector> styleSelectors = new List<IStyleSelector>();

                    string strSelector = item.Groups["selector"].Captures[0].Value.Trim();
                    var styleAttrs = new List<KeyValuePair<string,string>>();

                    string[] selectorCombine = strSelector.Split(',');
                    for (int i = 0; i < selectorCombine.Length; i++)
                    {
                        List<IStyleSelector> subSelectors = new List<IStyleSelector>();

                        MatchCollection selectors = selectorRegex.Matches(selectorCombine[i]);
                        foreach (Match selector in selectors.Cast<Match>())
                        {
                            if (!string.IsNullOrEmpty(selector.Value))
                            {
                                if (selector.Value == "*")
                                {
                                    subSelectors.Add(new EverythingSelector());
                                }
                                else if (selector.Value.StartsWith('.'))
                                {
                                    subSelectors.Add(new TagSelector(selector.Value[1..]));
                                }
                                else if (selector.Value.StartsWith('#'))
                                {
                                    subSelectors.Add(new IdSelector(selector.Value[1..]));
                                }
                                else if (selector.Value.StartsWith(':'))
                                {
                                    switch (selector.Value)
                                    {
                                        case ":default":
                                            subSelectors.Add(new StateSelector(WidgetState.Default));
                                            break;
                                        case ":hover":
                                            subSelectors.Add(new StateSelector(WidgetState.Hovered));
                                            break;
                                        case ":press":
                                            subSelectors.Add(new StateSelector(WidgetState.Pressed));
                                            break;
                                        case ":focus":
                                            subSelectors.Add(new StateSelector(WidgetState.Focused));
                                            break;
                                        default:
                                            Console.WriteLine("Unrecognized state selector: " + selector.Value);
                                            break;
                                    }
                                }
                                else
                                {
                                    subSelectors.Add(new TypeSelector(selector.Value));
                                }
                            }
                        }

                        styleSelectors.Add(new AllSelector(subSelectors.ToArray()));
                    }

                    for (int i = 0; i < item.Groups["name"].Captures.Count; i++)
                    {
                        string className = item.Groups["name"].Captures[i].Value.Trim();
                        string value = item.Groups["value"].Captures[i].Value.Trim();

                        if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(value))
                        {
                            styleAttrs.Add(new KeyValuePair<string, string>(className, value));
                        }
                    }

                    Style style = new();
                    style.Parse(game, styleAttrs);
                    
                    Styles.Add(new KeyValuePair<IStyleSelector, Style>(new AnySelector(styleSelectors.ToArray()), style));
                }
            }
        }
    }

    [GeneratedRegex(CSS_GROUPS, RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex CssGroupRegex();

    [GeneratedRegex(CSS_COMMENTS, RegexOptions.Compiled)]
    private static partial Regex CssCommentRegex();

    [GeneratedRegex(CSS_SELECTOR, RegexOptions.Compiled)]
    private static partial Regex CssSelectorRegex();
}
