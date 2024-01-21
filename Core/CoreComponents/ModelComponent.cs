using ResourceCache.Core;

namespace Praxis.Core;

public struct ModelComponent
{
    public ObjectHandle<Model> modelHandle;
}

public struct ModelResourceComponent
{
    public ObjectHandle<ResourceHandle<Model>> modelResourceHandle;
}