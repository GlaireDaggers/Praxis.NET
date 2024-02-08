using Microsoft.Xna.Framework;

namespace Praxis.Core;

public static class MathUtils
{
    public static Color Multiply(Color lhs, Color rhs)
    {
        return new Color(lhs.ToVector4() * rhs.ToVector4());
    }
}
