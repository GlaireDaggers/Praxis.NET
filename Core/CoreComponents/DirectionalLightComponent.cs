namespace Praxis.Core;

using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Praxis.Core.ECS;

public struct DirectionalLightComponent
{
    public Vector3 color;
}

[SerializedComponent(nameof(DirectionalLightComponent))]
public class DirectionalLightComponentData : IComponentData
{
    [JsonPropertyName("color")]
    public Vector3 Color { get; set; }

    private DirectionalLightComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new DirectionalLightComponent
        {
            color = Color
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}