using System.Text.Json.Serialization;
using Praxis.Core.ECS;

namespace Praxis.Core;

public struct ModelComponent
{
    public RuntimeResource<Model> model;
}

[SerializedComponent(nameof(ModelComponent))]
public class ModelComponentData : IComponentData
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    private ModelComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new ModelComponent
        {
            model = game.Resources.Load<Model>(Model!)
        };
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}