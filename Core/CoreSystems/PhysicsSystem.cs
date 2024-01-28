namespace Praxis.Core;

using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Praxis.Core.ECS;

using Microsoft.Xna.Framework;
using System.Diagnostics;

public struct PhysicsMaterial
{
    public static readonly PhysicsMaterial Default = new PhysicsMaterial
    {
        friction = 1f,
        maxRecoveryVelocity = float.MaxValue,
        bounceFrequency = 30f,
        bounceDamping = 1f
    };

    public float friction;
    public float maxRecoveryVelocity;
    public float bounceFrequency;
    public float bounceDamping;
}

[ExecuteBefore(typeof(CalculateTransformSystem))]
[ExecuteBefore(typeof(CleanupSystem))]
public class PhysicsSystem : PraxisSystem
{
    struct RigidbodyStateComponent
    {
        public TypedIndex shape;
        public BodyHandle body;
    }

    struct StaticRigidbodyStateComponent
    {
        public TypedIndex shape;
        public StaticHandle body;
    }

    struct ConstraintStateComponent
    {
        public BodyHandle other;
        public ConstraintHandle constraint;
    }

    struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public PhysicsSystem system;

        public NarrowPhaseCallbacks(PhysicsSystem system)
        {
            this.system = system;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            if (a.Mobility != CollidableMobility.Dynamic && b.Mobility != CollidableMobility.Dynamic)
                return false;

            uint maskA = a.Mobility == CollidableMobility.Static ? uint.MaxValue : system._bodyMasks[a.BodyHandle];
            uint maskB = b.Mobility == CollidableMobility.Static ? uint.MaxValue : system._bodyMasks[b.BodyHandle];

            return (maskA & maskB) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            var matA = pair.A.Mobility == CollidableMobility.Static ? PhysicsMaterial.Default : system._bodyMaterials[pair.A.BodyHandle];
            var matB = pair.B.Mobility == CollidableMobility.Static ? PhysicsMaterial.Default : system._bodyMaterials[pair.B.BodyHandle];

            var freq = MathF.Max(matA.bounceFrequency, matB.bounceFrequency);
            var damp = MathF.Min(matA.bounceDamping, matB.bounceDamping);

            pairMaterial.FrictionCoefficient = matA.friction * matB.friction;
            pairMaterial.MaximumRecoveryVelocity = MathF.Max(matA.maxRecoveryVelocity, matB.maxRecoveryVelocity);
            pairMaterial.SpringSettings = new SpringSettings(freq, damp);
            
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
        }

