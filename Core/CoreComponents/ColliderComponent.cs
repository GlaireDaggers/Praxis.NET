namespace Praxis.Core;

using System.Text.Json.Serialization;
using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

[SerializedComponent(nameof(ColliderComponent))]
public class ColliderComponentData : IComponentData
{
    [JsonPropertyName("collider")]
    public ColliderDefinition? collider { get; set; }

    private ColliderComponent _component;

    public void OnDeserialize(PraxisGame game)
    {
        _component.collider = collider!;
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _component);
    }
}

public struct ColliderComponent
{
    public ColliderDefinition collider;
}

[JsonDerivedType(typeof(BoxColliderDefinition), "box")]
[JsonDerivedType(typeof(SphereColliderDefinition), "sphere")]
[JsonDerivedType(typeof(CylinderColliderDefinition), "cylinder")]
[JsonDerivedType(typeof(CapsuleColliderDefinition), "capsule")]
[JsonDerivedType(typeof(CompoundColliderDefinition), "compound")]
public abstract class ColliderDefinition
{
    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }

    [JsonPropertyName("rotation")]
    public Quaternion Rotation { get; set; }

    internal abstract TypedIndex ConstructDynamic(Simulation sim, out BodyInertia inertia);
    internal abstract TypedIndex ConstructKinematic(Simulation sim);
    internal abstract void AddToShapeBuilder(Simulation sim, ref CompoundBuilder builder);
}

public abstract class ConvexColliderDefinition<T> : ColliderDefinition
    where T : unmanaged, IConvexShape
{
    [JsonPropertyName("mass")]
    public float Mass { get; set; } = 1f;
    
    protected abstract T CreateShape();

    internal override TypedIndex ConstructDynamic(Simulation sim, out BodyInertia inertia)
    {
        var shape = CreateShape();
        inertia = shape.ComputeInertia(Mass);
        return sim.Shapes.Add(shape);
    }

    internal override TypedIndex ConstructKinematic(Simulation sim)
    {
        var shape = CreateShape();
        return sim.Shapes.Add(shape);
    }

    internal override void AddToShapeBuilder(Simulation sim, ref CompoundBuilder builder)
    {
        var shape = CreateShape();
        RigidPose localPose = new RigidPose(new System.Numerics.Vector3(Position.X, Position.Y, Position.Z),
            new System.Numerics.Quaternion(Rotation.X, Rotation.Y, Rotation.Z, Rotation.W));
        builder.Add(shape, localPose, Mass);
    }
}

public class BoxColliderDefinition : ConvexColliderDefinition<Box>
{
    [JsonPropertyName("size")]
    public Vector3 Size { get; set; }

    public BoxColliderDefinition(Vector3 size)
    {
        Size = size;
    }

    protected override Box CreateShape()
    {
        return new Box(Size.X, Size.Y, Size.Z);
    }
}

public class SphereColliderDefinition : ConvexColliderDefinition<Sphere>
{
    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    public SphereColliderDefinition(float radius)
    {
        Radius = radius;
    }

    protected override Sphere CreateShape()
    {
        return new Sphere(Radius);
    }
}

public class CylinderColliderDefinition : ConvexColliderDefinition<Cylinder>
{
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
    
    [JsonPropertyName("height")]
    public float Height { get; set; }

    public CylinderColliderDefinition(float radius, float height)
    {
        Radius = radius;
        Height = height;
    }

    protected override Cylinder CreateShape()
    {
        return new Cylinder(Radius, Height);
    }
}

public class CapsuleColliderDefinition : ConvexColliderDefinition<Capsule>
{
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
    
    [JsonPropertyName("height")]
    public float Height { get; set; }

    public CapsuleColliderDefinition(float radius, float height)
    {
        Radius = radius;
        Height = height;
    }

    protected override Capsule CreateShape()
    {
        return new Capsule(Radius, Height);
    }
}

public class CompoundColliderDefinition : ColliderDefinition
{
    public List<ColliderDefinition> children = new List<ColliderDefinition>();

    internal override TypedIndex ConstructDynamic(Simulation sim, out BodyInertia inertia)
    {
        var shapeBuilder = new CompoundBuilder(sim.BufferPool, sim.Shapes, 16);

        foreach (var child in children)
        {
            child.AddToShapeBuilder(sim, ref shapeBuilder);
        }

        shapeBuilder.BuildDynamicCompound(out var compoundChildren, out inertia);

        return sim.Shapes.Add(new Compound(compoundChildren));
    }

    internal override TypedIndex ConstructKinematic(Simulation sim)
    {
        var shapeBuilder = new CompoundBuilder(sim.BufferPool, sim.Shapes, 16);

        foreach (var child in children)
        {
            child.AddToShapeBuilder(sim, ref shapeBuilder);
        }

        shapeBuilder.BuildKinematicCompound(out var compoundChildren);

        return sim.Shapes.Add(new Compound(compoundChildren));
    }

    internal override void AddToShapeBuilder(Simulation sim, ref CompoundBuilder builder)
    {
        throw new NotImplementedException();
    }
}