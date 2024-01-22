using System.Diagnostics;

namespace Praxis.Core;

/// <summary>
/// Wrapper around a managed object which can be safely passed around in ECS component structs
/// </summary>
public struct ObjectHandle<T> : IDisposable
{
    private static uint _nextId = 0;
    private static Dictionary<uint, T> _idContainer = new Dictionary<uint, T>();

    public readonly static ObjectHandle<T> NULL = new ObjectHandle<T>(default);

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

    public T? Resolve()
    {
        return GetObject(_id);
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
        uint id = ++_nextId;
        Debug.Assert(id != 0);

        _idContainer[id] = value!;

        return id;
    }

    private static T? GetObject(uint id)
    {
        if (id == 0) return default;
        return _idContainer[id];
    }

    private static void UnregisterObject(uint id)
    {
        _idContainer.Remove(id);
    }
}
