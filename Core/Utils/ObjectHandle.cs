using System.Diagnostics;

namespace Praxis.Core;

/// <summary>
/// Wrapper around a managed object which can be safely passed around in ECS component structs
/// </summary>
public struct ObjectHandle<T> : IDisposable
{
    private static uint _nextId = 0;
    private static Dictionary<uint, T> _idMap = new Dictionary<uint, T>();
    private static Stack<uint> _idPool = new Stack<uint>();

    public readonly static ObjectHandle<T> NULL = new ObjectHandle<T>(default);

    /// <summary>
    /// Gets whether this handle has a valid value
    /// </summary>
    public bool HasValue => _id != 0;

    /// <summary>
    /// Gets the value of this handle
    /// </summary>
    public T? Value => GetObject(_id);

    private uint _id;

    public ObjectHandle(T? data)
    {
        if (data != null)
        {
            _id = RegisterObject(data);
        }
        else
        {
            _id = 0;
        }
    }

    public void Dispose()
    {
        if (_id != 0)
        {
            UnregisterObject(_id);
            _id = 0;
        }
    }

    private static uint RegisterObject(T value)
    {
        uint id;

        if (_idPool.Count > 0)
        {
            id = _idPool.Pop();
        }
        else
        {
            id = ++_nextId;
            Debug.Assert(id != 0);
        }
        
        _idMap[id] = value!;
        return id;
    }

    private static T? GetObject(uint id)
    {
        if (id == 0) return default;
        return _idMap[id];
    }

    private static void UnregisterObject(uint id)
    {
        _idMap.Remove(id);
        _idPool.Push(id);
    }
}
