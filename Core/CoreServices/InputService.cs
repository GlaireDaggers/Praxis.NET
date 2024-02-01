using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Praxis.Core;

public class InputService : PraxisService
{
    public static InputService? Instance { get; private set; }

    public bool enabled = true;

    private List<PlayerIndex> _gamepads = new List<PlayerIndex>();
    private Dictionary<PlayerIndex, GamePadState> _gamepadStates = new Dictionary<PlayerIndex, GamePadState>();

    private Dictionary<string, IInputSource<ButtonPhase>> _buttonInputSources = new Dictionary<string, IInputSource<ButtonPhase>>();
    private Dictionary<string, IInputSource<float>> _axisInputSources = new Dictionary<string, IInputSource<float>>();

    private List<IInputSource<ButtonPhase>> _buttonList = new List<IInputSource<ButtonPhase>>();
    private List<IInputSource<float>> _axisList = new List<IInputSource<float>>();

    public InputService(PraxisGame game) : base(game)
    {
        Instance = this;
    }

    public void BindInput(string name, IInputSource<ButtonPhase> source)
    {
        _buttonInputSources[name] = source;
        _buttonList.Add(source);
    }

    public void BindInput(string name, IInputSource<float> source)
    {
        _axisInputSources[name] = source;
        _axisList.Add(source);
    }

    public string[] GetButtonBindingNames()
    {
        return _buttonInputSources.Keys.ToArray();
    }

    public string[] GetAxisBindingNames()
    {
        return _axisInputSources.Keys.ToArray();
    }

    public bool HasButtonBinding(string name)
    {
        return _buttonInputSources.ContainsKey(name);
    }

    public bool HasAxisBinding(string name)
    {
        return _axisInputSources.ContainsKey(name);
    }

    public IInputSource<ButtonPhase> GetButtonBinding(string name)
    {
        return _buttonInputSources[name];
    }

    public IInputSource<float> GetAxisBinding(string name)
    {
        return _axisInputSources[name];
    }

    public ButtonPhase GetButton(string name)
    {
        if (_buttonInputSources.TryGetValue(name, out var binding))
        {
            return binding.GetValue();
        }

        return ButtonPhase.None;
    }

    public float GetAxis(string name)
    {
        if (_axisInputSources.TryGetValue(name, out var binding))
        {
            return binding.GetValue();
        }

        return 0f;
    }

    public override void Update(float deltaTime)
    {
        foreach (var gamepad in _gamepads)
        {
            _gamepadStates[gamepad] = GamePad.GetState(gamepad);
        }

        foreach (var button in _buttonList)
        {
            button.Update();
        }

        foreach (var axis in _axisList)
        {
            axis.Update();
        }
    }

    public bool GetKeyDown(Keys key)
    {
        if (!enabled) return false;

        return Game.CurrentKeyboardState.IsKeyDown(key);
    }

    public bool GetButtonDown(PlayerIndex index, Buttons button)
    {
        if (!enabled) return false;

        if (!_gamepadStates.ContainsKey(index))
        {
            _gamepads.Add(index);
            _gamepadStates[index] = GamePad.GetState(index);
        }

        return _gamepadStates[index].IsButtonDown(button);
    }

    public bool GetMouseButtonDown(MouseButtons button)
    {
        if (!enabled) return false;

        switch (button)
        {
            case MouseButtons.Left: return Game.CurrentMouseState.LeftButton == ButtonState.Pressed;
            case MouseButtons.Right: return Game.CurrentMouseState.RightButton == ButtonState.Pressed;
            case MouseButtons.Middle: return Game.CurrentMouseState.MiddleButton == ButtonState.Pressed;
        }

        return false;
    }

    public float GetGamepadAxis(PlayerIndex index, GamepadAxis axis)
    {
        if (!enabled) return 0f;

        if (!_gamepadStates.ContainsKey(index))
        {
            _gamepads.Add(index);
            _gamepadStates[index] = GamePad.GetState(index);
        }

        var state = _gamepadStates[index];

        switch (axis)
        {
            case GamepadAxis.LeftStickX:
                return state.ThumbSticks.Left.X;
            case GamepadAxis.LeftStickY:
                return state.ThumbSticks.Left.Y;
            case GamepadAxis.RightStickX:
                return state.ThumbSticks.Right.X;
            case GamepadAxis.RightStickY:
                return state.ThumbSticks.Right.Y;
            case GamepadAxis.LeftTrigger:
                return state.Triggers.Left;
            case GamepadAxis.RightTrigger:
                return state.Triggers.Right;
        }

        return 0f;
    }

    public float GetMouseAxis(MouseAxis axis)
    {
        if (!enabled) return 0f;

        switch (axis)
        {
            case MouseAxis.X:
                return Mouse.IsRelativeMouseModeEXT ? Game.CurrentMouseState.X : Game.CurrentMouseState.X - Game.PreviousMouseState.X;
            case MouseAxis.Y:
                return -(Mouse.IsRelativeMouseModeEXT ? Game.CurrentMouseState.Y : Game.CurrentMouseState.Y - Game.PreviousMouseState.Y);
            case MouseAxis.Scroll:
                return Game.CurrentMouseState.ScrollWheelValue - Game.PreviousMouseState.ScrollWheelValue;
        }

        return 0f;
    }
}
