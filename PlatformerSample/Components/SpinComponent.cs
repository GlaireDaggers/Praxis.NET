using System.Text.Json.Serialization;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public struct SpinComponent
{
    public float rate;
}

[SerializedComponent(nameof(SpinComponent))]
public class SpinComponentData : IComponentData
{
    [JsonPropertyName("rate")]
    public float Rate { get; set; }

    private SpinComponent _component;

    public void OnDeserialize(PraxisGame game)
    {
        _component = new SpinComponent
        {
            rate = Rate
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _component);
    }
}
