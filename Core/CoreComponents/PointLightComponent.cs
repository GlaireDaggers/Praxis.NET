namespace Praxis.Core;

using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct PointLightComponent
{
    public float radius;
    public Vector3 color;
}

[SerializedComponent(nameof(PointLightComponent))]
public class PointLightComponentData : IComponentData
{
    [JsonPropertyName("color")]
    public Vector3 Color { get; set; }

    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    private PointLightComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new PointLightComponent
        {
            color = Color,
            radius = Radius
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}