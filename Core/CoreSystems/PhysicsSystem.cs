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
using Matrix = Microsoft.Xna.Framework.Matrix;
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
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            var matA = pair.A.Mobility == CollidableMobility.Static ? system._staticMaterials[pair.A.StaticHandle] : system._bodyMaterials[pair.A.BodyHandle];
            var matB = pair.B.Mobility == CollidableMobility.Static ? system._staticMaterials[pair.B.StaticHandle] : system._bodyMaterials[pair.B.BodyHandle];

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
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public bool AllowSubstepsForUnconstrainedBodies => false;

        public bool IntegrateVelocityForKinematics => false;

        public System.Numerics.Vector3 gravity;

        private Vector3Wide _gravityWideDt;

        public void Initialize(Simulation simulation)
        {
        }

        public void IntegrateVelocity(System.Numerics.Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, System.Numerics.Vector<int> integrationMask, int workerIndex, System.Numerics.Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear += _gravityWideDt;
        }

        public void PrepareForIntegration(float dt)
        {
            _gravityWideDt = Vector3Wide.Broadcast(gravity * dt);
        }
    }

    public Simulation Sim => _sim;

    private BufferPool _bufferPool = new BufferPool();
    private IThreadDispatcher _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
    private CompoundBuilder _shapeBuilder;
    private Simulation _sim;

    private Dictionary<BodyHandle, PhysicsMaterial> _bodyMaterials = new Dictionary<BodyHandle, PhysicsMaterial>();
    private Dictionary<StaticHandle, PhysicsMaterial> _staticMaterials = new Dictionary<StaticHandle, PhysicsMaterial>();

    private float _accum;
    private float _timestep = 1f / 60f;
    private float _maxTimestep = 0.1f;
    private float _sleepThreshold = 0.01f;

    private Filter _initFilter;
    private Filter _updateFilter;

    public PhysicsSystem(WorldContext context) : base(context)
    {
        _initFilter = new FilterBuilder(World)
            .Include<RigidbodyComponent>()
            .Include<TransformComponent>()
            .Exclude<RigidbodyStateComponent>()
            .Exclude<StaticRigidbodyStateComponent>()
            .Build();

        _initFilter.tag = "init rigidbody";

        _updateFilter = new FilterBuilder(World)
            .Include<RigidbodyComponent>()
            .Include<TransformComponent>()
            .Include<RigidbodyStateComponent>()
            .Build();

        _sim = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(this), new PoseIntegratorCallbacks()
        {
            gravity = new System.Numerics.Vector3(0f, -10f, 0f)
        }, new SolveDescription(8, 8));

        _shapeBuilder = new CompoundBuilder(_bufferPool, _sim.Shapes, 16);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // init bodies
        foreach (var entity in _initFilter.Entities)
        {
            InitEntity(entity);
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
        foreach (var entity in _updateFilter.Entities)
        {
            UpdateEntity(entity);
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

    public void RegisterBody(in BodyHandle handle, in PhysicsMaterial material)
    {
        _bodyMaterials.Add(handle, material);
    }

    public void RegisterStatic(in StaticHandle handle, in PhysicsMaterial material)
    {
        _staticMaterials.Add(handle, material);
    }

    public void UnregisterBody(in BodyHandle handle)
    {
        _bodyMaterials.Remove(handle);
    }

    public void UnregisterStatic(in StaticHandle handle)
    {
        _staticMaterials.Remove(handle);
    }

    private void InitEntity(in Entity entity)
    {
        RigidbodyComponent rigidbody = World.Get<RigidbodyComponent>(entity);
        Matrix worldToLocal = Matrix.Invert(CalculateTransform(entity));
        GatherShapes(entity, worldToLocal, Matrix.Identity, ref _shapeBuilder, false);

        Matrix trs = CalculateTransform(entity);
        Debug.Assert(trs.Decompose(out _, out var rot, out var pos));

        RigidPose pose = new RigidPose(Convert(pos), Convert(rot));

        if (rigidbody.isStatic)
        {
            _shapeBuilder.BuildKinematicCompound(out var children);
            _shapeBuilder.Reset();

            var shape = _sim.Shapes.Add(new Compound(children));
            var body = _sim.Statics.Add(new StaticDescription(pose, shape));

            _staticMaterials.Add(body, rigidbody.material);

            World.Set(entity, new StaticRigidbodyStateComponent
            {
                shape = shape,
                body = body
            });
        }
        else
        {
            _shapeBuilder.BuildDynamicCompound(out var children, out var inertia);
            _shapeBuilder.Reset();

            var shape = _sim.Shapes.Add(new Compound(children));
            var body = _sim.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, shape, _sleepThreshold));

            _bodyMaterials.Add(body, rigidbody.material);

            World.Set(entity, new RigidbodyStateComponent
            {
                shape = shape,
                body = body
            });
        }
    }

    private void GatherShapes(in Entity entity, in Matrix worldToLocal, in Matrix parent, ref CompoundBuilder shapeBuilder, bool isChild)
    {
        // if a child has its own rigidbody component, don't include its collision hierarchy (let it build its own)
        if (isChild && World.Has<RigidbodyComponent>(entity))
        {
            return;
        }

        Matrix transform = CalculateLocalTransform(entity) * parent;
        Matrix localTransform = transform * worldToLocal;

        Debug.Assert(localTransform.Decompose(out var scale, out var rot, out var pos));
        var localPose = new RigidPose(Convert(pos), Convert(rot));

        if (World.Has<BoxColliderComponent>(entity))
        {
            BoxColliderComponent comp = World.Get<BoxColliderComponent>(entity);
            Vector3 size = comp.extents * 2f * scale;
            var shape = new Box(size.X, size.Y, size.Z);

            shapeBuilder.Add(shape, localPose, comp.weight);
        }

        if (World.HasInRelations<ChildOf>(entity))
        {
            foreach (var child in World.GetInRelations<ChildOf>(entity))
            {
                GatherShapes(child, worldToLocal, transform, ref shapeBuilder, true);
            }
        }
    }

    private Matrix CalculateLocalTransform(in Entity entity)
    {
        var transform = World.Get<TransformComponent>(entity);
        
        Matrix trs = Matrix.CreateScale(transform.scale)
            * Matrix.CreateFromQuaternion(transform.rotation)
            * Matrix.CreateTranslation(transform.position);

        return trs;
    }

    private Matrix CalculateTransform(in Entity entity)
    {
        var transform = World.Get<TransformComponent>(entity);
        
        Matrix trs = Matrix.CreateScale(transform.scale)
            * Matrix.CreateFromQuaternion(transform.rotation)
            * Matrix.CreateTranslation(transform.position);

        if (World.HasOutRelations<ChildOf>(entity))
        {
            var parent = World.GetFirstOutRelation<ChildOf>(entity);
            trs *= CalculateTransform(parent);
        }

        return trs;
    }

    private void UpdateEntity(in Entity entity)
    {
        Matrix worldToLocal = Matrix.Identity;

        if (World.HasOutRelations<ChildOf>(entity))
        {
            Entity parent = World.GetFirstOutRelation<ChildOf>(entity);
            worldToLocal = Matrix.Invert(CalculateTransform(parent));
        }

        RigidbodyStateComponent state = World.Get<RigidbodyStateComponent>(entity);
        var body = _sim.Bodies[state.body];

        var worldPos = new Vector3(body.Pose.Position.X, body.Pose.Position.Y, body.Pose.Position.Z);
        var worldRot = new Quaternion(body.Pose.Orientation.X, body.Pose.Orientation.Y, body.Pose.Orientation.Z, body.Pose.Orientation.W);

        Matrix localTransform = Matrix.CreateFromQuaternion(worldRot) * Matrix.CreateTranslation(worldPos);
        localTransform *= worldToLocal;

        TransformComponent transform = World.Get<TransformComponent>(entity);
        Debug.Assert(localTransform.Decompose(out _, out transform.rotation, out transform.position));

        World.Set(entity, transform);
    }

    private void OnDestroyEntity(in Entity entity)
    {
        if (World.Has<RigidbodyStateComponent>(entity))
        {
            RigidbodyStateComponent stateComp = World.Get<RigidbodyStateComponent>(entity);
            _bodyMaterials.Remove(stateComp.body);
            _sim.Shapes.Remove(stateComp.shape);
            _sim.Bodies.Remove(stateComp.body);
        }

        if (World.Has<StaticRigidbodyStateComponent>(entity))
        {
            StaticRigidbodyStateComponent stateComp = World.Get<StaticRigidbodyStateComponent>(entity);
            _staticMaterials.Remove(stateComp.body);
            _sim.Shapes.Remove(stateComp.shape);
            _sim.Statics.Remove(stateComp.body);
        }

        if (World.HasInRelations<ChildOf>(entity))
        {
            foreach (var child in World.GetInRelations<ChildOf>(entity))
            {
                OnDestroyEntity(child);
            }
        }
    }

    private System.Numerics.Vector3 Convert(Vector3 value)
    {
        return new System.Numerics.Vector3(value.X, value.Y, value.Z);
    }

    private System.Numerics.Quaternion Convert(Quaternion value)
    {
        return new System.Numerics.Quaternion(value.X, value.Y, value.Z, value.W);
    }
}
