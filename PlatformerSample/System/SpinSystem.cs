using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public class SpinSystem : PraxisSystem
{
    private Filter _spinFilter;

    public SpinSystem(WorldContext context) : base(context)
    {
        _spinFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<SpinComponent>()
            .Build("SpinSystem.spinFilter");
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var entity in _spinFilter.Entities)
        {
            var transform = World.Get<TransformComponent>(entity);
            var spin = World.Get<SpinComponent>(entity);

            Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(spin.rate) * deltaTime);
            transform.rotation = Quaternion.Multiply(transform.rotation, rot);

            World.Set(entity, transform);
        }
    }
}
