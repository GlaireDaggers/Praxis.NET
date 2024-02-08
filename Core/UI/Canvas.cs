using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Praxis.Core;

/// <summary>
/// Root container of widgets to be drawn to the screen
/// </summary>
public class Canvas(PraxisGame game, RuntimeResource<Stylesheet> stylesheet) : Widget()
{
    public static Canvas Load(PraxisGame game, XmlDocument xml)
    {
        if (xml.ChildNodes.Count != 1)
        {
            throw new FormatException("UI document has invalid number of root nodes (expected 1)");
        }

        var root = xml.FirstChild!;

        if (root.Name != "Canvas")
        {
            throw new FormatException("UI document has invalid root node (expected Canvas)");
        }

        string stylesheetPath = root.Attributes!["stylesheet"]!.Value;

        Canvas canvas = new(game, game.Resources.Load<Stylesheet>(stylesheetPath));
        canvas.Deserialize(game, root);

        foreach (var child in root.ChildNodes.Cast<XmlNode>())
        {
            if (child.Name == "#comment") continue;
                
            Widget childWidget = Load(game, child);
            canvas.AddWidget(childWidget);
        }

        return canvas;
    }

    public readonly PraxisGame Game = game;
    public readonly RuntimeResource<Stylesheet> Styles = stylesheet;

    private Widget? _currentHover = null;
    private Widget? _mouseDown = null;
    private Widget? _focusedWidget = null;

    public void DrawUI(UIRenderer renderer)
    {
        renderer.Begin();
        DrawInternal(renderer);
        renderer.End();
    }

    public override void UpdateLayout(UIRenderer renderer)
    {
        int screenWidth = renderer.ScreenWidth;
        int screenHeight = renderer.ScreenHeight;

        // calculate dimensions (Canvas does not respect anchor settings)
        _cachedRect.X = 0;
        _cachedRect.Y = 0;
        _cachedRect.Width = screenWidth;
        _cachedRect.Height = screenHeight;

        foreach (var child in Children)
        {
            child.UpdateLayout(renderer);
        }

        // now that layouts have been updated, we can also handle input
        Vector2 mousePos = new Vector2(Game.CurrentMouseState.X, Game.CurrentMouseState.Y);
        if (GetWidgetAtPos(mousePos) is Widget widget)
        {
            if (widget != _currentHover)
            {
                _currentHover?.HandleMouseExit();
                widget?.HandleMouseEnter();
                _currentHover = widget;
            }
        }
        else
        {
            _currentHover?.HandleMouseExit();
            _currentHover = null;
        }

        if (Game.CurrentMouseState.LeftButton == ButtonState.Pressed && Game.PreviousMouseState.LeftButton == ButtonState.Released)
        {
            _currentHover?.HandleMouseDown();
            _mouseDown = _currentHover;
        }
        else if (Game.CurrentMouseState.LeftButton == ButtonState.Released && Game.PreviousMouseState.LeftButton == ButtonState.Pressed)
        {
            _mouseDown?.HandleMouseUp();
            if (_mouseDown == _currentHover)
            {
                _mouseDown?.HandleClick();
                SetFocus(_mouseDown);
            }
            else if (_mouseDown == _focusedWidget)
            {
                SetFocus(null);
            }
            _mouseDown = null;
        }
    }

    public void SetFocus(Widget? newFocus)
    {
        if (newFocus != _focusedWidget)
        {
            _focusedWidget?.HandleFocusLost();
            _focusedWidget = newFocus;
            _focusedWidget?.HandleFocusGained();
        }
    }
}
