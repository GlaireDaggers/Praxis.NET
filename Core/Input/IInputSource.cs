namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

public enum GamepadAxis
{
    LeftStickX,
    LeftStickY,
    RightStickX,
    RightStickY,
    LeftTrigger,
    RightTrigger,
}

public enum MouseButtons
{
    Left,
    Right,
    Middle
}

public enum MouseAxis
{
    X,
    Y,
    Scroll   
}

public interface IInputSource<T> where T : struct
{
    void Update();
    T GetValue();
}

public class ButtonAxisSource : IInputSource<float>
{
    public readonly IInputSource<bool> button;

    public ButtonAxisSource(IInputSource<bool> button)
    {
        this.button = button;
    }

    public void Update()
    {
        button.Update();
    }

    public float GetValue()
    {
        if (this.button.GetValue())
        {
            return 1f;
        }

        return 0f;
    }
}

public class DualButtonAxisSource : IInputSource<float>
{
    public readonly IInputSource<bool> positive;
    public readonly IInputSource<bool> negative;

    public DualButtonAxisSource(IInputSource<bool> positive, IInputSource<bool> negative)
    {
        this.positive = positive;
        this.negative = negative;
    }

    public void Update()
    {
        positive.Update();
        negative.Update();
    }

    public float GetValue()
    {
        if (this.positive.GetValue())
        {
            return 1f;
        }
        
        if (this.negative.GetValue())
        {
            return -1f;
        }

        return 0f;
    }
}

public class AxisButtonSource : IInputSource<bool>
{
    public readonly IInputSource<float> axis;
    public readonly float threshold;
    public readonly bool reversed;

    public AxisButtonSource(IInputSource<float> axis, float threshold, bool reversed = false)
    {
        this.axis = axis;
        this.threshold = threshold;
        this.reversed = reversed;
    }

    public void Update()
    {
        axis.Update();
    }

    public bool GetValue()
    {
        if (reversed) return axis.GetValue() <= threshold;
        return axis.GetValue() >= threshold;
    }
}

public class CompositeButtonSource : IInputSource<bool>
{
    public List<IInputSource<bool>> sources = new List<IInputSource<bool>>();

    public void Update()
    {
        foreach(var src in sources)
        {
            src.Update();
        }
    }

    public bool GetValue()
    {
        foreach(var src in sources)
        {
            if (src.GetValue())
            {
                return true;
            }
        }

        return false;
    }
}

public class CompositeAxisSource : IInputSource<float>
{
    public List<IInputSource<float>> sources = new List<IInputSource<float>>();

    public void Update()
    {
        foreach (var src in sources)
        {
            src.Update();
        }
    }

    public float GetValue()
    {
        float value = 0f;

        foreach (var src in sources)
        {
            float v = src.GetValue();
            if (System.MathF.Abs(v) >= System.MathF.Abs(value))
            {
                value = v;
            }
        }

        return value;
    }
}

public enum ButtonPhase
{
    None,
    Pressed,
    Held,
    Released,
}

public class ButtonPhaseFilter : IInputSource<ButtonPhase>
{
    public readonly IInputSource<bool> button;

    private bool _prev;
    private bool _cur;

    public ButtonPhaseFilter(IInputSource<bool> button)
    {
        this.button = button;
    }

    public void Update()
    {
        _prev = _cur;
        _cur = button.GetValue();
    }

    public ButtonPhase GetValue()
    {
        if (_cur && !_prev)
            return ButtonPhase.Pressed;
        if (_cur)
            return ButtonPhase.Held;
        if (_prev && !_cur)
            return ButtonPhase.Released;
        return ButtonPhase.None;
    }
}

public class KeyboardButtonSource : IInputSource<bool>
{
    public readonly Keys key;

    public KeyboardButtonSource(Keys key)
    {
        this.key = key;
    }

    public void Update()
    {
    }

    public bool GetValue()
    {
        return InputService.Instance!.GetKeyDown(key);
    }
}

public class MouseButtonSource : IInputSource<bool>
{
    public readonly MouseButtons button;

    public MouseButtonSource(MouseButtons button)
    {
        this.button = button;
    }

    public void Update()
    {
    }

    public bool GetValue()
    {
        return InputService.Instance!.GetMouseButtonDown(button);
    }
}

public class GamepadButtonSource : IInputSource<bool>
{
    public readonly PlayerIndex index;
    public readonly Buttons button;

    public GamepadButtonSource(PlayerIndex index, Buttons button)
    {
        this.index = index;
        this.button = button;
    }

    public void Update()
    {
    }

    public bool GetValue()
    {
        return InputService.Instance!.GetButtonDown(index, button);
    }
}

public class GamepadAxisSource : IInputSource<float>
{
    public readonly PlayerIndex index;
    public readonly GamepadAxis axis;

    public GamepadAxisSource(PlayerIndex index, GamepadAxis axis)
    {
        this.index = index;
        this.axis = axis;
    }

    public void Update()
    {
    }

    public float GetValue()
    {
        return InputService.Instance!.GetGamepadAxis(index, axis);
    }
}

public class MouseAxisSource : IInputSource<float>
{
    public readonly MouseAxis axis;

    public MouseAxisSource(MouseAxis axis)
    {
        this.axis = axis;
    }

    public void Update()
    {
    }

    public float GetValue()
    {
        return InputService.Instance!.GetMouseAxis(axis);
    }
}