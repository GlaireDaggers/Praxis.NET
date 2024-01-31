namespace Praxis.Core;

using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct SpotLightComponent
{   
    public float radius;
    public float innerConeAngle;
    public float outerConeAngle;
    public Vector3 color;
}

[SerializedComponent(nameof(SpotLightComponent))]
public class SpotLightComponentData : IComponentData
{
    [JsonPropertyName("color")]
    public Vector3 Color { get; set; }

    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    [JsonPropertyName("innerConeAngle")]
    public float InnerConeAngle { get; set; }

    [JsonPropertyName("outerConeAngle")]
    public float OuterConeAngle { get; set; }

    private SpotLightComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new SpotLightComponent
        {
            color = Color,
            radius = Radius,
            innerConeAngle = InnerConeAngle,
            outerConeAngle = OuterConeAngle
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}