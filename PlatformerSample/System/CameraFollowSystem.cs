using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public class CameraFollowSystem : PraxisSystem
{
    private Filter _cameraFilter;

    public CameraFollowSystem(WorldContext context) : base(context)
    {
        _cameraFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<CameraFollowComponent>()
            .Build("CameraFollowSystem.cameraFilter");
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var entity in _cameraFilter.Entities)
        {
            var transform = World.Get<TransformComponent>(entity);
            var cameraFollow = World.Get<CameraFollowComponent>(entity);

            if (World.HasOutRelations<Following>(entity))
            {
                Entity following = World.GetFirstOutRelation<Following>(entity);
                var targetTransform = World.Get<TransformComponent>(following);

                var targetToCamVec = transform.position - targetTransform.position;
                targetToCamVec.Y = 0f;
                var targetPos = targetTransform.position + (SafeNormalize(targetToCamVec, -Vector3.UnitZ) * cameraFollow.followRadius) + (Vector3.UnitY * cameraFollow.followHeightOffset);

                float lerpFactor = 1f - MathF.Pow(cameraFollow.damping, deltaTime);
                transform.position = Vector3.Lerp(transform.position, targetPos, lerpFactor);

                var lookAtPoint = targetTransform.position + (Vector3.UnitY * cameraFollow.lookatHeightOffset);
                var lookAtMat = Matrix.CreateLookAt(transform.position, lookAtPoint, Vector3.UnitY);
                if (lookAtMat.Decompose(out _, out var rot, out _))
                {
                    transform.rotation = Quaternion.Inverse(rot);
                }

                World.Set(entity, transform);
            }
        }
    }

    private Vector3 SafeNormalize(Vector3 v, Vector3 fallback)
    {
        float lenSqr = v.LengthSquared();
        if (lenSqr == 0f) return fallback;
        return v / MathF.Sqrt(lenSqr);
    }
}
