namespace Praxis.Core;

using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;

public struct ColliderComponent
{
    public ColliderDefinition collider;
}

public abstract class ColliderDefinition
{
    public Vector3 position;
    public Quaternion rotation;

    internal abstract TypedIndex ConstructDynamic(Simulation sim, out BodyInertia inertia);
    internal abstract TypedIndex ConstructKinematic(Simulation sim);
    internal abstract void AddToShapeBuilder(Simulation sim, ref CompoundBuilder builder);
}

public class ConvexColliderDefinition<T> : ColliderDefinition
    where T : unmanaged, IConvexShape
{
    public float mass = 1f;
    public T shape;

    public ConvexColliderDefinition(T shape)
    {
        this.shape = shape;
    }

    internal override TypedIndex ConstructDynamic(Simulation sim, out BodyInertia inertia)
    {
        inertia = shape.ComputeInertia(mass);
        return sim.Shapes.Add(shape);
    }

    internal override TypedIndex ConstructKinematic(Simulation sim)
    {
        return sim.Shapes.Add(shape);
    }

    internal override void AddToShapeBuilder(Simulation sim, ref CompoundBuilder builder)
    {
        RigidPose localPose = new RigidPose(new System.Numerics.Vector3(position.X, position.Y, position.Z),
            new System.Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
        builder.Add(shape, localPose, mass);
    }
}

public class BoxColliderDefinition : ConvexColliderDefinition<Box>
{
    public BoxColliderDefinition(Vector3 size) : base(new Box(size.X, size.Y, size.Z))
    {
    }
}

public class SphereColliderDefinition : ConvexColliderDefinition<Sphere>
{
    public SphereColliderDefinition(float radius) : base(new Sphere(radius))
    {
    }
}

public class CylinderColliderDefinition : ConvexColliderDefinition<Cylinder>
{
    public CylinderColliderDefinition(float radius, float height) : base(new Cylinder(radius, height))
    {
    }
}

public class CapsuleColliderDefinition : ConvexColliderDefinition<Capsule>
{
    public CapsuleColliderDefinition(float radius, float height) : base(new Capsule(radius, height))
    {
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