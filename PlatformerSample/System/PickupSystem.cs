using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

[ExecuteAfter(typeof(PhysicsSystem))]
public class PickupSystem : PraxisSystem
{
    private Filter _coinFilter;

    public PickupSystem(WorldContext context) : base(context)
    {
        _coinFilter = new FilterBuilder(World)
            .Include<CoinComponent>()
            .Build("PickupSystem.coinFilter");
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var msg in World.GetMessages<TriggerEnterMessage>())
        {
            if (_coinFilter.Contains(msg.a))
            {
                PlayerStats stats;
                if (World.HasSingleton<PlayerStats>())
                {
                    stats = World.GetSingleton<PlayerStats>();
                }
                else
                {
                    stats = new PlayerStats();
                }

                stats.score++;
                Console.WriteLine($"Score: {stats.score}");

                World.SetSingleton(stats);

                World.Send(new DestroyEntity
                {
                    entity = msg.a
                });
            }
        }
    }
}
