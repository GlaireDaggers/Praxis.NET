namespace Praxis.Core;

using System.Text.Json.Serialization;
using BepuPhysics;
using BepuPhysics.Constraints;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

[SerializedComponent(nameof(ConstraintComponent))]
public class ConstraintComponentData : IComponentData
{
    [JsonPropertyName("other")]
    public string? Other { get; set; }

    [JsonPropertyName("constraint")]
    public ConstraintDefinition? Constraint { get; set; }

    public void OnDeserialize(PraxisGame game)
    {
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        if (world.FindTaggedEntityInChildren(Other!, root) is Entity other)
        {
            world.Set(entity, new ConstraintComponent
            {
                other = other,
                constraint = Constraint!
            });
        }   
    }
}

public struct ConstraintComponent
{
    public Entity other;
    public ConstraintDefinition constraint;
}

[JsonDerivedType(typeof(BallSocketDefinition), "ballSocket")]
[JsonDerivedType(typeof(DistanceLimitDefinition), "distanceLimit")]
[JsonDerivedType(typeof(WeldDefinition), "weld")]
[JsonDerivedType(typeof(HingeDefinition), "hinge")]
[JsonDerivedType(typeof(PointOnLineDefinition), "pointOnLine")]
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
    [JsonPropertyName("localOffsetA")]
    public Vector3 LocalOffsetA { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("localOffsetB")]
    public Vector3 LocalOffsetB { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("springFrequency")]
    public float SpringFrequency { get; set; } = 30f;
    
    [JsonPropertyName("springDamping")]
    public float SpringDamping { get; set; } = 5f;

    protected override BallSocket GetConstraint()
    {
        return new BallSocket
        {
            LocalOffsetA = NumericsConversion.Convert(LocalOffsetA),
            LocalOffsetB = NumericsConversion.Convert(LocalOffsetB),
            SpringSettings = new SpringSettings(SpringFrequency, SpringDamping)
        };
    }
}

public class DistanceLimitDefinition : ConstraintDefinition<DistanceLimit>
{
    [JsonPropertyName("minDistance")]
    public float MinDistance { get; set; } = 1f;
    
    [JsonPropertyName("maxDistance")]
    public float MaxDistance { get; set; } = 2f;
    
    [JsonPropertyName("LocalOffsetA")]
    public Vector3 LocalOffsetA { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("LocalOffsetB")]
    public Vector3 LocalOffsetB { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("SpringFrequency")]
    public float SpringFrequency { get; set; } = 30f;
    
    [JsonPropertyName("SpringDamping")]
    public float SpringDamping { get; set; } = 5f;

    protected override DistanceLimit GetConstraint()
    {
        return new DistanceLimit
        {
            MinimumDistance = MinDistance,
            MaximumDistance = MaxDistance,
            LocalOffsetA = NumericsConversion.Convert(LocalOffsetA),
            LocalOffsetB = NumericsConversion.Convert(LocalOffsetB),
            SpringSettings = new SpringSettings(SpringFrequency, SpringDamping)
        };
    }
}

public class WeldDefinition : ConstraintDefinition<Weld>
{
    [JsonPropertyName("localOffset")]
    public Vector3 LocalOffset { get; set; }
    
    [JsonPropertyName("localRotation")]
    public Quaternion LocalRotation { get; set; }
    
    [JsonPropertyName("springFrequency")]
    public float SpringFrequency { get; set; } = 30f;
    
    [JsonPropertyName("springDamping")]
    public float SpringDamping { get; set; } = 5f;

    protected override Weld GetConstraint()
    {
        return new Weld
        {
            LocalOffset = NumericsConversion.Convert(LocalOffset),
            LocalOrientation = NumericsConversion.Convert(LocalRotation),
            SpringSettings = new SpringSettings(SpringFrequency, SpringDamping)
        };
    }
}

public class HingeDefinition : ConstraintDefinition<Hinge>
{
    [JsonPropertyName("localOffsetA")]
    public Vector3 LocalOffsetA { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("localHingeAxisA")]
    public Vector3 LocalHingeAxisA { get; set; } = Vector3.UnitX;
    
    [JsonPropertyName("localOffsetB")]
    public Vector3 LocalOffsetB { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("localHingeAxisB")]
    public Vector3 LocalHingeAxisB { get; set; } = Vector3.UnitX;
    
    [JsonPropertyName("springFrequency")]
    public float SpringFrequency { get; set; } = 30f;
    
    [JsonPropertyName("springDamping")]
    public float SpringDamping { get; set; } = 5f;

    protected override Hinge GetConstraint()
    {
        return new Hinge
        {
            LocalOffsetA = NumericsConversion.Convert(LocalOffsetA),
            LocalOffsetB = NumericsConversion.Convert(LocalOffsetB),
            LocalHingeAxisA = NumericsConversion.Convert(LocalHingeAxisA),
            LocalHingeAxisB = NumericsConversion.Convert(LocalHingeAxisB),
            SpringSettings = new SpringSettings(SpringFrequency, SpringDamping)
        };
    }
}

public class PointOnLineDefinition : ConstraintDefinition<PointOnLineServo>
{
    [JsonPropertyName("localOffsetA")]
    public Vector3 LocalOffsetA { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("localOffsetB")]
    public Vector3 LocalOffsetB { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("localDirection")]
    public Vector3 LocalDirection { get; set; } = Vector3.Zero;
    
    [JsonPropertyName("springFrequency")]
    public float SpringFrequency { get; set; } = 30f;
    
    [JsonPropertyName("springDamping")]
    public float SpringDamping { get; set; } = 5f;

    protected override PointOnLineServo GetConstraint()
    {
        return new PointOnLineServo
        {
            LocalOffsetA = NumericsConversion.Convert(LocalOffsetA),
            LocalOffsetB = NumericsConversion.Convert(LocalOffsetB),
            LocalDirection = NumericsConversion.Convert(LocalDirection),
            SpringSettings = new SpringSettings(SpringFrequency, SpringDamping),
            ServoSettings = ServoSettings.Default
        };
    }
}