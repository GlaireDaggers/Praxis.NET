﻿using System.Collections;
using System.Runtime.InteropServices;

namespace Praxis.Core.ECS;

public class World
{
    private uint _nextId = 0;
    private Stack<uint> _idPool = new Stack<uint>();

    private List<IComponentStorage> _componentDepotList = new List<IComponentStorage>();
    private Dictionary<uint, IComponentStorage> _componentDepot = new Dictionary<uint, IComponentStorage>();
    private List<IRelationStorage> _relationDepotList = new List<IRelationStorage>();
    private Dictionary<uint, IRelationStorage> _relationDepot = new Dictionary<uint, IRelationStorage>();

    private List<IList> _messageDepotList = new List<IList>();
    private Dictionary<uint, IList> _messageDepot = new Dictionary<uint, IList>();

    private Dictionary<uint, object> _singletonComponentStorage = new Dictionary<uint, object>();

    private Dictionary<FilterSignature, Filter> _filters = new Dictionary<FilterSignature, Filter>();
    private List<Filter> _filterList = new List<Filter>();

    public void SetSingleton<T>(in T data)
        where T : notnull
    {
        uint typeId = TypeId.GetTypeId<T>();
        _singletonComponentStorage[typeId] = data;
    }

    public bool HasSingleton<T>()
        where T : notnull
    {
        uint typeId = TypeId.GetTypeId<T>();
        return _singletonComponentStorage.ContainsKey(typeId);
    }

    public T GetSingleton<T>()
        where T : notnull
    {
        uint typeId = TypeId.GetTypeId<T>();
        return (T)_singletonComponentStorage[typeId];
    }

    public void Send<T>(in T message)
    {
        GetMessageStorage<T>().Add(message);
    }

    public Entity CreateEntity(string? tag = null)
    {
        uint id;
        if (_idPool.Count > 0)
        {
            id = _idPool.Pop();
        }
        else
        {
            id = _nextId++;
        }

        return new Entity(id, tag);
    }

    public void DestroyEntity(in Entity entity)
    {
        foreach (var comp in _componentDepotList)
        {
            comp.Remove(entity);
        }

        foreach (var relation in _relationDepotList)
        {
            relation.RemoveEntity(entity);
        }

        _idPool.Push(entity.ID);
    }

    public void Set<T>(in Entity entity, T data)
        where T : struct
    {
        if (!GetComponentStorage<T>().Set(entity, data))
        {
            UpdateFilters(entity);
        }
    }

    public bool Has<T>(in Entity entity)
        where T : struct
    {
        return GetComponentStorage<T>().Contains(entity);
    }

    public T Get<T>(in Entity entity)
        where T : struct
    {
        return GetComponentStorage<T>().Get(entity);
    }

    public bool Remove<T>(in Entity entity)
        where T : struct
    {
        if (GetComponentStorage<T>().Remove(entity))
        {
            UpdateFilters(entity);
            return true;
        }

        return false;
    }

    public void Relate<T>(in Entity from, in Entity to, in T data)
        where T : struct
    {
        GetRelationStorage<T>().Set(from, to, data);
    }

    public bool HasRelation<T>(in Entity from, in Entity to)
        where T : struct
    {
        return GetRelationStorage<T>().Has(from, to);
    }

    public T GetRelation<T>(in Entity from, in Entity to)
        where T : struct
    {
        return GetRelationStorage<T>().Get(from, to);
    }

    public bool HasOutRelations<T>(in Entity from)
        where T : struct
    {
        return GetRelationStorage<T>().HasOutRelations(from);
    }

    public bool HasInRelations<T>(in Entity to)
        where T : struct
    {
        return GetRelationStorage<T>().HasInRelations(to);
    }

    public SpanEnumerator<Entity> GetOutRelations<T>(in Entity from)
        where T : struct
    {
        return GetRelationStorage<T>().GetOutRelations(from);
    }

    public SpanEnumerator<Entity> GetInRelations<T>(in Entity from)
        where T : struct
    {
        return GetRelationStorage<T>().GetInRelations(from);
    }

    public Entity GetFirstOutRelation<T>(in Entity from)
        where T : struct
    {
        return GetRelationStorage<T>().GetFirstOutRelation(from);
    }

    public Entity GetFirstInRelation<T>(in Entity from)
        where T : struct
    {
        return GetRelationStorage<T>().GetFirstInRelation(from);
    }

    public SpanEnumerator<T> GetMessages<T>()
    {
        var span = CollectionsMarshal.AsSpan(GetMessageStorage<T>());
        return new SpanEnumerator<T>(span);
    }

    public void PostUpdate()
    {
        foreach (var storage in _messageDepotList)
        {
            storage.Clear();
        }
    }

    internal Filter GetFilter(FilterSignature signature)
    {
        if (_filters.ContainsKey(signature))
        {
            return _filters[signature];
        }

        var filter = new Filter(this, signature);
        _filters[signature] = filter;
        _filterList.Add(filter);

        return filter;
    }

    private void UpdateFilters(in Entity entity)
    {
        foreach (var filter in _filterList)
        {
            filter.Check(entity);
        }
    }

    internal bool Has(in Entity entity, uint typeId)
    {
        if (!_componentDepot.ContainsKey(typeId))
        {
            return false;
        }

        IComponentStorage storage = _componentDepot[typeId];
        return storage.Contains(entity);
    }

    private List<T> GetMessageStorage<T>()
    {
        uint typeId = TypeId.GetTypeId<T>();

        if (!_messageDepot.ContainsKey(typeId))
        {
            var storage = new List<T>();
            _messageDepot.Add(typeId, storage);
            _messageDepotList.Add(storage);
        }

        return (List<T>)_messageDepot[typeId];
    }

    private RelationStorage<T> GetRelationStorage<T>()
        where T : struct
    {
        uint typeId = TypeId.GetTypeId<T>();

        if (!_relationDepot.ContainsKey(typeId))
        {
            var storage = new RelationStorage<T>();
            _relationDepot.Add(typeId, storage);
            _relationDepotList.Add(storage);
        }

        return (RelationStorage<T>)_relationDepot[typeId];
    }

    private ComponentStorage<T> GetComponentStorage<T>()
        where T : struct
    {
        uint typeId = TypeId.GetTypeId<T>();

        if (!_componentDepot.ContainsKey(typeId))
        {
            var storage = new ComponentStorage<T>();
            _componentDepot.Add(typeId, storage);
            _componentDepotList.Add(storage);
        }

        return (ComponentStorage<T>)_componentDepot[typeId];
    }
}