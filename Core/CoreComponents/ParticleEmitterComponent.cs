namespace Praxis.Core;

using Microsoft.Xna.Framework;

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

public struct ParticleEmitterAddSimplexForceComponent
{
    public int seed;
    public bool worldSpace;
    public Vector3 scroll;
    public float frequency;
    public float magnitude;
}

public struct ParticleEmitterSpriteRenderComponent
{
    public float sortBias;
    public RuntimeResource<Material> material;
    public ColorAnimationCurve colorOverLifetime;
    public Vector2AnimationCurve sizeOverLifetime;
}