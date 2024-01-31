namespace Praxis.Core;

using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct TransformComponent
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformComponent(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }
}

[SerializedComponent(nameof(TransformComponent))]
public class TransformComponentData : IComponentData
{
    [JsonPropertyName("position")]
    public Vector3 Position { get; set; } = Vector3.Zero;

    [JsonPropertyName("rotation")]
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    
    [JsonPropertyName("scale")]
    public Vector3 Scale { get; set; } = Vector3.One;

    private TransformComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new TransformComponent
        {
            position = Position,
            rotation = Rotation,
            scale = Scale
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}