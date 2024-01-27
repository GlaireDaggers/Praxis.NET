using Microsoft.Xna.Framework;

namespace Praxis.Core;

public struct RigidbodyComponent
{
    public bool isStatic;
    public PhysicsMaterial material;

    public RigidbodyComponent()
    {
        isStatic = false;
        material = PhysicsMaterial.Default;
    }
}

public struct BoxColliderComponent
{
    public float weight;
    public Vector3 center;
    public Vector3 extents;
}