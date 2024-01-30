namespace Praxis.Core;

using System.Diagnostics;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct Particle
{
    public float lifetime;
    public float maxLifetime;
    public Vector3 position;
    public float angle;
    public Vector3 velocity;
    public float angularVelocity;
}

public struct ParticleEmitterStateComponent
{
    public Particle[] particles;
    public int particleCount;
    public float burstTimer;
    public int burstCount;
}

public class ParticleEmitterSystem : PraxisSystem
{
    public const int MAXPARTICLES = 8192;

    [ExecuteAfter(typeof(BasicForwardRenderer))]
    private class ParticleEmitterDebugGizmoSystem : DebugGizmoSystem
    {
        private Filter _filter;

        public ParticleEmitterDebugGizmoSystem(WorldContext context) : base(context)
        {
            _filter = new FilterBuilder(World)
                .Include<CachedMatrixComponent>()
                .Include<ParticleEmitterComponent>()
                .Include<ParticleEmitterStateComponent>()
                .Build();
        }

        protected override void DrawGizmos()
        {
            base.DrawGizmos();

            foreach (var entity in _filter.Entities)
            {
                var transform = World.Get<CachedMatrixComponent>(entity);
                var emitter = World.Get<ParticleEmitterComponent>(entity);
                var state = World.Get<ParticleEmitterStateComponent>(entity);

                for (int i = 0; i < state.particleCount; i++)
                {
                    Vector3 p = emitter.worldSpace ? state.particles[i].position : Vector3.Transform(state.particles[i].position, transform.transform);
                    DrawSphereGizmo(p, 0.1f, Color.White);
                }
            }
        }
    }

    private Filter _initParticles;
    private Filter _updateParticles;
    private Random _rng;

    private FastNoiseLite _noise1;
    private FastNoiseLite _noise2;
    private FastNoiseLite _noise3;

    public ParticleEmitterSystem(WorldContext context) : base(context)
    {
        // install gizmo visualizer
        new ParticleEmitterDebugGizmoSystem(context);

        _rng = new Random();

        _initParticles = new FilterBuilder(World)
            .Include<ParticleEmitterComponent>()
            .Exclude<ParticleEmitterStateComponent>()
            .Build("ParticleEmitterSystem.initParticles");

        _updateParticles = new FilterBuilder(World)
            .Include<ParticleEmitterComponent>()
            .Include<ParticleEmitterStateComponent>()
            .Build("ParticleEmitterSystem.updateParticles");

        _noise1 = new FastNoiseLite();
        _noise1.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noise2 = new FastNoiseLite();
        _noise2.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noise3 = new FastNoiseLite();
        _noise3.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var entity in _initParticles.Entities)
        {
            var emitter = World.Get<ParticleEmitterComponent>(entity);
            Debug.Assert(emitter.maxParticles <= MAXPARTICLES);
            World.Set(entity, new ParticleEmitterStateComponent
            {
                particles = new Particle[emitter.maxParticles],
                particleCount = 0,
                burstTimer = 0f,
                burstCount = 0
            });
        }

