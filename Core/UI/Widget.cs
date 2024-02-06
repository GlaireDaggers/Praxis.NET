using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

/// <summary>
/// Base class for all UI widgets
/// </summary>
public class Widget
{
    public Widget? Parent { get; private set; }

    public bool clipChildren;

    public Dimension left;
    public Dimension top;
    public Dimension width;
    public Dimension height;
    public Dimension pivotX = Dimension.Percent(0.5f);
    public Dimension pivotY = Dimension.Percent(0.5f);
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

        // calculate dimensions
        _cachedRect.X = (int)left.Calculate(Parent._cachedRect.Width);
        _cachedRect.Y = (int)top.Calculate(Parent._cachedRect.Height);
        _cachedRect.Width = (int)width.Calculate(Parent._cachedRect.Width);
        _cachedRect.Height = (int)height.Calculate(Parent._cachedRect.Height);

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
        float px = pivotX.Calculate(_cachedRect.Width);
        float py = pivotY.Calculate(_cachedRect.Height);

        Matrix transform = Matrix.CreateTranslation(-px, -py, 0f);
        transform *= Matrix.CreateRotationZ(MathHelper.ToRadians(rotation));
        transform *= Matrix.CreateTranslation(px + _cachedRect.X, py + _cachedRect.Y, 0f);

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
    }

    public void RemoveWidget(Widget widget)
    {
        if (widget.Parent == null || widget.Parent != this) throw new InvalidOperationException();

        _children.Remove(widget);
        widget.Parent = null;
    }
}
