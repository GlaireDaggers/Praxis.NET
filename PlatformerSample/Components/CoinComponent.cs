using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public struct CoinComponent
{
}

[SerializedComponent(nameof(CoinComponent))]
public class CoinComponentData : IComponentData
{
    public void OnDeserialize(PraxisGame game)
    {
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, new CoinComponent());
    }
}
