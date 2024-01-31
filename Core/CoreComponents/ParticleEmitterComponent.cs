namespace Praxis.Core;

using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct ParticleEmitterComponent
{
    public bool worldSpace;
    public int maxParticles;
    public int particlesPerBurst;
    public float burstInterval;
    public int maxBurstCount;
    public float minParticleLifetime;
    public float maxParticleLifetime;
    public float minAngle;
    public float maxAngle;
    public float minAngularVelocity;
    public float maxAngularVelocity;
    public float linearDamping;
    public float angularDamping;
}

public struct ParticleEmitterSphereShapeComponent
{
    public float radius;
}

public struct ParticleEmitterBoxShapeComponent
{
    public Vector3 extents;
}

public struct ParticleEmitterCylinderShapeComponent
{
    public float radius;
    public float height;
}

public struct ParticleEmitterInitRandomVelocityComponent
{
    public Vector3 minForce;
    public Vector3 maxForce;
}

public struct ParticleEmitterInitVelocityFromPointComponent
{
    public Vector3 origin;
    public float minForce;
    public float maxForce;
}

public struct ParticleEmitterInitVelocityInConeComponent
{
    public Vector3 direction;
    public float angle;
    public float minForce;
    public float maxForce;
}

public struct ParticleEmitterAddLinearForceComponent
{
    public bool worldSpace;
    public Vector3 force;
}

public struct ParticleEmitterAddNoiseForceComponent
{
    public int seed;
    public bool worldSpace;
    public Vector3 scroll;
    public float frequency;
    public float magnitude;
}

public struct ParticleEmitterAttractionForceComponent
{
    public bool worldSpace;
    public Vector3 target;
    public float maxRadius;
    public float force;
}

public struct ParticleEmitterSpriteRenderComponent
{
    public float sortBias;
    public RuntimeResource<Material> material;
    public ColorAnimationCurve colorOverLifetime;
    public Vector2AnimationCurve sizeOverLifetime;
    public int sheetRows;
    public int sheetColumns;
    public int sheetCycles;
}

[SerializedComponent(nameof(ParticleEmitterComponent))]
public class ParticleEmitterComponentData : IComponentData
{
    [JsonPropertyName("worldSpace")]
    public bool WorldSpace { get; set; }
    
    [JsonPropertyName("maxParticles")]
    public int MaxParticles { get; set; }
    
    [JsonPropertyName("particlesPerBurst")]
    public int ParticlesPerBurst { get; set; }
    
    [JsonPropertyName("burstInterval")]
    public float BurstInterval { get; set; }
    
    [JsonPropertyName("maxBurstCount")]
    public int MaxBurstCount { get; set; }
    
    [JsonPropertyName("minParticleLifetime")]
    public float MinParticleLifetime { get; set; }
    
    [JsonPropertyName("maxParticleLifetime")]
    public float MaxParticleLifetime { get; set; }
    
    [JsonPropertyName("minAngle")]
    public float MinAngle { get; set; }
    
    [JsonPropertyName("maxAngle")]
    public float MaxAngle { get; set; }
    
    [JsonPropertyName("minAngularVelocity")]
    public float MinAngularVelocity { get; set; }
    
    [JsonPropertyName("maxAngularVelocity")]
    public float MaxAngularVelocity { get; set; }
    
    [JsonPropertyName("linearDamping")]
    public float LinearDamping { get; set; }
    
    [JsonPropertyName("angularDamping")]
    public float AngularDamping { get; set; }

    private ParticleEmitterComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterComponent
        {
            worldSpace = WorldSpace,
            maxParticles = MaxParticles,
            particlesPerBurst = ParticlesPerBurst,
            burstInterval = BurstInterval,
            maxBurstCount = MaxBurstCount,
            minParticleLifetime = MinParticleLifetime,
            maxParticleLifetime = MaxParticleLifetime,
            minAngle = MinAngle,
            maxAngle = MaxAngle,
            minAngularVelocity = MinAngularVelocity,
            maxAngularVelocity = MaxAngularVelocity,
            linearDamping = LinearDamping,
            angularDamping = AngularDamping
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterSphereShapeComponent))]
public class ParticleEmitterSphereShapeComponentData : IComponentData
{
    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    private ParticleEmitterSphereShapeComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterSphereShapeComponent
        {
            radius = Radius
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterBoxShapeComponent))]
public class ParticleEmitterBoxShapeComponentData : IComponentData
{
    [JsonPropertyName("extents")]
    public Vector3 Extents { get; set; }

    private ParticleEmitterBoxShapeComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterBoxShapeComponent
        {
            extents = Extents
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterCylinderShapeComponent))]
public class ParticleEmitterCylinderShapeComponentData : IComponentData
{
    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    [JsonPropertyName("height")]
    public float Height { get; set; }

    private ParticleEmitterCylinderShapeComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterCylinderShapeComponent
        {
            radius = Radius,
            height = Height
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterInitRandomVelocityComponent))]
public class ParticleEmitterInitRandomVelocityComponentData : IComponentData
{
    [JsonPropertyName("minForce")]
    public Vector3 MinForce { get; set; }

