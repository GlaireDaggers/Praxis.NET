namespace Praxis.Core.ECS;

public class FilterBuilder
{
    private World _world;
    private IndexableSet<uint> _included = new IndexableSet<uint>();
    private IndexableSet<uint> _excluded = new IndexableSet<uint>();

    public FilterBuilder(World world)
    {
        _world = world;
    }

    public FilterBuilder Include<T>()
    {
        _included.Add(TypeId.GetTypeId<T>());
        return this;
    }

    public FilterBuilder Exclude<T>()
    {
        _excluded.Add(TypeId.GetTypeId<T>());
        return this;
    }

    public Filter Build(string? tag = null)
    {
        var signature = new FilterSignature(_included, _excluded);
        _included = new IndexableSet<uint>();
        _excluded = new IndexableSet<uint>();
        return _world.GetFilter(signature, tag);
    }
}
