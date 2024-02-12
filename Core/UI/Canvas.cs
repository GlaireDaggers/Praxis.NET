using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Praxis.Core;

/// <summary>
/// Root container of widgets to be drawn to the screen
/// </summary>
public class Canvas(PraxisGame game, RuntimeResource<Stylesheet> stylesheet) : Widget()
{
    private const float REPEAT_DELAY = 0.5f;
    private const float REPEAT_INTERVAL = 0.05f;

    private const int DRAG_THRESHOLD = 4;
    private const int DRAG_THRESHOLD2 = DRAG_THRESHOLD * DRAG_THRESHOLD;

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

    public Widget? FocusedWidget => _focusedWidget;

    private Vector2 _mouseDownPos = Vector2.Zero;
    private bool _isDragging = false;
    
    private Widget? _currentHover = null;
    private Widget? _mouseDown = null;
    private Widget? _focusedWidget = null;

    private float _navRepeatTimer = 0f;
    private bool _navPressed = false;

    private Widget? _dragSource = null;
    private object? _dragData = null;

    public void DrawUI(UIRenderer renderer)
    {
        renderer.Begin();
        DrawInternal(renderer);
        renderer.End();
    }

    public override void Update(UIRenderer renderer, float deltaTime)
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
            child.Update(renderer, deltaTime);
        }

        // now that layouts have been updated, we can also handle input
        Vector2 mousePos = new Vector2(Game.CurrentMouseState.X, Game.CurrentMouseState.Y);
        Vector2 prevMousePos = new Vector2(Game.PreviousMouseState.X, Game.PreviousMouseState.Y);

        if (Vector2.DistanceSquared(mousePos, prevMousePos) >= 1f)
        {
            if (GetWidgetAtPos(mousePos) is Widget widget)
            {
                SetHover(widget);
            }
            else
            {
                SetHover(null);
            }
        }

        if (Game.CurrentMouseState.LeftButton == ButtonState.Pressed && Game.PreviousMouseState.LeftButton == ButtonState.Released)
        {
            _currentHover?.HandleMouseDown();
            _mouseDown = _currentHover;
            _mouseDownPos = mousePos;
        }
        else if (Game.CurrentMouseState.LeftButton == ButtonState.Released && Game.PreviousMouseState.LeftButton == ButtonState.Pressed)
        {
            if (_dragData != null)
            {
                _currentHover?.HandleDragDrop(_dragData);
            }
            
            _dragSource?.HandleDragEnd(_dragData == null);
            _dragSource = null;
            _isDragging = false;
            _dragData = null;

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

        if (_mouseDown != null)
        {
            if (!_isDragging && Vector2.DistanceSquared(_mouseDownPos, mousePos) >= DRAG_THRESHOLD2)
            {
                _mouseDown.HandleDragStart();
                _isDragging = true;
            }
        }

        // don't send input events if there's a screen keyboard open
        if (!TextInputEXT.IsScreenKeyboardShown(Game.Window.Handle))
        {
            if (Game.CurrentKeyboardState.IsKeyDown(Keys.Up) || Game.Input.GetButtonDown(PlayerIndex.One, Buttons.DPadUp))
            {
                HandleNav(NavigationDirection.Up, deltaTime);
            }
            else if (Game.CurrentKeyboardState.IsKeyDown(Keys.Down) || Game.Input.GetButtonDown(PlayerIndex.One, Buttons.DPadDown))
            {
                HandleNav(NavigationDirection.Down, deltaTime);
            }
            else if (Game.CurrentKeyboardState.IsKeyDown(Keys.Left) || Game.Input.GetButtonDown(PlayerIndex.One, Buttons.DPadLeft))
            {
                HandleNav(NavigationDirection.Left, deltaTime);
            }
            else if (Game.CurrentKeyboardState.IsKeyDown(Keys.Right) || Game.Input.GetButtonDown(PlayerIndex.One, Buttons.DPadRight))
            {
                HandleNav(NavigationDirection.Right, deltaTime);
            }
            else
            {
                _navPressed = false;
                _navRepeatTimer = 0f;
            }
        }
        else
        {
            _navPressed = false;
            _navRepeatTimer = 0f;
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

    public void SetHover(Widget? newHover)
    {
        if (newHover != _currentHover)
        {
            _currentHover?.HandleMouseExit();
            newHover?.HandleMouseEnter();
            
            if (_dragData != null)
            {
                _currentHover?.HandleDragExit();
                newHover?.HandleDragEnter(_dragData);
            }
            
            _currentHover = newHover;
        }
    }

    public void BeginDrag(Widget source, object dragData)
    {
        _dragSource = source;
        _dragData = dragData;
    }

    public void AcceptDrag()
    {
        _dragData = null;
    }

    private void HandleNav(NavigationDirection dir, float deltaTime)
    {
        if (!_navPressed)
        {
            _navPressed = true;
            _focusedWidget?.HandleNavigation(dir);
            _navRepeatTimer = REPEAT_DELAY;
        }
        else
        {
            _navRepeatTimer -= deltaTime;
            if (_navRepeatTimer <= 0f)
            {
                _focusedWidget?.HandleNavigation(dir);
                _navRepeatTimer = REPEAT_INTERVAL;
            }
        }
    }
}