    [JsonPropertyName("maxForce")]
    public Vector3 MaxForce { get; set; }

    private ParticleEmitterInitRandomVelocityComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterInitRandomVelocityComponent
        {
            minForce = MinForce,
            maxForce = MaxForce
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterInitVelocityInConeComponent))]
public class ParticleEmitterInitVelocityInConeComponentData : IComponentData
{
    [JsonPropertyName("direction")]
    public Vector3 Direction { get; set; }

    [JsonPropertyName("angle")]
    public float Angle { get; set; }
    
    [JsonPropertyName("minForce")]
    public float MinForce { get; set; }
    
    [JsonPropertyName("maxForce")]
    public float MaxForce { get; set; }

    private ParticleEmitterInitVelocityInConeComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterInitVelocityInConeComponent
        {
            direction = Direction,
            angle = Angle,
            minForce = MinForce,
            maxForce = MaxForce
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterInitVelocityFromPointComponent))]
public class ParticleEmitterInitVelocityFromPointComponentData : IComponentData
{
    [JsonPropertyName("origin")]
    public Vector3 Origin { get; set; }

    [JsonPropertyName("minForce")]
    public float MinForce { get; set; }
    
    [JsonPropertyName("maxForce")]
    public float MaxForce { get; set; }

    private ParticleEmitterInitVelocityFromPointComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterInitVelocityFromPointComponent
        {
            origin = Origin,
            minForce = MinForce,
            maxForce = MaxForce
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterAddLinearForceComponent))]
public class ParticleEmitterAddLinearForceComponentData : IComponentData
{
    [JsonPropertyName("worldSpace")]
    public bool WorldSpace { get; set; }

    [JsonPropertyName("force")]
    public Vector3 Force { get; set; }

    private ParticleEmitterAddLinearForceComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterAddLinearForceComponent
        {
            worldSpace = WorldSpace,
            force = Force
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterAddNoiseForceComponent))]
public class ParticleEmitterAddNoiseForceComponentData : IComponentData
{
    [JsonPropertyName("worldSpace")]
    public bool WorldSpace { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; } = 1337;

    [JsonPropertyName("scroll")]
    public Vector3 Scroll { get; set; }

    [JsonPropertyName("frequency")]
    public float Frequency { get; set; }

    [JsonPropertyName("magnitude")]
    public float Magnitude { get; set; }

    private ParticleEmitterAddNoiseForceComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterAddNoiseForceComponent
        {
            worldSpace = WorldSpace,
            seed = Seed,
            scroll = Scroll,
            frequency = Frequency,
            magnitude = Magnitude
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterAttractionForceComponent))]
public class ParticleEmitterAttractionForceComponentData : IComponentData
{
    [JsonPropertyName("worldSpace")]
    public bool WorldSpace { get; set; }

    [JsonPropertyName("target")]
    public Vector3 Target { get; set; }

    [JsonPropertyName("force")]
    public float Force { get; set; }

    private ParticleEmitterAttractionForceComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterAttractionForceComponent
        {
            worldSpace = WorldSpace,
            target = Target,
            force = Force
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}

[SerializedComponent(nameof(ParticleEmitterSpriteRenderComponent))]
public class ParticleEmitterSpriteRenderComponentData : IComponentData
{
    [JsonPropertyName("sortBias")]
    public float SortBias { get; set; }
    
    [JsonPropertyName("material")]
    public string? Material { get; set; }
    
    [JsonPropertyName("colorOverLifetimeCurveType")]
    public CurveInterpolationMode ColorOverLifetimeCurveType { get; set; } = CurveInterpolationMode.Linear;
    
    [JsonPropertyName("colorOverLifetimeCurve")]
    public ColorAnimationCurve.CurvePoint[]? ColorOverLifetimeCurve { get; set; }
    
    [JsonPropertyName("sizeOverLifetimeCurveType")]
    public CurveInterpolationMode SizeOverLifetimeCurveType { get; set; } = CurveInterpolationMode.Linear;
    
    [JsonPropertyName("sizeOverLifetimeCurve")]
    public Vector2AnimationCurve.CurvePoint[]? SizeOverLifetimeCurve { get; set; }
    
    [JsonPropertyName("sheetRows")]
    public int SheetRows { get; set; } = 1;
    
    [JsonPropertyName("sheetColumns")]
    public int SheetColumns { get; set; } = 1;
    
    [JsonPropertyName("sheetCycles")]
    public int SheetCycles { get; set; } = 1;

    private ParticleEmitterSpriteRenderComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ParticleEmitterSpriteRenderComponent
        {
            sortBias = SortBias,
            material = game.Resources.Load<Material>(Material!),
            colorOverLifetime = new ColorAnimationCurve(ColorOverLifetimeCurveType, ColorOverLifetimeCurve!),
            sizeOverLifetime = new Vector2AnimationCurve(SizeOverLifetimeCurveType, SizeOverLifetimeCurve!),
            sheetRows = SheetRows,
            sheetColumns = SheetColumns,
            sheetCycles = SheetCycles
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}