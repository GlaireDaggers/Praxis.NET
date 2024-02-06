namespace Praxis.Core;

/// <summary>
/// Specifies a relative dimension for UI elements
/// </summary>
public struct Dimension
{
    public float percent;
    public int px;

    public static Dimension Pixels(int pixels)
    {
        return new Dimension { percent = 0f, px = pixels };
    }

    public static Dimension Percent(float percent, int offsetPixels = 0)
    {
        return new Dimension { percent = percent, px = offsetPixels };
    }

    public readonly int Calculate(int relativeTo)
    {
        return (int)(percent * relativeTo) + px;
    }
}
