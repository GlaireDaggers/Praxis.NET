using MoonTools.ECS;

namespace Praxis.Core;

/// <summary>
/// Message which can be posted to destroy an entity and all of its children
/// </summary>
public struct DestroyEntity
{
    public Entity entity;
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

        var destroyEntities = World.ReadMessages<DestroyEntity>();

        foreach (var msg in destroyEntities)
        {
            DestroyEntity(msg.entity);
        }
    }

    private void DestroyEntity(in Entity entity)
    {
        if (World.HasInRelation<ChildOf>(entity))
        {
            foreach (var child in World.InRelations<ChildOf>(entity))
            {
                DestroyEntity(child);
            }
        }

        if (World.Has<CachedPoseComponent>(entity))
        {
            World.Get<CachedPoseComponent>(entity).Pose.Dispose();
        }

        World.Destroy(entity);
    }
}
