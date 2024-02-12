using System.Collections.ObjectModel;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public delegate void MouseEnterHandler();
public delegate void MouseExitHandler();
public delegate void MouseDownHandler();
public delegate void MouseUpHandler();
public delegate void ClickHandler();
public delegate void FocusGainedHandler();
public delegate void FocusLostHandler();
public delegate void SubmitHandler();
public delegate void DragStartHandler();
public delegate void DragEndHandler(bool accepted);
public delegate void DragEnterHandler(object data);
public delegate void DragExitHandler();
public delegate void DragDropHandler(object data);
public delegate void NavigationHandler(NavigationDirection dir);

public enum NavigationDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Enumeration of visual states for a widget
/// </summary>
[Flags]
public enum WidgetState
{
    Default = 0,
    Hovered = 1,
    Pressed = 2,
    Focused = 4,
    Checked = 8,
}

public static class WidgetStateExtensions
{
    public static bool HasFlags(this WidgetState state, WidgetState flags)
    {
        return ((int)state & (int)flags) != 0;
    }
}

/// <summary>
/// Base class for all UI widgets
/// </summary>
public class Widget
{
    private static Dictionary<string, Type> _widgetTypes = [];

    static Widget()
    {
        // build a list of types derived from Widget
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAssignableTo(typeof(Widget)))
                {
                    _widgetTypes.Add(type.Name, type);
                }
            }
        }
    }

    public static Widget Load(PraxisGame game, XmlNode node)
    {
        if (node.Name == "Template")
        {
            string src = node.Attributes!["src"]!.Value;
            Widget widget = Load(game, game.Resources.Load<XmlDocument>(src).Value.FirstChild!);
            widget.Deserialize(game, node);
            return widget;
        }
        else
        {
            Type widgetType = _widgetTypes[node.Name];
            Widget widget = (Activator.CreateInstance(widgetType) as Widget)!;
            widget.Deserialize(game, node);
            foreach (var child in node.ChildNodes.Cast<XmlNode>())
            {
                if (child.Name == "#comment") continue;

                Widget childWidget = Load(game, child);
                widget.AddWidget(childWidget);
            }
            return widget;
        }
    }

    public string? id;
    public HashSet<string> tags = [];
    public bool inheritVisualState = false;

    public Canvas? Root { get; private set; }
    public Widget? Parent { get; private set; }

    public WidgetState VisualState
    {
        get
        {
            if (inheritVisualState && Parent != null) return Parent.VisualState;

            WidgetState state = WidgetState.Default;
            if (_isPress) state |= WidgetState.Pressed;
            if (_isHover) state |= WidgetState.Hovered;
            if (_isFocus) state |= WidgetState.Focused;

            return state;
        }
    }

    public readonly Style WidgetStyle = new();

    public event MouseEnterHandler? OnMouseEnter;
    public event MouseExitHandler? OnMouseExit;
    public event MouseDownHandler? OnMouseDown;
    public event MouseUpHandler? OnMouseUp;
    public event ClickHandler? OnClick;
    public event FocusGainedHandler? OnFocusGained;
    public event FocusLostHandler? OnFocusLost;
    public event NavigationHandler? OnNavigate;
    public event DragStartHandler? OnDragStart;
    public event DragEndHandler? OnDragEnd;
    public event DragEnterHandler? OnDragEnter;
    public event DragExitHandler? OnDragExit;
    public event DragDropHandler? OnDragDrop;

    public Widget? NavigateUp;
    public Widget? NavigateDown;
    public Widget? NavigateLeft;
    public Widget? NavigateRight;

    public bool clipChildren;
    public bool interactive;
    public bool visible = true;

    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector2 offsetMin;
    public Vector2 offsetMax;
    public Vector2 pivot;
    public float rotation;

    public ReadOnlyCollection<Widget> Children => _childrenReadOnly;

    private bool _isHover = false;
    private bool _isFocus = false;
    private bool _isPress = false;

    internal Rectangle _cachedRect;
    private List<Widget> _children = new List<Widget>();
    private ReadOnlyCollection<Widget> _childrenReadOnly;

    public Widget()
    {
        _childrenReadOnly = _children.AsReadOnly();
    }

    public Widget? FindById(string id)
    {
        if (this.id == id) return this;

        foreach (var child in Children)
        {
            if (child.FindById(id) is Widget w)
            {
                return w;
            }
        }

        return null;
    }

    public Widget? FindWithTag(string tag)
    {
        if (tags.Contains(tag)) return this;

        foreach (var child in Children)
        {
            return child.FindWithTag(tag);
        }

        return null;
    }

    public void FindAllWithTag(string tag, List<Widget> results)
    {
        if (tags.Contains(tag)) results.Add(this);

        foreach (var child in Children)
        {
            child.FindAllWithTag(tag, results);
        }
    }

    public virtual void Deserialize(PraxisGame game, XmlNode node)
    {
        if (node.Attributes?["id"] is XmlAttribute id)
        {
            this.id = id.Value;
        }

        if (node.Attributes?["tags"] is XmlAttribute tags)
        {
            foreach (var tag in tags.Value.Split(','))
            {
                this.tags.Add(tag.Trim());
            }
        }

        if (node.Attributes?["anchorMin"] is XmlAttribute anchorMin)
        {
            this.anchorMin = JsonVector2Converter.Parse(anchorMin.Value);
        }

        if (node.Attributes?["anchorMax"] is XmlAttribute anchorMax)
        {
            this.anchorMax = JsonVector2Converter.Parse(anchorMax.Value);
        }

        if (node.Attributes?["anchoredPosition"] is XmlAttribute anchoredPosition)
        {
            this.anchoredPosition = JsonVector2Converter.Parse(anchoredPosition.Value);
        }

        if (node.Attributes?["sizeDelta"] is XmlAttribute sizeDelta)
        {
            this.sizeDelta = JsonVector2Converter.Parse(sizeDelta.Value);
        }

        if (node.Attributes?["offsetMin"] is XmlAttribute offsetMin)
        {
            this.offsetMin = JsonVector2Converter.Parse(offsetMin.Value);
        }

        if (node.Attributes?["offsetMax"] is XmlAttribute offsetMax)
        {
            this.offsetMax = JsonVector2Converter.Parse(offsetMax.Value);
        }

        if (node.Attributes?["pivot"] is XmlAttribute pivot)
        {
            this.pivot = JsonVector2Converter.Parse(pivot.Value);
        }

        if (node.Attributes?["rotation"] is XmlAttribute rotation)
        {
            this.rotation = float.Parse(rotation.Value);
        }
    }

    public virtual void Update(UIRenderer renderer, float deltaTime)
    {
        if (Parent == null) throw new InvalidOperationException();

        // calculate anchor rect
        Vector2 min = anchorMin * new Vector2(Parent._cachedRect.Width, Parent._cachedRect.Height);
        Vector2 max = anchorMax * new Vector2(Parent._cachedRect.Width, Parent._cachedRect.Height);

        // offset
        min += offsetMin;
        max += offsetMax;

        // final pos
        Vector2 pos = min + anchoredPosition;
        Vector2 size = max - min + sizeDelta;

        // final rect
        _cachedRect.X = (int)pos.X;
        _cachedRect.Y = (int)pos.Y;
        _cachedRect.Width = (int)size.X;
        _cachedRect.Height = (int)size.Y;

        foreach (var child in _children)
        {
            child.Update(renderer, deltaTime);
        }
    }

    private void UpdateStyle(Stylesheet sheet)
    {
        WidgetStyle.Clear();
        sheet.Apply(this);
        OnStyleUpdated();
    }

    protected virtual void Draw(UIRenderer renderer, Rectangle rect)
    {
        if (clipChildren)
        {
            renderer.PushClipRect(rect);
        }

        // draw children
        foreach (var child in _children)
        {
            child.DrawInternal(renderer);
        }

        if (clipChildren)
        {
            renderer.PopClipRect();
        }
    }

    internal void DrawInternal(UIRenderer renderer)
    {
        if (!visible) return;
        
        // calculate transform
        Matrix transform = CreateTransform();

        Rectangle drawRect = _cachedRect;
        drawRect.X = 0;
        drawRect.Y = 0;

        // push new transform
        renderer.PushMatrix(transform);

        // draw self
        Draw(renderer, drawRect);

        // pop transform
        renderer.PopMatrix();
    }

    public void AddWidget(Widget widget)
    {
        if (widget.Parent != null || widget.Parent == this) throw new InvalidOperationException();

        _children.Add(widget);
        widget.Parent = this;
        widget.UpdateRoot();
    }

    public void RemoveWidget(Widget widget)
    {
        if (widget.Parent == null || widget.Parent != this) throw new InvalidOperationException();

        _children.Remove(widget);
        widget.Parent = null;
        widget.UpdateRoot();
    }

    public Widget? GetWidgetAtPos(Vector2 pos)
    {
        return GetWidgetAtPosInternal(pos, GetParentTransform());
    }

    public virtual void HandleMouseEnter()
    {
        _isHover = true;
        UpdateStyle();
        OnMouseEnter?.Invoke();
    }

    public virtual void HandleMouseExit()
    {
        _isHover = false;
        UpdateStyle();
        OnMouseExit?.Invoke();
    }

    public virtual void HandleMouseDown()
    {
        _isPress = true;
        UpdateStyle();
        OnMouseDown?.Invoke();
    }

    public virtual void HandleMouseUp()
    {
        _isPress = false;
        UpdateStyle();
        OnMouseUp?.Invoke();
    }

    public virtual void HandleClick()
    {
        OnClick?.Invoke();
    }

    public virtual void HandleFocusGained()
    {
        _isFocus = true;
        UpdateStyle();
        OnFocusGained?.Invoke();
    }

    public virtual void HandleFocusLost()
    {
        _isFocus = false;
        UpdateStyle();
        OnFocusLost?.Invoke();
    }

    public virtual void HandleDragStart()
    {
        OnDragStart?.Invoke();
    }
    
    public virtual void HandleDragEnd(bool accepted)
    {
        OnDragEnd?.Invoke(accepted);
    }
    
    public virtual void HandleDragEnter(object data)
    {
        OnDragEnter?.Invoke(data);
    }
    
    public virtual void HandleDragExit()
    {
        OnDragExit?.Invoke();
    }
    
    public virtual void HandleDragDrop(object data)
    {
        OnDragDrop?.Invoke(data);
    }

    public virtual void HandleNavigation(NavigationDirection direction)
    {
        switch (direction)
        {
            case NavigationDirection.Up:
                if (NavigateUp != null)
                {
                    Root?.SetFocus(NavigateUp);
                }
                break;
            case NavigationDirection.Down:
                if (NavigateDown != null)
                {
                    Root?.SetFocus(NavigateDown);
                }
                break;
            case NavigationDirection.Left:
                if (NavigateLeft != null)
                {
                    Root?.SetFocus(NavigateLeft);
                }
                break;
            case NavigationDirection.Right:
                if (NavigateRight != null)
                {
                    Root?.SetFocus(NavigateRight);
                }
                break;
        }

        OnNavigate?.Invoke(direction);
    }

    public void UpdateStyle()
    {
        UpdateStyle(Root!.Styles.Value);
    }

    protected virtual void OnStyleUpdated()
    {
    }

    private void UpdateRoot()
    {
        Root = GetRoot() as Canvas;
        foreach (var child in Children)
        {
            child.UpdateRoot();
        }
        if (Root != null)
        {
            UpdateStyle(Root.Styles.Value);
        }
    }

    private Widget GetRoot()
    {
        if (Parent != null) return Parent.GetRoot();
        return this;
    }

    private Matrix CreateTransform()
    {
        float px = pivot.X * _cachedRect.Width;
        float py = pivot.Y * _cachedRect.Height;

        Matrix transform = Matrix.CreateTranslation(-px, -py, 0f);
        transform *= Matrix.CreateRotationZ(MathHelper.ToRadians(rotation));
        transform *= Matrix.CreateTranslation(_cachedRect.X, _cachedRect.Y, 0f);

        return transform;
    }

    private Matrix GetParentTransform()
    {
        return Parent?.GetWorldTransform() ?? Matrix.Identity;
    }

    private Matrix GetWorldTransform()
    {
        Matrix transform = CreateTransform();
        if (Parent != null)
        {
            transform *= Parent.GetWorldTransform();
        }

        return transform;
    }

    internal Widget? GetWidgetAtPosInternal(Vector2 pos, Matrix curTransform)
    {
        curTransform = CreateTransform() * curTransform;

        bool testSelf = false;

        // check pos against ourselves
        Matrix worldToLocal = Matrix.Invert(curTransform);
        Vector2 localPos = Vector2.Transform(pos, worldToLocal);

        if (localPos.X >= 0f && localPos.X <= _cachedRect.Width &&
            localPos.Y >= 0f && localPos.Y <= _cachedRect.Height)
        {
            testSelf = true;
        }

        // if we clip children, don't bother testing if cursor lies outside of our bounds
        if (clipChildren && !testSelf)
        {
            return null;
        }

        Widget? atPos = null;
        foreach (var child in _children)
        {
            atPos = child.GetWidgetAtPosInternal(pos, curTransform) ?? atPos;
        }

        // if cursor doesn't reside within any children, but lies within our bounds, return self
        if (testSelf && interactive)
        {
            atPos ??= this;
        }

        return atPos;
    }
}
