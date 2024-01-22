using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonTools.ECS;
using Praxis.Core;
using ResourceCache.Core.FS;

using Model = Praxis.Core.Model;

namespace Example;

public class ExampleGame : PraxisGame
{
    public ExampleGame() : base("Example Game", 800, 600)
    {
    }

    protected override void Init()
    {
        base.Init();

        new CameraMovementSystem(DefaultContext);

        Resources.Mount("content", new FolderFS("content/bin"));

        var lanternModel = Resources.Load<Model>("content/models/LanternModel.json");
        var lanternModelHandle = new ObjectHandle<RuntimeResource<Model>>(lanternModel);

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
        DefaultContext.World.Set(lantern, new ModelComponent
        {
            modelHandle = lanternModelHandle
        });

        Entity ambientLight = DefaultContext.World.CreateEntity("ambientLight");
        DefaultContext.World.Set(ambientLight, new AmbientLightComponent
        {
            color = new Vector3(0.1f, 0.1f, 0.2f)
        });

        Entity directionalLight = DefaultContext.World.CreateEntity("directionalLight");
        DefaultContext.World.Set(directionalLight, new TransformComponent(
            Vector3.Zero,
            Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(45f), MathHelper.ToRadians(-45f), 0f),
            Vector3.One
        ));
        DefaultContext.World.Set(directionalLight, new DirectionalLightComponent
        {
            color = new Vector3(1f, 1f, 1f)
        });

        Entity pointLight = DefaultContext.World.CreateEntity("pointLight");
        DefaultContext.World.Set(pointLight, new TransformComponent(
            new Vector3(5f, 10f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(pointLight, new PointLightComponent
        {
            radius = 20f,
            color = new Vector3(1f, 0f, 1f)
        });
    }
}
