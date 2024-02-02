namespace Praxis.Core;

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

[Flags]
public enum CharacterCollisionFlags
{
    None = 0,
    CollideAbove = 1,
    CollideSides = 2,
    CollideBelow = 4
}

/// <summary>
/// Instance of a CharacterController which can perform collide-and-slide collision
/// </summary>
public struct CharacterController : IDisposable
{
    private const int MAX_SLIDE_ITERATIONS = 5;

    public float Radius
    {
        readonly get => _shape.Radius;
        set
        {
            _shape.Radius = value;
            _system.Sim.Shapes.Remove(_shapeHandle);
            _shapeHandle = _system.Sim.Shapes.Add(_shape);
            _system.Sim.Bodies[_collision].SetShape(_shapeHandle);
        }
    }

    public float Height
    {
        readonly get => _shape.Length;
        set
        {
            Debug.Assert(value >= (Radius * 2f));
            _shape.Length = value - (Radius * 2f);
            _system.Sim.Shapes.Remove(_shapeHandle);
            _shapeHandle = _system.Sim.Shapes.Add(_shape);
            _system.Sim.Bodies[_collision].SetShape(_shapeHandle);
        }
    }

    public float MaxSlope
    {
        readonly get => _maxSlope;
        set
        {
            _maxSlope = value;
            _maxSlopeCos = MathF.Cos(MathHelper.ToRadians(value));
        }
    }

    public Vector3 Position
    {
        readonly get => _position;
        set
        {
            _position = value;
            _system.Sim.Bodies[_collision].Pose.Position = NumericsConversion.Convert(value);
        }
    }

    private PhysicsSystem _system;
    private BodyHandle _collision;
    private Capsule _shape;
    private TypedIndex _shapeHandle;
    private Vector3 _position;
    private uint _mask;
    private float _skinWidth;
    private float _maxSlope;
    private float _maxSlopeCos;

    public CharacterController(PhysicsSystem system, float height, float radius, float skinWidth, float maxSlope, uint collisionMask, Entity owner)
    {
        Debug.Assert(height >= (radius * 2f));

        _maxSlope = maxSlope;
        _maxSlopeCos = MathF.Cos(MathHelper.ToRadians(_maxSlope));
        _skinWidth = skinWidth;
        _mask = collisionMask;
        _system = system;
        _position = Vector3.Zero;

        _shape = new Capsule(radius, height - (radius * 2f));
        _shapeHandle = system.Sim.Shapes.Add(_shape);

        RigidPose pose = new RigidPose(System.Numerics.Vector3.Zero);
        _collision = system.Sim.Bodies.Add(BodyDescription.CreateKinematic(pose, _shapeHandle, 0.01f));

        system.RegisterBody(_collision, owner, collisionMask, PhysicsMaterial.Default);
    }

    public readonly void Dispose()
    {
        _system.UnregisterBody(_collision);
        _system.Sim.Bodies.Remove(_collision);
        _system.Sim.Shapes.Remove(_shapeHandle);
    }

    public CharacterCollisionFlags Move(in Vector3 velocity, float deltaTime)
    {
        if (velocity.LengthSquared() == 0f)
        {
            return CharacterCollisionFlags.None;
        }

        Vector3 delta = velocity * deltaTime;

        // decompose delta into sideways and vertical velocities
        Vector3 sideways = new Vector3(delta.X, 0f, delta.Z);
        Vector3 vertical = new Vector3(0f, delta.Y, 0f);
        Vector3 pos = Position;

        // temporarily disable collision against our own body by setting its mask to 0
        _system.SetBodyMask(_collision, 0);

        CharacterCollisionFlags flags = CharacterCollisionFlags.None;

        // sweep
        flags |= SweepSideways(ref pos, sideways);
        flags |= SweepVertical(ref pos, vertical);

        // re-enable collision
        _system.SetBodyMask(_collision, _mask);

        // set new position
        Position = pos;
        
        return flags;
    }

    private readonly CharacterCollisionFlags SweepSideways(ref Vector3 pos, in Vector3 delta)
    {
        CharacterCollisionFlags flags = CharacterCollisionFlags.None;
        Vector3 remainingDelta = delta;
        
        for (int i = 0; i < MAX_SLIDE_ITERATIONS; i++)
        {
            // sweep along remaining delta
            if (SweepAndMove(ref pos, remainingDelta, out var hit))
            {
                flags |= CharacterCollisionFlags.CollideSides;

                // subtract how far we moved from remaining delta
                float len = remainingDelta.Length();
                // treat what we just hit as an infinite plane and reproject remaining delta along it
                remainingDelta = Vector3.Normalize(remainingDelta) * (len - hit.hitDistance);
                float d = Vector3.Dot(remainingDelta, hit.hitNormal);
                remainingDelta -= hit.hitNormal * d;
            }
            else
            {
                break;
            }
        }

        return flags;
    }

    private readonly CharacterCollisionFlags SweepVertical(ref Vector3 pos, in Vector3 delta)
    {
        CharacterCollisionFlags flags = CharacterCollisionFlags.None;
        Vector3 remainingDelta = delta;

        for (int i = 0; i < MAX_SLIDE_ITERATIONS; i++)
        {
            // sweep along remaining delta
            if (SweepAndMove(ref pos, remainingDelta, out var hit))
            {
                if (remainingDelta.Y <= 0f)
                {
                    flags |= CharacterCollisionFlags.CollideBelow;

                    // check the slope of what we just hit. if it's greater than maxSlope, just exit now
                    float slope = hit.hitNormal.Y;
                    if (slope >= _maxSlopeCos)
                    {
                        break;
                    }
                }
                else
                {
                    flags |= CharacterCollisionFlags.CollideAbove;
                }
                
                // subtract how far we moved from remaining delta
                float len = remainingDelta.Length();
                // treat what we just hit as an infinite plane and reproject remaining delta along it
                remainingDelta = Vector3.Normalize(remainingDelta) * (len - hit.hitDistance);
                float d = Vector3.Dot(remainingDelta, hit.hitNormal);
                remainingDelta -= hit.hitNormal * d;
            }
            else
            {
                break;
            }
        }

        return flags;
    }

    private readonly bool SweepAndMove(ref Vector3 pos, in Vector3 delta, out RaycastHit hit)
    {
        float len = delta.LengthSquared();

        if (len == 0f)
        {
            hit = new RaycastHit();
            return false;
        }

        len = MathF.Sqrt(len);
        Vector3 sweepDir = delta / len;

        if (_system.CapsuleCast(pos, Quaternion.Identity, sweepDir, Radius, Height, len + _skinWidth, out hit, _mask))
        {
            // set new position
            hit.hitDistance -= _skinWidth;
            if (hit.hitDistance < 0f) hit.hitDistance = 0f;
            
            pos += sweepDir * hit.hitDistance;
            return true;
        }
        else
        {
            pos += delta;
            return false;
        }
    }
}
