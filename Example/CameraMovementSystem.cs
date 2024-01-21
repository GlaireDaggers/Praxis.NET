namespace Example;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MoonTools.ECS;
using Praxis.Core;

public class CameraMovementSystem : PraxisSystem
{
    Filter _cameraFilter;

    public CameraMovementSystem(WorldContext context) : base(context)
    {
        _cameraFilter = World.FilterBuilder
            .Include<TransformComponent>()
            .Include<CameraMovementComponent>()
            .Build();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        KeyboardState kb = Keyboard.GetState();

        foreach (var entity in _cameraFilter.Entities)
        {
            var transformComp = World.Get<TransformComponent>(entity);
            var movementComp = World.Get<CameraMovementComponent>(entity);

            Matrix rot = Matrix.CreateFromQuaternion(transformComp.rotation);

            Vector3 fwd = Vector3.TransformNormal(-Vector3.UnitZ, rot);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);

            if (kb.IsKeyDown(Keys.W))
            {
                transformComp.position += fwd * movementComp.moveSpeed * deltaTime;
            }
            else if (kb.IsKeyDown(Keys.S))
            {
                transformComp.position -= fwd * movementComp.moveSpeed * deltaTime;
            }

            if (kb.IsKeyDown(Keys.D))
            {
                transformComp.position += right * movementComp.moveSpeed * deltaTime;
            }
            else if (kb.IsKeyDown(Keys.A))
            {
                transformComp.position -= right * movementComp.moveSpeed * deltaTime;
            }

            World.Set(entity, transformComp);
        }
    }
}
