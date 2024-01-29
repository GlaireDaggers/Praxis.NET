namespace Praxis.Core;

using Praxis.Core.ECS;

/// <summary>
/// Helper system which responds to DestroyEntity messages and cleans up entities (including children attached via ChildOf)
/// </summary>
public class CleanupSystem : PraxisSystem
{
    public override SystemExecutionStage ExecutionStage => SystemExecutionStage.PostUpdate;

    public CleanupSystem(WorldContext context) : base(context)
    {
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var msg in World.GetMessages<DestroyEntity>())
        {
            DestroyEntity(msg.entity);
        }
    }

    private void DestroyEntity(in Entity entity)
    {
        if (World.HasInRelations<ChildOf>(entity))
        {
            foreach (var child in World.GetInRelations<ChildOf>(entity))
            {
                DestroyEntity(child);
            }
        }

        if (World.HasInRelations<BelongsTo>(entity))
        {
            foreach (var child in World.GetInRelations<BelongsTo>(entity))
            {
                DestroyEntity(child);
            }
        }

        World.DestroyEntity(entity);
    }
}
