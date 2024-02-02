namespace Example;

using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

[ExecuteBefore(typeof(CalculateTransformSystem))]
public class CameraMovementSystem : PraxisSystem
{
    Filter _cameraFilter;

    public CameraMovementSystem(WorldContext context) : base(context)
    {
        _cameraFilter = new FilterBuilder(World)
            .Include<TransformComponent>()
            .Include<CameraMovementComponent>()
            .Build("CameraMovementSystem.cameraFilter");
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var entity in _cameraFilter.Entities)
        {
            var transformComp = World.Get<TransformComponent>(entity);
            var movementComp = World.Get<CameraMovementComponent>(entity);

            Matrix rot = Matrix.CreateFromQuaternion(transformComp.rotation);

            Vector3 fwd = Vector3.TransformNormal(-Vector3.UnitZ, rot);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);

            float moveX = Game.Input.GetAxis("Camera X");
            float moveY = Game.Input.GetAxis("Camera Y");

            transformComp.position += (fwd * moveY * movementComp.moveSpeed * deltaTime) + (right * moveX * movementComp.moveSpeed * deltaTime);

            World.Set(entity, transformComp);
        }
    }
}
