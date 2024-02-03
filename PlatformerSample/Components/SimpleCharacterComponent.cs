using System.Text.Json.Serialization;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public struct SimpleCharacterComponent
{
    public uint collisionMask;
    public float radius;
    public float height;
    public float skinWidth;
    public float maxSlope;
    public float moveSpeed;
    public float jumpForce;
}

[SerializedComponent(nameof(SimpleCharacterComponent))]
public class SimpleCharacterComponentData : IComponentData
{
    [JsonPropertyName("collisionMask")]
    public uint CollisionMask { get; set; } = uint.MaxValue;
    
    [JsonPropertyName("radius")]
    public float Radius { get; set; } = 0.5f;
    
    [JsonPropertyName("height")]
    public float Height { get; set; } = 2f;
    
    [JsonPropertyName("skinWidth")]
    public float SkinWidth { get; set; } = 0.1f;
    
    [JsonPropertyName("maxSlope")]
    public float MaxSlope { get; set; } = 60f;
    
    [JsonPropertyName("moveSpeed")]
    public float MoveSpeed { get; set; } = 5f;
    
    [JsonPropertyName("jumpForce")]
    public float JumpForce { get; set; } = 5f;

    private SimpleCharacterComponent _component;

    public void OnDeserialize(PraxisGame game)
    {
        _component = new SimpleCharacterComponent
        {
            collisionMask = CollisionMask,
            radius = Radius,
            height = Height,
            skinWidth = SkinWidth,
            maxSlope = MaxSlope,
            moveSpeed = MoveSpeed,
            jumpForce = JumpForce
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _component);
    }
}