using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace Praxis.Core;

public enum CurveInterpolationMode
{
    Step,
    Linear,
    Cubic,
}

/// <summary>
/// Base class for animation curves of some type
/// </summary>
/// <typeparam name="T">The type of data this curve animates</typeparam>
public abstract class AnimationCurve<T>
{
    public struct CurvePoint : IComparable<CurvePoint>
    {
        [JsonPropertyName("time")]
        public float Time { get; set; }

        [JsonPropertyName("tangentIn")]
        public T TangentIn { get; set; }
        
        [JsonPropertyName("value")]
        public T Value { get; set; }
        
        [JsonPropertyName("tangentOut")]
        public T TangentOut { get; set; }

        public int CompareTo(CurvePoint other)
        {
            return Time.CompareTo(other.Time);
        }
    }

    public CurveInterpolationMode interpolationMode;

    private List<CurvePoint> _curvePoints = new List<CurvePoint>();

    public AnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points)
    {
        this.interpolationMode = interpolationMode;
        _curvePoints.AddRange(points);
        _curvePoints.Sort();
    }

    public AnimationCurve(T start, T end, float startTime = 0f, float endTime = 1f)
        : this(CurveInterpolationMode.Linear, [new CurvePoint { Time = startTime, Value = start }, new CurvePoint { Time = endTime, Value = end }])
    {
    }

    public T Sample(float time)
    {
        if (time <= _curvePoints[0].Time)
        {
            return _curvePoints[0].Value;
        }
        if (time >= _curvePoints[_curvePoints.Count - 1].Time)
        {
            return _curvePoints[_curvePoints.Count - 1].Value;
        }

        // get left+right keyframes
        CurvePoint lhs = _curvePoints[0];
        CurvePoint rhs = _curvePoints[1];

        for (int i = 0; i < _curvePoints.Count; i++)
        {
            if (time <= _curvePoints[i].Time)
            {
                lhs = _curvePoints[i - 1];
                rhs = _curvePoints[i];
                break;
            }
        }

        // interpolate
        float nt = (time - lhs.Time) / (rhs.Time - lhs.Time);
        if (interpolationMode == CurveInterpolationMode.Step)
        {
            return lhs.Value;
        }
        else if (interpolationMode == CurveInterpolationMode.Linear)
        {
            return Lerp(lhs.Value, rhs.Value, nt);
        }
        else
        {
            return InterpolateCubic(lhs.Value, rhs.Value, lhs.TangentOut, rhs.TangentIn, nt);
        }
    }

    protected abstract T Lerp(T a, T b, float t);
    protected abstract T InterpolateCubic(T a, T b, T inTangent, T outTangent, float t);

    protected static (float startPosition, float endPosition, float startTangent, float endTangent) CreateHermitePointWeights(float amount)
    {
        // http://mathworld.wolfram.com/HermitePolynomial.html

        // https://www.cubic.org/docs/hermite.htm

        var squared = amount * amount;
        var cubed = amount * squared;

        var part2 = (3.0f * squared) - (2.0f * cubed);
        var part1 = 1 - part2;
        var part4 = cubed - squared;
        var part3 = part4 - squared + amount;

        return (part1, part2, part3, part4);
    }
}

/// <summary>
/// Animation curve which stores float values
/// </summary>
public class FloatAnimationCurve : AnimationCurve<float>
{
    public FloatAnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points) : base(interpolationMode, points)
    {
    }

    public FloatAnimationCurve(float start, float end, float startTime = 0f, float endTime = 1f) : base(start, end, startTime, endTime)
    {
    }

    protected override float Lerp(float a, float b, float t)
    {
        return MathHelper.Lerp(a, b, t);
    }

    protected override float InterpolateCubic(float a, float b, float inTangent, float outTangent, float t)
    {
        var hermite = CreateHermitePointWeights(t);
        return (a * hermite.startPosition) + (b * hermite.endPosition) + (inTangent * hermite.startTangent) + (outTangent * hermite.endTangent);
    }
}

