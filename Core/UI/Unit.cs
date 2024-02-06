namespace Praxis.Core;

/// <summary>
/// Enumeration of measurement types a unit can represent
/// </summary>
public enum UnitType
{
    /// <summary>
    /// Unit represents pixels
    /// </summary>
    Pixels,

    /// <summary>
    /// Unit represents a percent (as a value between 0.0 and 1.0)
    /// </summary>
    Percent
}

/// <summary>
/// Specifies a unit of measurement
/// </summary>
public struct Unit
{
    public UnitType type;
    public float value;
    public float offset;

    public static Unit Pixels(int pixels)
    {
        return new Unit { value = pixels, type = UnitType.Pixels };
    }

    public static Unit Percent(float percent, int offsetPixels = 0)
    {
        return new Unit { value = percent, offset = offsetPixels, type = UnitType.Percent };
    }

    public readonly float Calculate(float relativeTo)
    {
        return type switch
        {
            UnitType.Pixels => value + offset,
            UnitType.Percent => (value * relativeTo) + offset,
            _ => 0f,
        };
    }
}
