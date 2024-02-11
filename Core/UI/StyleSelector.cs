namespace Praxis.Core;

/// <summary>
/// Interface style selectors
/// </summary>
public interface IStyleSelector
{
    /// <summary>
    /// Return true if the given widget matches this selector
    /// </summary>
    bool MatchWidget(Widget widget);
}

/// <summary>
/// A selector which matches everything
/// </summary>
public class EverythingSelector : IStyleSelector
{
    public bool MatchWidget(Widget widget)
    {
        return true;
    }
}

/// <summary>
/// A selector which matches on widget ID
/// </summary>
public class IdSelector : IStyleSelector
{
    public string id;

    public IdSelector(string id)
    {
        this.id = id;
    }

    public bool MatchWidget(Widget widget)
    {
        return widget.id == id;
    }
}

/// <summary>
/// A selector which matches on widget tags
/// </summary>
public class TagSelector : IStyleSelector
{
    public string tag;

    public TagSelector(string tag)
    {
        this.tag = tag;
    }

    public bool MatchWidget(Widget widget)
    {
        return widget.tags.Contains(tag);
    }
}

/// <summary>
/// A selector which matches on widget visual state
/// </summary>
public class StateSelector : IStyleSelector
{
    public WidgetState state;

    public StateSelector(WidgetState state)
    {
        this.state = state;
    }

    public bool MatchWidget(Widget widget)
    {
        // return widget.VisualState.HasFlag(state);
        // Enum.HasFlag boxes. go figure.
        return ((int)widget.VisualState & (int)state) != 0;
    }
}

/// <summary>
/// A selector which matches any of a set of child selectors
/// </summary>
public class AnySelector : IStyleSelector
{
    public IStyleSelector[] selectors;

    public AnySelector(IStyleSelector[] selectors)
    {
        this.selectors = selectors;
    }

    public bool MatchWidget(Widget widget)
    {
        foreach (var selector in selectors)
        {
            if (selector.MatchWidget(widget))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// A selector which matches all of a set of child selectors
/// </summary>
public class AllSelector : IStyleSelector
{
    public IStyleSelector[] selectors;

    public AllSelector(IStyleSelector[] selectors)
    {
        this.selectors = selectors;
    }

    public bool MatchWidget(Widget widget)
    {
        foreach (var selector in selectors)
        {
            if (!selector.MatchWidget(widget))
            {
                return false;
            }
        }

        return true;
    }
}