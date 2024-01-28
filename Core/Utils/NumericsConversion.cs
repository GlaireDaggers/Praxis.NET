namespace Praxis.Core;

public static class NumericsConversion
{
    public static System.Numerics.Vector2 Convert(Microsoft.Xna.Framework.Vector2 value)
    {
        return new System.Numerics.Vector2(value.X, value.Y);
    }

    public static System.Numerics.Vector3 Convert(Microsoft.Xna.Framework.Vector3 value)
    {
        return new System.Numerics.Vector3(value.X, value.Y, value.Z);
    }

    public static System.Numerics.Vector4 Convert(Microsoft.Xna.Framework.Vector4 value)
    {
        return new System.Numerics.Vector4(value.X, value.Y, value.Z, value.W);
    }

    public static System.Numerics.Quaternion Convert(Microsoft.Xna.Framework.Quaternion value)
    {
        return new System.Numerics.Quaternion(value.X, value.Y, value.Z, value.W);
    }
}