        public void Initialize(Simulation simulation)
        {
        }
    }

    struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public PhysicsSystem system;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public bool AllowSubstepsForUnconstrainedBodies => false;

        public bool IntegrateVelocityForKinematics => false;

        private Vector3Wide _gravityWideDt;

        public PoseIntegratorCallbacks(PhysicsSystem system)
        {
            this.system = system;
        }

        public void Initialize(Simulation simulation)
        {
        }

        public void IntegrateVelocity(System.Numerics.Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, System.Numerics.Vector<int> integrationMask, int workerIndex, System.Numerics.Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear += _gravityWideDt;
        }

        public void PrepareForIntegration(float dt)
        {
            _gravityWideDt = Vector3Wide.Broadcast(NumericsConversion.Convert(system._gravity) * dt);
        }
    }

    public Simulation Sim => _sim;

    private BufferPool _bufferPool = new BufferPool();
    private IThreadDispatcher _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
    private Simulation _sim;

    private Dictionary<BodyHandle, PhysicsMaterial> _bodyMaterials = new Dictionary<BodyHandle, PhysicsMaterial>();
    private Dictionary<BodyHandle, uint> _bodyMasks = new Dictionary<BodyHandle, uint>();
    private Dictionary<BodyHandle, Entity> _incomingConstraints = new Dictionary<BodyHandle, Entity>();

    private float _accum;
    private float _timestep = 1f / 60f;
    private float _maxTimestep = 0.1f;
    private float _sleepThreshold = 0.01f;
    private Vector3 _gravity = new Vector3(0f, -9.8f, 0f);

    private Filter _initBodyFilter;
    private Filter _initStaticFilter;
    private Filter _initConstraintFilter;
    private Filter _updateBodyFilter;
    private Filter _updateConstraintFilter;

    public PhysicsSystem(WorldContext context) : base(context)
    {
        _initBodyFilter = new FilterBuilder(World)
            .Include<RigidbodyComponent>()
            .Include<TransformComponent>()
            .Include<ColliderComponent>()
            .Exclude<RigidbodyStateComponent>()
            .Build();

        _initStaticFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<ColliderComponent>()
            .Exclude<RigidbodyComponent>()
            .Exclude<StaticRigidbodyStateComponent>()
            .Build();

        _initConstraintFilter = new FilterBuilder(World)
            .Include<RigidbodyStateComponent>()
            .Include<ConstraintComponent>()
            .Exclude<ConstraintStateComponent>()
            .Build();

        _updateBodyFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<RigidbodyStateComponent>()
            .Build();

        _updateConstraintFilter = new FilterBuilder(World)
            .Include<ConstraintStateComponent>()
            .Build();

        _sim = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(this), new PoseIntegratorCallbacks(this), new SolveDescription(8, 4));
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (World.HasSingleton<PhysicsConfigSingleton>())
        {
            var config = World.GetSingleton<PhysicsConfigSingleton>();
            _timestep = config.timestep;
            _maxTimestep = config.maxTimestep;
            _sleepThreshold = config.sleepThreshold;
            _gravity = config.gravity;
        }

        // init bodies & statics
        foreach (var entity in _initBodyFilter.Entities)
        {
            InitBody(entity);
        }

        foreach (var entity in _initStaticFilter.Entities)
        {
            InitStatic(entity);
        }

        // init constraints
        foreach (var entity in _initConstraintFilter.Entities)
        {
            InitConstraint(entity);
        }

        _accum += deltaTime;
        if (_accum > _maxTimestep)
        {
            _accum = _maxTimestep;
        }

        while (_accum >= _timestep)
        {
            _accum -= _timestep;
            _sim.Timestep(_timestep, _threadDispatcher);
        }

        // update bodies
        foreach (var entity in _updateBodyFilter.Entities)
        {
            UpdateBody(entity);
        }

        // update constraints
        foreach (var entity in _updateConstraintFilter.Entities)
        {
            UpdateConstraint(entity);
        }
    }

    public override void PostUpdate(float deltaTime)
    {
        base.PostUpdate(deltaTime);

        foreach (var msg in World.GetMessages<DestroyEntity>())
        {
            OnDestroyEntity(msg.entity);
        }
    }

    public void RegisterBody(in BodyHandle handle, uint collisionMask, in PhysicsMaterial material)
    {
        _bodyMaterials[handle] = material;
        _bodyMasks[handle] = collisionMask;
    }

    public void UnregisterBody(in BodyHandle handle)
    {
        _bodyMaterials.Remove(handle);
        _bodyMasks.Remove(handle);
    }

    public void SetPose(in Entity entity, in Vector3 position, in Quaternion rotation)
    {
        TransformComponent transform = World.Get<TransformComponent>(entity);
        RigidbodyStateComponent state = World.Get<RigidbodyStateComponent>(entity);

        // if any entities are attached via a BelongsTo relationship, make sure they move along with this entity
        // (for example: if you move a Car entity and it has wheels attached to it via BelongsTo, they should move along with the car)
        if (World.HasInRelations<BelongsTo>(entity))
        {
            foreach (var child in World.GetInRelations<BelongsTo>(entity))
            {
                if (World.Has<TransformComponent>(child) && World.Has<RigidbodyStateComponent>(child))
                {
                    var childTransform = World.Get<TransformComponent>(child);
                    var childState = World.Get<RigidbodyStateComponent>(child);
                    Vector3 posOffset = childTransform.position - transform.position;
                    Quaternion rotOffset = Quaternion.Concatenate(Quaternion.Inverse(transform.rotation), childTransform.rotation);
                    Vector3 newPos = position + posOffset;
                    Quaternion newRot = Quaternion.Concatenate(rotation, rotOffset);
                    _sim.Bodies[childState.body].Pose = new RigidPose(NumericsConversion.Convert(newPos), NumericsConversion.Convert(newRot));
                }
            }
        }

        _sim.Bodies[state.body].Pose = new RigidPose(NumericsConversion.Convert(position), NumericsConversion.Convert(rotation));
    }

    private void InitBody(in Entity entity)
    {
        // ChildOf relations are not supported for physics simulation
        Debug.Assert(!World.HasOutRelations<ChildOf>(entity), "Physics entity has ChildOf relation, but this is unsupported. Consider using BelongsTo instead");

        RigidbodyComponent rigidbody = World.Get<RigidbodyComponent>(entity);
        ColliderComponent collider = World.Get<ColliderComponent>(entity);
        TransformComponent transform = World.Get<TransformComponent>(entity);

        var pose = new RigidPose(
            NumericsConversion.Convert(transform.position),
            NumericsConversion.Convert(transform.rotation)  
        );

        TypedIndex shape;
        BodyHandle body;

        if (rigidbody.isKinematic)
        {
            shape = collider.collider.ConstructKinematic(_sim);
            body = _sim.Bodies.Add(BodyDescription.CreateKinematic(pose, shape, _sleepThreshold));
        }
        else
        {
            shape = collider.collider.ConstructDynamic(_sim, out var inertia);
            
            // https://forum.bepuentertainment.com/viewtopic.php?t=2722
            if (rigidbody.lockRotationX)
            {
                inertia.InverseInertiaTensor.XX = 0f;
                inertia.InverseInertiaTensor.YX = 0f;
                inertia.InverseInertiaTensor.ZX = 0f;
            }
            if (rigidbody.lockRotationY)
            {
                inertia.InverseInertiaTensor.YX = 0f;
                inertia.InverseInertiaTensor.YY = 0f;
                inertia.InverseInertiaTensor.ZY = 0f;
            }
            if (rigidbody.lockRotationZ)
            {
                inertia.InverseInertiaTensor.ZX = 0f;
                inertia.InverseInertiaTensor.ZY = 0f;
                inertia.InverseInertiaTensor.ZZ = 0f;
            }

            body = _sim.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, shape, _sleepThreshold));
        }

        _bodyMaterials.Add(body, rigidbody.material);
        _bodyMasks.Add(body, rigidbody.collisionMask);

        World.Set(entity, new RigidbodyStateComponent
        {
            body = body,
            shape = shape
        });
    }

    private void InitStatic(in Entity entity)
    {
        // ChildOf relations are not supported for physics simulation
        Debug.Assert(!World.HasOutRelations<ChildOf>(entity), "Physics entity has ChildOf relation, but this is unsupported. Consider using BelongsTo instead");

        ColliderComponent collider = World.Get<ColliderComponent>(entity);
        TransformComponent transform = World.Get<TransformComponent>(entity);

        var pose = new RigidPose(
            NumericsConversion.Convert(transform.position),
            NumericsConversion.Convert(transform.rotation)  
        );

        var shape = collider.collider.ConstructKinematic(_sim);
        var body = _sim.Statics.Add(new StaticDescription(pose, shape));

        World.Set(entity, new StaticRigidbodyStateComponent
        {
            body = body,
            shape = shape
        });
    }

    private void InitConstraint(in Entity entity)
    {
        RigidbodyStateComponent state = World.Get<RigidbodyStateComponent>(entity);
        ConstraintComponent constraint = World.Get<ConstraintComponent>(entity);
        RigidbodyStateComponent otherState = World.Get<RigidbodyStateComponent>(constraint.other);

        var handle = constraint.constraint.Construct(_sim, state.body, otherState.body);

        World.Set(entity, new ConstraintStateComponent
        {
            other = otherState.body,
            constraint = handle
        });

        _incomingConstraints.Add(otherState.body, entity);
    }

    private void UpdateBody(in Entity entity)
    {
        TransformComponent transform = World.Get<TransformComponent>(entity);
        RigidbodyStateComponent state = World.Get<RigidbodyStateComponent>(entity);

        if (!World.Has<RigidbodyComponent>(entity))
        {
            // if the rigidbody component gets removed, clean up & remove state
            _sim.Shapes.Remove(state.shape);
            RemoveBody(state.body);
            World.Remove<RigidbodyStateComponent>(entity);
        }
        else
        {
            RigidbodyComponent rigidbody = World.Get<RigidbodyComponent>(entity);

            var body = _sim.Bodies[state.body];

            _bodyMaterials[state.body] = rigidbody.material;
            _bodyMasks[state.body] = rigidbody.collisionMask;

            if (body.Awake)
            {
                transform.position = new Vector3(body.Pose.Position.X, body.Pose.Position.Y, body.Pose.Position.Z);
                transform.rotation = new Quaternion(body.Pose.Orientation.X, body.Pose.Orientation.Y, body.Pose.Orientation.Z, body.Pose.Orientation.W);

                World.Set(entity, transform);
            }
        }
    }

    private void UpdateConstraint(in Entity entity)
    {
        ConstraintStateComponent state = World.Get<ConstraintStateComponent>(entity);

        if (!World.Has<ConstraintComponent>(entity))
        {
            // if the constraint component gets removed, clean up & remove state
            _sim.Solver.Remove(state.constraint);
            if (_incomingConstraints.ContainsKey(state.other))
            {
                _incomingConstraints.Remove(state.other);
            }
            World.Remove<ConstraintStateComponent>(entity);
        }
        else
        {
            // TODO: should this be skipped if it hasn't changed?
            ConstraintComponent constraint = World.Get<ConstraintComponent>(entity);
            constraint.constraint.Update(_sim, state.constraint);
        }
    }

    private void OnDestroyEntity(in Entity entity)
    {
        if (World.Has<RigidbodyStateComponent>(entity))
        {
            RigidbodyStateComponent stateComp = World.Get<RigidbodyStateComponent>(entity);
            _sim.Shapes.Remove(stateComp.shape);
            RemoveBody(stateComp.body);
        }

        if (World.Has<ConstraintStateComponent>(entity))
        {
            ConstraintStateComponent stateComp = World.Get<ConstraintStateComponent>(entity);
            _sim.Solver.Remove(stateComp.constraint);
        }

        if (World.Has<StaticRigidbodyStateComponent>(entity))
        {
            StaticRigidbodyStateComponent stateComp = World.Get<StaticRigidbodyStateComponent>(entity);
            _sim.Shapes.Remove(stateComp.shape);
            _sim.Statics.Remove(stateComp.body);
        }
    }

    private void RemoveBody(BodyHandle body)
    {
        _bodyMaterials.Remove(body);
        _bodyMasks.Remove(body);

        // remove any constraints that linked other entities to this one
        if (_incomingConstraints.ContainsKey(body))
        {
            var other = _incomingConstraints[body];
            var constraintState = World.Get<ConstraintStateComponent>(other);
            _sim.Solver.Remove(constraintState.constraint);
            World.Remove<ConstraintStateComponent>(other);
            _incomingConstraints.Remove(body);
        }

        _sim.Bodies.Remove(body);
    }
}
