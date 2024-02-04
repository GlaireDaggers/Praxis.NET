using System.Text.Json.Serialization;
using Praxis.Core.ECS;

namespace Praxis.Core;

public struct RigidbodyComponent
{
    public bool isKinematic;
    public bool isTrigger;
    public bool lockRotationX;
    public bool lockRotationY;
    public bool lockRotationZ;
    public uint collisionMask;
    public PhysicsMaterial material;

    public RigidbodyComponent()
    {
        isKinematic = false;
        isTrigger = false;
        lockRotationX = false;
        lockRotationY = false;
        lockRotationZ = false;
        collisionMask = uint.MaxValue;
        material = PhysicsMaterial.Default;
    }
}

[SerializedComponent(nameof(RigidbodyComponent))]
public class RigidbodyComponentData : IComponentData
{
    [JsonPropertyName("isKinematic")]
    public bool IsKinematic { get; set; } = false;
    
    [JsonPropertyName("isTrigger")]
    public bool IsTrigger { get; set; } = false;
    
    [JsonPropertyName("lockRotationX")]
    public bool LockRotationX { get; set; } = false;
    
    [JsonPropertyName("lockRotationY")]
    public bool LockRotationY { get; set; } = false;
    
    [JsonPropertyName("lockRotationZ")]
    public bool LockRotationZ { get; set; } = false;
    
    [JsonPropertyName("collisionMask")]
    public uint CollisionMask { get; set; } = uint.MaxValue;
    
    [JsonPropertyName("friction")]
    public float Friction { get; set; } = 1f;
    
    [JsonPropertyName("maxRecoveryVelocity")]
    public float MaxRecoveryVelocity { get; set; } = float.MaxValue;
    
    [JsonPropertyName("bounceFrequency")]
    public float BounceFrequency { get; set; } = 30f;
    
    [JsonPropertyName("bounceDamping")]
    public float BounceDamping { get; set; } = 1f;

    private RigidbodyComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new RigidbodyComponent
        {
            isKinematic = IsKinematic,
            isTrigger = IsTrigger,
            lockRotationX = LockRotationX,
            lockRotationY = LockRotationY,
            lockRotationZ = LockRotationZ,
            collisionMask = CollisionMask,
            material = new PhysicsMaterial
            {
                friction = Friction,
                maxRecoveryVelocity = MaxRecoveryVelocity,
                bounceFrequency = BounceFrequency,
                bounceDamping = BounceDamping
            }
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}