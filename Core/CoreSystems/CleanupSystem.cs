namespace Praxis.Core;

using Praxis.Core.ECS;

/// <summary>
/// Message which can be posted to destroy an entity and all of its children
/// </summary>
public struct DestroyEntity
{
    public Entity entity;

    public DestroyEntity(Entity entity)
    {
        this.entity = entity;
    }
}

/// <summary>
/// Helper system which responds to DestroyEntity messages and cleans up entities (including children attached via ChildOf)
/// </summary>
public class CleanupSystem : PraxisSystem
{
    public CleanupSystem(WorldContext context) : base(context)
    {
    }

    public override void LateUpdate(float deltaTime)
    {
        base.LateUpdate(deltaTime);

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

        World.DestroyEntity(entity);
    }
}
