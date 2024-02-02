using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace Example;

public struct SimpleCharacterStateComponent
{
    public Vector3 velocity;
    public CharacterController characterController;
}

[ExecuteAfter(typeof(PhysicsSystem))]
[ExecuteBefore(typeof(CameraMovementSystem))]
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

            stateComp.velocity.X = Game.Input.GetAxis("Move X") * 5f;
            stateComp.velocity.Z = -Game.Input.GetAxis("Move Y") * 5f;

            // move character
            var flags = stateComp.characterController.Move(stateComp.velocity, deltaTime);
            transformComp.position = stateComp.characterController.Position;

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
