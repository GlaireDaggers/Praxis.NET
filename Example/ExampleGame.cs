using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonTools.ECS;
using Praxis.Core;
using ResourceCache.Core;
using ResourceCache.Core.FS;

using Model = Praxis.Core.Model;

namespace Example;

public class ExampleGame : PraxisGame
{
    public ExampleGame() : base("Example Game")
    {
    }

    protected override void Init()
    {
        base.Init();

        new CameraMovementSystem(DefaultContext);

        Resources.Mount("content", new FolderFS("content"));

        var lanternModel = Resources.Load<Model>("content/models/Lantern.glb");
        var lanternModelHandle = new ObjectHandle<ResourceHandle<Model>>(lanternModel);

        Entity camera = DefaultContext.World.CreateEntity("camera");
        DefaultContext.World.Set(camera, new TransformComponent(new Vector3(0f, 10f, 20f), Quaternion.Identity, Vector3.One));
        DefaultContext.World.Set(camera, new CameraComponent()
        {
            isOrthographic = false,
            fieldOfView = 60f,
            near = 0.1f,
            far = 1000.0f,
            clearColor = Color.CornflowerBlue,
            renderTarget = ObjectHandle<RenderTarget2D>.NULL
        });
        DefaultContext.World.Set(camera, new CameraMovementComponent
        {
            moveSpeed = 5f
        });

        Entity lantern = DefaultContext.World.CreateEntity("lantern");
        DefaultContext.World.Set(lantern, new TransformComponent(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One));
        DefaultContext.World.Set(lantern, new ModelResourceComponent
        {
            modelResourceHandle = lanternModelHandle
        });
    }
}
