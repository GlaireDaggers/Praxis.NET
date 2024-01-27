namespace Praxis.Core.ECS;

using System.Runtime.InteropServices;

internal class IndexableSet<T>
    where T : struct
{
    public int Count => _list.Count;
    public Span<T> AsSpan => CollectionsMarshal.AsSpan(_list);

    private List<T> _list = new List<T>();
    private Dictionary<T, int> _set = new Dictionary<T, int>();

    public bool Contains(T data)
    {
        return _set.ContainsKey(data);
    }

    public bool Add(T data)
    {
        if (!Contains(data))
        {
            _set.Add(data, _list.Count);
            _list.Add(data);

            return true;
        }

        return false;
    }

    public bool Remove(T data)
    {
        if (Contains(data))
        {
            var index = _set[data];

            for (int i = index; i < _list.Count - 1; i++)
            {
                _list[i] = _list[i + 1];
                _set[_list[i]] = i;
            }

            _set.Remove(data);

            return true;
        }

        return false;
    }

    public void Clear()
    {
        _list.Clear();
        _set.Clear();
    }
}

internal interface IRelationStorage
{
    void RemoveEntity(in Entity entity);
}

internal class RelationStorage<T> : IRelationStorage
    where T : struct
{
    private Dictionary<(Entity, Entity), T> _relationData = new Dictionary<(Entity, Entity), T>();
    private Dictionary<Entity, IndexableSet<Entity>> _outRelationSets = new Dictionary<Entity, IndexableSet<Entity>>();
    private Dictionary<Entity, IndexableSet<Entity>> _inRelationSets = new Dictionary<Entity, IndexableSet<Entity>>();
    private Stack<IndexableSet<Entity>> _setPool = new Stack<IndexableSet<Entity>>();

    public void Set(in Entity from, in Entity to, in T data)
    {
        var relation = (from, to);

        if (_relationData.ContainsKey(relation))
        {
            _relationData[relation] = data;
            return;
        }

        if (!_outRelationSets.ContainsKey(from))
        {
            _outRelationSets[from] = GetOrCreateHashSet();
        }
        _outRelationSets[from].Add(to);

        if (!_inRelationSets.ContainsKey(from))
        {
            _inRelationSets[to] = GetOrCreateHashSet();
        }
        _inRelationSets[to].Add(from);

        _relationData.Add(relation, data);
    }

    public T Get(in Entity from, in Entity to)
    {
        return _relationData[(from, to)];
    }

    public bool Has(in Entity from, in Entity to)
    {
        return _relationData.ContainsKey((from, to));
    }

    public bool HasOutRelations(in Entity from)
    {
        return _outRelationSets.ContainsKey(from) && _outRelationSets[from].Count > 0;
    }

    public bool HasInRelations(in Entity to)
    {
        return _inRelationSets.ContainsKey(to) && _inRelationSets[to].Count > 0;
    }

    public SpanEnumerator<Entity> GetOutRelations(in Entity from)
    {
        if (_outRelationSets.TryGetValue(from, out var outRelations))
        {
            return new SpanEnumerator<Entity>(outRelations.AsSpan);
        }

        return new SpanEnumerator<Entity>();
    }

    public SpanEnumerator<Entity> GetInRelations(in Entity to)
    {
        if (_inRelationSets.TryGetValue(to, out var inRelations))
        {
            return new SpanEnumerator<Entity>(inRelations.AsSpan);
        }

        return new SpanEnumerator<Entity>();
    }

    public Entity GetFirstOutRelation(in Entity from)
    {
        return _outRelationSets[from].AsSpan[0];
    }

    public Entity GetFirstInRelation(in Entity to)
    {
        return _inRelationSets[to].AsSpan[0];
    }

    public (bool, bool) Remove(in Entity from, in Entity to)
    {
        var fromEmpty = false;
		var toEmpty = false;
		var relation = (from, to);

		if (_outRelationSets.TryGetValue(from, out var outRelations))
		{
			outRelations.Remove(to);
			if (_outRelationSets[from].Count == 0)
			{
				fromEmpty = true;
			}
		}

		if (_inRelationSets.TryGetValue(to, out var inRelations))
		{
			inRelations.Remove(from);
			if (_inRelationSets[to].Count == 0)
			{
				toEmpty = true;
			}
		}

		if (_relationData.ContainsKey(relation))
		{
			_relationData.Remove(relation);
		}

		return (fromEmpty, toEmpty);
    }

    public void RemoveEntity(in Entity entity)
    {
        if (_outRelationSets.TryGetValue(entity, out var outRelations))
		{
			foreach (var entityB in outRelations.AsSpan)
			{
				Remove(entity, entityB);
			}

            _setPool.Push(outRelations);
			_outRelationSets.Remove(entity);
		}

		if (_inRelationSets.TryGetValue(entity, out var inRelations))
		{
			foreach (var entityA in inRelations.AsSpan)
			{
				Remove(entityA, entity);
			}

            _setPool.Push(inRelations);
			_inRelationSets.Remove(entity);
		}
    }

    private IndexableSet<Entity> GetOrCreateHashSet()
    {
        if (_setPool.Count > 0)
        {
            return _setPool.Pop();
        }

        return new IndexableSet<Entity>();
    }
}
