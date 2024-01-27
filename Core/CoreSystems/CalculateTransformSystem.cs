namespace Praxis.Core;

using Praxis.Core.ECS;
using Microsoft.Xna.Framework;

public class CalculateTransformSystem : PraxisSystem
{
    HashSet<Entity> _seenCache = new HashSet<Entity>();

    Filter _transformFilter;

    public CalculateTransformSystem(WorldContext context) : base(context)
    {
        _transformFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Build();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        _seenCache.Clear();

        foreach (var entity in _transformFilter.Entities)
        {
            CalculateTransform(entity);
        }
    }

    private void CalculateTransform(in Entity entity)
    {
        if (_seenCache.Contains(entity))
        {
            return;
        }

        var transform = World.Get<TransformComponent>(entity);
        
        Matrix trs = Matrix.CreateScale(transform.scale)
            * Matrix.CreateFromQuaternion(transform.rotation)
            * Matrix.CreateTranslation(transform.position);

        if (World.HasOutRelations<ChildOf>(entity))
        {
            var parent = World.GetFirstOutRelation<ChildOf>(entity);
            CalculateTransform(parent);
            trs *= World.Get<CachedMatrixComponent>(parent).transform;
        }

        World.Set(entity, new CachedMatrixComponent
        {
            transform = trs
        });

        _seenCache.Add(entity);
    }
}