        foreach (var entity in _updateParticles.Entities)
        {
            var transform = World.Get<CachedMatrixComponent>(entity);
            var emitter = World.Get<ParticleEmitterComponent>(entity);
            var emitterState = World.Get<ParticleEmitterStateComponent>(entity);

            if (emitterState.burstCount < emitter.maxBurstCount || emitter.maxBurstCount <= 0)
            {
                emitterState.burstTimer -= deltaTime;
                if (emitterState.burstTimer <= 0f)
                {
                    emitterState.burstTimer = emitter.burstInterval;
                    EmitBurst(entity, transform, emitter, ref emitterState);
                }
            }

            if (World.Has<ParticleEmitterAddLinearForceComponent>(entity))
            {
                var addLinearForce = World.Get<ParticleEmitterAddLinearForceComponent>(entity);
                
                Vector3 force = addLinearForce.force;
                if (emitter.worldSpace && !addLinearForce.worldSpace)
                {
                    // transform vector from local to world
                    force = Vector3.TransformNormal(force, transform.transform);
                }
                else if (addLinearForce.worldSpace && !emitter.worldSpace)
                {
                    // transform vector from world to local
                    force = Vector3.TransformNormal(force, Matrix.Invert(transform.transform));
                }

                for (int i = 0; i < emitterState.particleCount; i++)
                {
                    emitterState.particles[i].velocity += force * deltaTime;
                }
            }

            if (World.Has<ParticleEmitterAddSimplexForceComponent>(entity))
            {
                var addSimplexForce = World.Get<ParticleEmitterAddSimplexForceComponent>(entity);

                _noise1.SetSeed(addSimplexForce.seed);
                _noise2.SetSeed(addSimplexForce.seed + 1);
                _noise3.SetSeed(addSimplexForce.seed + 2);

                _noise1.SetFrequency(addSimplexForce.frequency);
                _noise2.SetFrequency(addSimplexForce.frequency);
                _noise3.SetFrequency(addSimplexForce.frequency);

                Matrix forceTransform = Matrix.Identity;
                
                if (emitter.worldSpace && !addSimplexForce.worldSpace)
                {
                    // transform vector from local to world
                    forceTransform = transform.transform;
                }
                else if (addSimplexForce.worldSpace && !emitter.worldSpace)
                {
                    // transform vector from world to local
                    forceTransform = Matrix.Invert(transform.transform);
                }

                for (int i = 0; i < emitterState.particleCount; i++)
                {
                    ref var particle = ref emitterState.particles[i];
                    var samplePos = particle.position + (addSimplexForce.scroll * deltaTime);
                    float x = _noise1.GetNoise(samplePos.X, samplePos.Y, samplePos.Z);
                    float y = _noise2.GetNoise(samplePos.X, samplePos.Y, samplePos.Z);
                    float z = _noise3.GetNoise(samplePos.X, samplePos.Y, samplePos.Z);
                    particle.velocity += Vector3.TransformNormal(new Vector3(x, y, z), forceTransform) * addSimplexForce.magnitude * deltaTime;
                }
            }

            for (int i = 0; i < emitterState.particleCount; i++)
            {
                ref var particle = ref emitterState.particles[i];

                particle.lifetime += deltaTime;

                if (particle.lifetime >= particle.maxLifetime)
                {
                    emitterState.particles[i] = emitterState.particles[--emitterState.particleCount];
                    i--;
                }
                else
                {
                    particle.position += particle.velocity * deltaTime;
                    particle.angle += particle.angularVelocity * deltaTime;

                    // https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
                    particle.velocity *= MathF.Pow(1f - emitter.linearDamping, deltaTime);
                    particle.angularVelocity *= MathF.Pow(1f - emitter.angularDamping, deltaTime);
                }
            }

            World.Set(entity, emitterState);
        }
    }

    private void EmitBurst(in Entity entity, in CachedMatrixComponent transform, in ParticleEmitterComponent emitter, ref ParticleEmitterStateComponent state)
    {
        int idxStart = state.particleCount;
        int idxEnd = idxStart + emitter.particlesPerBurst;

        if (idxEnd > emitter.maxParticles)
        {
            idxEnd = emitter.maxParticles;
        }

        state.particleCount = idxEnd;

        for (int i = idxStart; i < idxEnd; i++)
        {
            state.particles[i] = new Particle
            {
                lifetime = 0f,
                maxLifetime = RandomRange(emitter.minParticleLifetime, emitter.maxParticleLifetime),
                angle = RandomRange(emitter.minAngle, emitter.maxAngle),
                angularVelocity = RandomRange(emitter.minAngularVelocity, emitter.maxAngularVelocity)
            };
        }

        if (World.Has<ParticleEmitterBoxShapeComponent>(entity))
        {
            var shape = World.Get<ParticleEmitterBoxShapeComponent>(entity);
            
            for (int i = idxStart; i < idxEnd; i++)
            {
                state.particles[i].position = new Vector3(
                    RandomRange(-shape.extents.X, shape.extents.X),
                    RandomRange(-shape.extents.Y, shape.extents.Y),
                    RandomRange(-shape.extents.Z, shape.extents.Z)
                );
            }
        }
        else if (World.Has<ParticleEmitterSphereShapeComponent>(entity))
        {
            var shape = World.Get<ParticleEmitterSphereShapeComponent>(entity);
            
            for (int i = idxStart; i < idxEnd; i++)
            {
                Vector3 vec = Vector3.Normalize(new Vector3(
                    RandomRange(-1f, 1f),
                    RandomRange(-1f, 1f),
                    RandomRange(-1f, 1f)
                ));

                state.particles[i].position = vec * RandomRange(0f, shape.radius);
            }
        }
        else if (World.Has<ParticleEmitterCylinderShapeComponent>(entity))
        {
            var shape = World.Get<ParticleEmitterCylinderShapeComponent>(entity);
            
            for (int i = idxStart; i < idxEnd; i++)
            {
                Vector3 vec = Vector3.Normalize(new Vector3(
                    RandomRange(-1f, 1f),
                    RandomRange(-1f, 1f),
                    0f
                ));

                Vector3 offset = Vector3.UnitX * RandomRange(-shape.height, shape.height);

                state.particles[i].position = (vec * RandomRange(0f, shape.radius)) + offset;
            }
        }

        if (World.Has<ParticleEmitterInitRandomVelocityComponent>(entity))
        {
            var init = World.Get<ParticleEmitterInitRandomVelocityComponent>(entity);

            for (int i = idxStart; i < idxEnd; i++)
            {
                Vector3 vec = Vector3.Normalize(new Vector3(
                    RandomRange(init.minForce.X, init.maxForce.X),
                    RandomRange(init.minForce.Y, init.maxForce.Y),
                    RandomRange(init.minForce.Z, init.maxForce.Z)
                ));

                state.particles[i].velocity += vec;
            }
        }
        
        if (World.Has<ParticleEmitterInitVelocityFromPointComponent>(entity))
        {
            var init = World.Get<ParticleEmitterInitVelocityFromPointComponent>(entity);

            for (int i = idxStart; i < idxEnd; i++)
            {
                Vector3 vec = Vector3.Normalize(
                    state.particles[i].position - init.origin
                ) * RandomRange(init.minForce, init.maxForce);

                state.particles[i].velocity += vec;
            }
        }
        
        if (World.Has<ParticleEmitterInitVelocityInConeComponent>(entity))
        {
            var init = World.Get<ParticleEmitterInitVelocityInConeComponent>(entity);

            var coneAngle = MathHelper.ToRadians(init.angle);
            var cosConeAngle = MathF.Cos(coneAngle);

            for (int i = idxStart; i < idxEnd; i++)
            {
                float y = RandomRange(0f, 1f) * (1f - cosConeAngle) + cosConeAngle;
                float y2 = y * y;
                float sqrt_one_minus_y2 = MathF.Sqrt(1 - y2);
                float phi = RandomRange(0f, 1f) * 2f * MathF.PI;
                float x = sqrt_one_minus_y2 * MathF.Cos(phi);
                float z = sqrt_one_minus_y2 * MathF.Sin(phi);

                Vector3 dir = new Vector3(x, y, z);

                if (init.direction.X == 0f && init.direction.Z == 0f)
                {
                    dir.Y = MathF.CopySign(dir.Y, init.direction.Y);
                }
                else
                {
                    Matrix rot = Matrix.CreateLookAt(Vector3.Zero, init.direction, Vector3.UnitY);
                    dir = Vector3.TransformNormal(dir, rot);
                }

                state.particles[i].velocity += dir * RandomRange(init.minForce, init.maxForce);
            }
        }

        if (emitter.worldSpace)
        {
            for (int i = idxStart; i < idxEnd; i++)
            {
                ref var particle = ref state.particles[i];
                particle.position = Vector3.Transform(particle.position, transform.transform);
                particle.velocity = Vector3.TransformNormal(particle.velocity, transform.transform);
            }
        }
    }

    private float RandomRange(float min, float max)
    {
        return MathHelper.Lerp(min, max, (float)_rng.NextDouble());
    }
}
