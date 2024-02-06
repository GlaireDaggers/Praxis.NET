using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public delegate void MouseEnterHandler();
public delegate void MouseExitHandler();
public delegate void MouseDownHandler();
public delegate void MouseUpHandler();
public delegate void ClickHandler();
public delegate void FocusGainedHandler();
public delegate void FocusLostHandler();

/// <summary>
/// Base class for all UI widgets
/// </summary>
public class Widget
{
    public string? id;

    public Widget? Root { get; private set; }
    public Widget? Parent { get; private set; }

    public event MouseEnterHandler? OnMouseEnter;
    public event MouseExitHandler? OnMouseExit;
    public event MouseDownHandler? OnMouseDown;
    public event MouseUpHandler? OnMouseUp;
    public event ClickHandler? OnClick;
    public event FocusGainedHandler? OnFocusGained;
    public event FocusLostHandler? OnFocusLost;

    public bool clipChildren;
    public bool interactive;
    
    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector2 offsetMin;
    public Vector2 offsetMax;
    public Vector2 pivot;
    public float rotation;

    public ReadOnlyCollection<Widget> Children => _childrenReadOnly;

    internal Rectangle _cachedRect;
    private List<Widget> _children = new List<Widget>();
    private ReadOnlyCollection<Widget> _childrenReadOnly;

    public Widget()
    {
        _childrenReadOnly = _children.AsReadOnly();
    }

    public virtual void UpdateLayout(UIRenderer renderer)
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
            child.UpdateLayout(renderer);
        }
    }

    protected virtual void Draw(UIRenderer renderer, Rectangle rect)
    {
    }

    internal void DrawInternal(UIRenderer renderer)
    {
        // calculate transform
        Matrix transform = CreateTransform();

        Rectangle drawRect = _cachedRect;
        drawRect.X = 0;
        drawRect.Y = 0;

        // push new transform
        renderer.PushMatrix(transform);

        // draw self
        Draw(renderer, drawRect);

        if (clipChildren)
        {
            renderer.PushClipRect(drawRect);
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

        // pop transform
        renderer.PopMatrix();
    }

    public void AddWidget(Widget widget)
    {
        if (widget.Parent != null || widget.Parent == this) throw new InvalidOperationException();

        _children.Add(widget);
        widget.Parent = this;
        widget.Root = GetRoot();
    }

    public void RemoveWidget(Widget widget)
    {
        if (widget.Parent == null || widget.Parent != this) throw new InvalidOperationException();

        _children.Remove(widget);
        widget.Parent = null;
        widget.Root = null;
    }

    public Widget? GetWidgetAtPos(Vector2 pos)
    {
        return GetWidgetAtPosInternal(pos, GetParentTransform());
    }

    public void HandleMouseEnter()
    {
        OnMouseEnter?.Invoke();
    }

    public void HandleMouseExit()
    {
        OnMouseExit?.Invoke();
    }

    public void HandleMouseDown()
    {
        OnMouseDown?.Invoke();
    }

    public void HandleMouseUp()
    {
        OnMouseUp?.Invoke();
    }

    public void HandleClick()
    {
        OnClick?.Invoke();
    }

    public void HandleFocusGained()
    {
        OnFocusGained?.Invoke();
    }

    public void HandleFocusLost()
    {
        OnFocusLost?.Invoke();
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
            atPos ??= child.GetWidgetAtPosInternal(pos, curTransform);
        }

        // if cursor doesn't reside within any children, but lies within our bounds, return self
        if (testSelf)
        {
            atPos ??= this;
        }

        return atPos;
    }
}
