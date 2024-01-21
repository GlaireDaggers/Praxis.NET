namespace Praxis.Core;

using ResourceCache.Core;

// this feels kind of goofy, but basically lets us "spoof" resource handles with runtime assets that are always considered "loaded"

/// <summary>
/// A helper which wraps either a resource handle loaded from disk *or* a runtime-constructed resource object
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
public struct RuntimeResource<T>
{
    /// <summary>
    /// The current loading state of the resource. If this is a runtime resource, it is always considered loaded
    /// </summary>
    public ResourceLoadState State => _handle?.State ?? ResourceLoadState.Loaded;

    /// <summary>
    /// The underlying value of the resource. If the resource is still loading, the calling thread will wait until loading finishes
    /// </summary>
    public T Value => _res ?? _handle!.Value.Value;

    private T? _res;
    private ResourceHandle<T>? _handle;

    /// <summary>
    /// Construct a reference to a runtime-created resource
    /// </summary>
    public RuntimeResource(T resource)
    {
        _res = resource;
        _handle = null;
    }

    /// <summary>
    /// Construct a reference to a loaded resource handle
    /// </summary>
    public RuntimeResource(ResourceHandle<T> resourceHandle)
    {
        _res = default;
        _handle = resourceHandle;
    }

    public static implicit operator RuntimeResource<T>(T resource)
    {
        return new RuntimeResource<T>(resource);
    }

    public static implicit operator RuntimeResource<T>(ResourceHandle<T> resource)
    {
        return new RuntimeResource<T>(resource);
    }
}
