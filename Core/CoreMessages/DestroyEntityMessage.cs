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