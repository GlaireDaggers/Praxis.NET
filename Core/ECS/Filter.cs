namespace Praxis.Core.ECS;

public class Filter
{
    public string? tag;
    public Entity FirstEntity => entitySet.AsSpan[0];
    public ReverseSpanEnumerator<Entity> Entities => new ReverseSpanEnumerator<Entity>(entitySet.AsSpan);
    public int Count => entitySet.Count;

    internal FilterSignature signature;
    internal IndexableSet<Entity> entitySet = new IndexableSet<Entity>();
    private World _world;

    internal Filter(World world, FilterSignature signature)
    {
        _world = world;
        this.signature = signature;
    }

    public bool Contains(in Entity entity)
    {
        return entitySet.Contains(entity);
    }

    internal void Check(in Entity entity)
    {
        foreach (var type in signature.Included.AsSpan)
        {
            if (!_world.Has(entity, type))
            {
                entitySet.Remove(entity);
                return;
            }
        }

        foreach (var type in signature.Excluded.AsSpan)
        {
            if (_world.Has(entity, type))
            {
                entitySet.Remove(entity);
                return;
            }
        }

        entitySet.Add(entity);
    }

    public Entity GetRandomEntity(Random rng)
    {
        return entitySet.AsSpan[rng.Next(entitySet.Count)];
    }
}
