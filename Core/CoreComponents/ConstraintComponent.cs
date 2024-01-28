namespace Praxis.Core;

using BepuPhysics;
using BepuPhysics.Constraints;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct ConstraintComponent
{
    public Entity other;
    public ConstraintDefinition constraint;
}

public abstract class ConstraintDefinition
{
    public abstract ConstraintHandle Construct(Simulation sim, in BodyHandle a, in BodyHandle b);
    public abstract void Update(Simulation sim, ConstraintHandle constraint);
}

public abstract class ConstraintDefinition<T> : ConstraintDefinition
    where T : unmanaged, ITwoBodyConstraintDescription<T>
{
    public override ConstraintHandle Construct(Simulation sim, in BodyHandle a, in BodyHandle b)
    {
        return sim.Solver.Add(a, b, GetConstraint());
    }

    public override void Update(Simulation sim, ConstraintHandle constraint)
    {
        sim.Solver.ApplyDescriptionWithoutWaking(constraint, GetConstraint());
    }

    protected abstract T GetConstraint();
}

public class BallSocketDefinition : ConstraintDefinition<BallSocket>
{
    public Vector3 localOffsetA = Vector3.Zero;
    public Vector3 localOffsetB = Vector3.Zero;
    public float springFrequency = 30f;
    public float springDamping = 5f;

    protected override BallSocket GetConstraint()
    {
        return new BallSocket
        {
            LocalOffsetA = NumericsConversion.Convert(localOffsetA),
            LocalOffsetB = NumericsConversion.Convert(localOffsetB),
            SpringSettings = new SpringSettings(springFrequency, springDamping)
        };
    }
}

public class DistanceLimitDefinition : ConstraintDefinition<DistanceLimit>
{
    public float minDistance = 1f;
    public float maxDistance = 2f;
    public Vector3 localOffsetA = Vector3.Zero;
    public Vector3 localOffsetB = Vector3.Zero;
    public float springFrequency = 30f;
    public float springDamping = 5f;

    protected override DistanceLimit GetConstraint()
    {
        return new DistanceLimit
        {
            MinimumDistance = minDistance,
            MaximumDistance = maxDistance,
            LocalOffsetA = NumericsConversion.Convert(localOffsetA),
            LocalOffsetB = NumericsConversion.Convert(localOffsetB),
            SpringSettings = new SpringSettings(springFrequency, springDamping)
        };
    }
}

public class WeldDefinition : ConstraintDefinition<Weld>
{
    public Vector3 localOffset;
    public Quaternion localRotation;
    public float springFrequency = 30f;
    public float springDamping = 5f;

    protected override Weld GetConstraint()
    {
        return new Weld
        {
            LocalOffset = NumericsConversion.Convert(localOffset),
            LocalOrientation = NumericsConversion.Convert(localRotation),
            SpringSettings = new SpringSettings(springFrequency, springDamping)
        };
    }
}

public class HingeDefinition : ConstraintDefinition<Hinge>
{
    public Vector3 localOffsetA = Vector3.Zero;
    public Vector3 localHingeAxisA = Vector3.UnitX;
    public Vector3 localOffsetB = Vector3.Zero;
    public Vector3 localHingeAxisB = Vector3.UnitX;
    public float springFrequency = 30f;
    public float springDamping = 5f;

    protected override Hinge GetConstraint()
    {
        return new Hinge
        {
            LocalOffsetA = NumericsConversion.Convert(localOffsetA),
            LocalOffsetB = NumericsConversion.Convert(localOffsetB),
            LocalHingeAxisA = NumericsConversion.Convert(localHingeAxisA),
            LocalHingeAxisB = NumericsConversion.Convert(localHingeAxisB),
            SpringSettings = new SpringSettings(springFrequency, springDamping)
        };
    }
}

public class PointOnLineDefinition : ConstraintDefinition<PointOnLineServo>
{
    public Vector3 localOffsetA = Vector3.Zero;
    public Vector3 localOffsetB = Vector3.Zero;
    public Vector3 localDirection = Vector3.Zero;
    public float springFrequency = 30f;
    public float springDamping = 5f;

    protected override PointOnLineServo GetConstraint()
    {
        return new PointOnLineServo
        {
            LocalOffsetA = NumericsConversion.Convert(localOffsetA),
            LocalOffsetB = NumericsConversion.Convert(localOffsetB),
            LocalDirection = NumericsConversion.Convert(localDirection),
            SpringSettings = new SpringSettings(springFrequency, springDamping),
            ServoSettings = ServoSettings.Default
        };
    }
}