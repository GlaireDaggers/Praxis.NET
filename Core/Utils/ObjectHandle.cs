namespace Praxis.Core;

/// <summary>
/// Wrapper around a managed object which can be safely passed around in ECS component structs
/// </summary>
public struct ObjectHandle<T> : IDisposable
{
    public readonly static ObjectHandle<T> NULL = new ObjectHandle<T>(default);

    private uint _id;

    public ObjectHandle(T? data)
    {
        if (data != null)
        {
            _id = PraxisGame.Instance!.RegisterObject(data);
        }
        else
        {
            _id = 0;
        }
    }

    public T? Resolve()
    {
        return PraxisGame.Instance!.GetObject<T>(_id);
    }

    public void Dispose()
    {
        if (_id != 0)
        {
            PraxisGame.Instance!.UnregisterObject(_id);
            _id = 0;
        }
    }
}
