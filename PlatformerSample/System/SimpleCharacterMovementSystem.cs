using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public struct SimpleCharacterStateComponent
{
    public Entity visual;
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

            var childMesh = World.FindTaggedEntityInChildren("Mesh", entity) ?? throw new NullReferenceException();

            World.Set(entity, new SimpleCharacterStateComponent
            {
                characterController = characterController,
                visual = childMesh
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

            Vector3 inputDir;

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

                inputDir = (fwd * Game.Input.GetAxis("Move Y")) + (right * Game.Input.GetAxis("Move X"));
            }
            else
            {
                inputDir = new Vector3(Game.Input.GetAxis("Move X"), 0f, -Game.Input.GetAxis("Move Y"));
            }

            stateComp.velocity.X = inputDir.X * characterComp.moveSpeed;
            stateComp.velocity.Z = inputDir.Z * characterComp.moveSpeed;

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

            // rotate character to face input direction
            if (inputDir.LengthSquared() > 0f)
            {
                Vector3 facing = Vector3.Normalize(inputDir);
                Matrix lookAt = Matrix.CreateLookAt(Vector3.Zero, facing, Vector3.UnitY);
                lookAt.Decompose(out _, out var lookRot, out _);
                transformComp.rotation = Quaternion.Slerp(transformComp.rotation, Quaternion.Inverse(lookRot), 1f - MathF.Pow(0.01f, deltaTime));
            }

            // play animation based on current state
            var model = World.Get<ModelComponent>(stateComp.visual);
            var anim = World.Get<AnimationStateComponent>(stateComp.visual);

            if (!stateComp.grounded)
            {
                if (stateComp.velocity.Y > 0f)
                {
                    anim.SetAnimation(model.model.Value.GetAnimationId("jump"));
                }
                else
                {
                    anim.SetAnimation(model.model.Value.GetAnimationId("fall"));
                }
            }
            else
            {
                if (inputDir.LengthSquared() > 0f)
                {
                    anim.SetAnimation(model.model.Value.GetAnimationId("run"));
                }
                else
                {
                    anim.SetAnimation(model.model.Value.GetAnimationId("idle"));
                }
            }

            World.Set(stateComp.visual, anim);
            
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
