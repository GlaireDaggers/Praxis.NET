using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Praxis.Core;

/// <summary>
/// Root container of widgets to be drawn to the screen
/// </summary>
public class Canvas : Widget
{
    public readonly PraxisGame Game;

    private Widget? _currentHover = null;
    private Widget? _mouseDown = null;
    private Widget? _focusedWidget = null;

    public Canvas(PraxisGame game) : base()
    {
        Game = game;
    }

    public void DrawUI(UIRenderer renderer)
    {
        renderer.Begin();
        DrawInternal(renderer);
        renderer.End();
    }

    public override void UpdateLayout(UIRenderer renderer)
    {
        int screenWidth = renderer.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = renderer.GraphicsDevice.PresentationParameters.BackBufferHeight;

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
            _mouseDown?.HandleMouseDown();
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
