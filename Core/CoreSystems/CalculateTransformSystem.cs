namespace Praxis.Core;

using Microsoft.Xna.Framework;
using MoonTools.ECS;

public class CalculateTransformSystem : PraxisSystem
{
    Filter _transformFilter;

    public CalculateTransformSystem(WorldContext context) : base(context)
    {
        _transformFilter = World.FilterBuilder
            .Include<TransformComponent>()
            .Build();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var entity in _transformFilter.Entities)
        {
            Matrix trs = CalculateTransform(entity);

            World.Set(entity, new CachedMatrixComponent
            {
                transform = trs
            });
        }
    }

    private Matrix CalculateTransform(in Entity entity)
    {
        var transform = World.Get<TransformComponent>(entity);
            
        Matrix trs = Matrix.CreateTranslation(transform.position)
            * Matrix.CreateFromQuaternion(transform.rotation)
            * Matrix.CreateScale(transform.scale);

        if (World.HasOutRelation<ChildOf>(entity))
        {
            var parent = World.OutRelationSingleton<ChildOf>(entity);
            trs = trs * CalculateTransform(parent);
        }

        return trs;
    }
}
