namespace Praxis.Core;

public struct RigidbodyComponent
{
    public bool isKinematic;
    public bool lockRotationX;
    public bool lockRotationY;
    public bool lockRotationZ;
    public uint collisionMask;
    public PhysicsMaterial material;

    public RigidbodyComponent()
    {
        isKinematic = false;
        lockRotationX = false;
        lockRotationY = false;
        lockRotationZ = false;
        collisionMask = uint.MaxValue;
        material = PhysicsMaterial.Default;
    }
}