/// <summary>
/// Animation curve which stores Vector2 values
/// </summary>
public class Vector2AnimationCurve : AnimationCurve<Vector2>
{
    public Vector2AnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points) : base(interpolationMode, points)
    {
    }

    public Vector2AnimationCurve(Vector2 start, Vector2 end, float startTime = 0f, float endTime = 1f) : base(start, end, startTime, endTime)
    {
    }

    protected override Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        return Vector2.Lerp(a, b, t);
    }

    protected override Vector2 InterpolateCubic(Vector2 a, Vector2 b, Vector2 inTangent, Vector2 outTangent, float t)
    {
        var hermite = CreateHermitePointWeights(t);
        return (a * hermite.startPosition) + (b * hermite.endPosition) + (inTangent * hermite.startTangent) + (outTangent * hermite.endTangent);
    }
}

/// <summary>
/// Animation curve which stores Vector3 values
/// </summary>
public class Vector3AnimationCurve : AnimationCurve<Vector3>
{
    public Vector3AnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points) : base(interpolationMode, points)
    {
    }

    public Vector3AnimationCurve(Vector3 start, Vector3 end, float startTime = 0f, float endTime = 1f) : base(start, end, startTime, endTime)
    {
    }

    protected override Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        return Vector3.Lerp(a, b, t);
    }

    protected override Vector3 InterpolateCubic(Vector3 a, Vector3 b, Vector3 inTangent, Vector3 outTangent, float t)
    {
        var hermite = CreateHermitePointWeights(t);
        return (a * hermite.startPosition) + (b * hermite.endPosition) + (inTangent * hermite.startTangent) + (outTangent * hermite.endTangent);
    }
}

/// <summary>
/// Animation curve which stores Vector4 values
/// </summary>
public class Vector4AnimationCurve : AnimationCurve<Vector4>
{
    public Vector4AnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points) : base(interpolationMode, points)
    {
    }

    public Vector4AnimationCurve(Vector4 start, Vector4 end, float startTime = 0f, float endTime = 1f) : base(start, end, startTime, endTime)
    {
    }

    protected override Vector4 Lerp(Vector4 a, Vector4 b, float t)
    {
        return Vector4.Lerp(a, b, t);
    }

    protected override Vector4 InterpolateCubic(Vector4 a, Vector4 b, Vector4 inTangent, Vector4 outTangent, float t)
    {
        var hermite = CreateHermitePointWeights(t);
        return (a * hermite.startPosition) + (b * hermite.endPosition) + (inTangent * hermite.startTangent) + (outTangent * hermite.endTangent);
    }
}

/// <summary>
/// Animation curve which stores Quaternion values
/// </summary>
public class QuaternionAnimationCurve : AnimationCurve<Quaternion>
{
    public QuaternionAnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points) : base(interpolationMode, points)
    {
    }

    public QuaternionAnimationCurve(Quaternion start, Quaternion end, float startTime = 0f, float endTime = 1f) : base(start, end, startTime, endTime)
    {
    }

    protected override Quaternion Lerp(Quaternion a, Quaternion b, float t)
    {
        return Quaternion.Slerp(a, b, t);
    }

    protected override Quaternion InterpolateCubic(Quaternion a, Quaternion b, Quaternion inTangent, Quaternion outTangent, float t)
    {
        var hermite = CreateHermitePointWeights(t);
        return Quaternion.Normalize((a * hermite.startPosition) + (b * hermite.endPosition) + (inTangent * hermite.startTangent) + (outTangent * hermite.endTangent));
    }
}

/// <summary>
/// Animation curve which stores Color values
/// </summary>
public class ColorAnimationCurve : AnimationCurve<Color>
{
    public ColorAnimationCurve(CurveInterpolationMode interpolationMode, CurvePoint[] points) : base(interpolationMode, points)
    {
    }

    public ColorAnimationCurve(Color start, Color end, float startTime = 0f, float endTime = 1f) : base(start, end, startTime, endTime)
    {
    }

    protected override Color Lerp(Color a, Color b, float t)
    {
        return Color.Lerp(a, b, t);
    }

    protected override Color InterpolateCubic(Color a, Color b, Color inTangent, Color outTangent, float t)
    {
        // interpolate as Vector4 and then convert back to color
        var hermite = CreateHermitePointWeights(t);
        Vector4 v = (a.ToVector4() * hermite.startPosition) + (b.ToVector4() * hermite.endPosition) + (inTangent.ToVector4() * hermite.startTangent) + (outTangent.ToVector4() * hermite.endTangent);
        return new Color(v);
    }
}