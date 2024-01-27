using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Praxis.Core;
using Praxis.Core.ECS;
using ResourceCache.Core.FS;

using Model = Praxis.Core.Model;

namespace Example;

public class ExampleGame : PraxisGame
{
    public ExampleGame() : base("Example Game", 1280, 720)
    {
    }

    protected override void Init()
    {
        base.Init();

        new CameraMovementSystem(DefaultContext);

        Resources.Mount("content", new FolderFS("content/bin"));

        var foxModel = Resources.Load<Model>("content/models/Fox.pmdl");

        var filterStack = new ScreenFilterStack(this);
        filterStack.filters.Add(new BloomFilter(this));
        filterStack.filters.Add(new TestFilter(this));

        LoadScene("content/levels/TestLevel.owblevel", DefaultContext.World);

        Entity camera = DefaultContext.World.CreateEntity("camera");
        DefaultContext.World.Set(camera, new TransformComponent(new Vector3(0f, 10f, 20f), Quaternion.Identity, Vector3.One));
        DefaultContext.World.Set(camera, new CameraComponent()
        {
            isOrthographic = false,
            fieldOfView = 60f,
            near = 0.1f,
            far = 1000.0f,
            clearColor = Color.CornflowerBlue,
            renderTarget = null,
            filterStack = filterStack
        });
        DefaultContext.World.Set(camera, new CameraMovementComponent
        {
            moveSpeed = 5f
        });
        
        Entity fox = DefaultContext.World.CreateEntity("fox");
        SimpleAnimationComponent foxAnim = new SimpleAnimationComponent();
        foxAnim.SetAnimation(foxModel.Value.GetAnimationId("Run"));
        DefaultContext.World.Set(fox, new TransformComponent(new Vector3(20f, 0f, 0f), Quaternion.Identity, Vector3.One * 0.1f));
        DefaultContext.World.Set(fox, new ModelComponent
        {
            model = foxModel
        });
        DefaultContext.World.Set(fox, foxAnim);
    }
}
