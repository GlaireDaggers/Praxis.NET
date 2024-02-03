using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public struct SimpleCharacterStateComponent
{
    public Vector3 velocity;
    public CharacterController characterController;
    public bool grounded;
}

[ExecuteAfter(typeof(PhysicsSystem))]
[ExecuteBefore(typeof(CameraFollowSystem))]
public class SimpleCharacterMovementSystem : PraxisSystem
{
    [ExecuteAfter(typeof(BasicForwardRenderer))]
    private class CharacterDebugGizmoSystem : DebugGizmoSystem
    {
        private Filter _characters;

        public CharacterDebugGizmoSystem(WorldContext context) : base(context)
        {
            _characters = new FilterBuilder(World)
                .Include<SimpleCharacterStateComponent>()
                .Build();
        }

        protected override void DrawGizmos()
        {
            base.DrawGizmos();

            foreach (var entity in _characters.Entities)
            {
                var state = World.Get<SimpleCharacterStateComponent>(entity);
                DrawCapsuleGizmo(state.characterController.Position, state.characterController.Height,
                    state.characterController.Radius, Quaternion.Identity, Color.Cyan);
            }
        }
    }

    [ExecuteBefore(typeof(CleanupSystem))]
    public class SimpleCharacterMovementCleanupSystem(WorldContext context) : PraxisSystem(context)
    {
        public override SystemExecutionStage ExecutionStage => SystemExecutionStage.PostUpdate;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            foreach (var msg in World.GetMessages<DestroyEntity>())
            {
                if (World.Has<SimpleCharacterStateComponent>(msg.entity))
                {
                    var comp = World.Get<SimpleCharacterStateComponent>(msg.entity);
                    comp.characterController.Dispose();
                    World.Remove<SimpleCharacterStateComponent>(msg.entity);
                }
            }
        }
    }

    private PhysicsSystem _physics;
    private Filter _initFilter;
    private Filter _updateFilter;
    private Filter _cleanupFilter;

    private Filter _cameraFilter;

    public SimpleCharacterMovementSystem(WorldContext context) : base(context)
    {
        // automatically install cleanup system
        new SimpleCharacterMovementCleanupSystem(context);
        new CharacterDebugGizmoSystem(context);

        _physics = context.GetSystem<PhysicsSystem>()!;

        _initFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<SimpleCharacterComponent>()
            .Exclude<SimpleCharacterStateComponent>()
            .Build("SimpleCharacterMovementSystem.initFilter");

        _updateFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<SimpleCharacterComponent>()
            .Include<SimpleCharacterStateComponent>()
            .Build("SimpleCharacterMovementSystem.updateFilter");

        _cleanupFilter = new FilterBuilder(World)
            .Include<SimpleCharacterStateComponent>()
            .Exclude<SimpleCharacterComponent>()
            .Build();

        _cameraFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<CameraComponent>()
            .Build("SimpleCharacterMovementSystem.cameraFilter");
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // initialize characters
        foreach (var entity in _initFilter.Entities)
        {
            var transformComp = World.Get<TransformComponent>(entity);
            var characterComp = World.Get<SimpleCharacterComponent>(entity);

            var characterController = new CharacterController(_physics, characterComp.height, characterComp.radius, characterComp.skinWidth,
                characterComp.maxSlope, characterComp.collisionMask, entity)
            {
                Position = transformComp.position
            };

            World.Set(entity, new SimpleCharacterStateComponent
            {
                characterController = characterController
            });
        }

        // update characters
        foreach (var entity in _updateFilter.Entities)
        {
            var transformComp = World.Get<TransformComponent>(entity);
            var characterComp = World.Get<SimpleCharacterComponent>(entity);
            var stateComp = World.Get<SimpleCharacterStateComponent>(entity);

            // update settings
            if (characterComp.height != stateComp.characterController.Height)
            {
                stateComp.characterController.Height = characterComp.height;
            }

            if (characterComp.radius != stateComp.characterController.Radius)
            {
                stateComp.characterController.Radius = characterComp.radius;
            }

            if (characterComp.maxSlope != stateComp.characterController.MaxSlope)
            {
                stateComp.characterController.MaxSlope = characterComp.maxSlope;
            }

            Vector3 inputVel;

            if (_cameraFilter.Count > 0)
            {
                var camera = _cameraFilter.FirstEntity;
                var camTransform = World.Get<TransformComponent>(camera);
                var rotMatrix = Matrix.CreateFromQuaternion(camTransform.rotation);
                var fwd = Vector3.TransformNormal(-Vector3.UnitZ, rotMatrix);
                fwd.Y = 0f;
                fwd = Vector3.Normalize(fwd);
                var right = Vector3.TransformNormal(Vector3.UnitX, rotMatrix);
                right.Y = 0f;
                right = Vector3.Normalize(right);

                inputVel = (fwd * Game.Input.GetAxis("Move Y")) + (right * Game.Input.GetAxis("Move X"));
            }
            else
            {
                inputVel = new Vector3(Game.Input.GetAxis("Move X"), 0f, -Game.Input.GetAxis("Move Y"));
            }

            stateComp.velocity.X = inputVel.X * characterComp.moveSpeed;
            stateComp.velocity.Z = inputVel.Z * characterComp.moveSpeed;

            if (stateComp.grounded && Game.Input.GetButton("Jump") == ButtonPhase.Pressed)
            {
                stateComp.velocity.Y = characterComp.jumpForce;
            }

            // move character
            var flags = stateComp.characterController.Move(stateComp.velocity, deltaTime);
            transformComp.position = stateComp.characterController.Position;

            stateComp.grounded = flags.HasFlag(CharacterCollisionFlags.CollideBelow);

            if (flags.HasFlag(CharacterCollisionFlags.CollideBelow) && stateComp.velocity.Y < 0f)
            {
                stateComp.velocity.Y = -1f;
            }
            else
            {
                stateComp.velocity.Y -= 10f * deltaTime;
            }
            
            World.Set(entity, stateComp);
            World.Set(entity, transformComp);
        }

        // clean up states for any entity which had its SimpleCharacterComponent removed
        foreach (var entity in _cleanupFilter.Entities)
        {
            var comp = World.Get<SimpleCharacterStateComponent>(entity);
            comp.characterController.Dispose();
            World.Remove<SimpleCharacterStateComponent>(entity);
        }
    }
}
