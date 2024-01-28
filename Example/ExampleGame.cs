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
        var boxModel = Resources.Load<Model>("content/models/Box.pmdl");

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

        Entity floor = DefaultContext.World.CreateEntity("floor");
        DefaultContext.World.Set(floor, new TransformComponent(new Vector3(0f, -1f, 0f), Quaternion.Identity, Vector3.One));
        DefaultContext.World.Set(floor, new BoxColliderComponent
        {
            weight = 1f,
            extents = new Vector3(100f, 1f, 100f)
        });
        DefaultContext.World.Set(floor, new RigidbodyComponent
        {
            isStatic = true,
            material = PhysicsMaterial.Default
        });

        Random rng = new Random();
        for (int i = -4; i < 4; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                float yaw = (float)rng.NextDouble() * MathHelper.Pi * 2f;
                float pitch = (float)rng.NextDouble() * MathHelper.Pi * 2f;
                float roll = (float)rng.NextDouble() * MathHelper.Pi * 2f;

                Entity testRigidbody = DefaultContext.World.CreateEntity($"test rigidbody {i} {j}");
                DefaultContext.World.Set(testRigidbody, new TransformComponent(
                    new Vector3(i * 5f, 5f + (j * 5f), 0f),
                    Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll),
                    Vector3.One));
                DefaultContext.World.Set(testRigidbody, new BoxColliderComponent
                {
                    weight = 1f,
                    center = new Vector3(0f, 0f, 0f),
                    extents = new Vector3(1f, 1f, 1f)
                });
                DefaultContext.World.Set(testRigidbody, new RigidbodyComponent
                {
                    isStatic = false,
                    material = new PhysicsMaterial
                    {
                        friction = 1f,
                        maxRecoveryVelocity = float.MaxValue,
                        bounceFrequency = 30f,
                        bounceDamping = 0.1f
                    }
                });
                
                Entity testRigidbodyMesh = DefaultContext.World.CreateEntity();
                DefaultContext.World.Set(testRigidbodyMesh, new TransformComponent(Vector3.Zero, Quaternion.Identity, Vector3.One * 2f));
                DefaultContext.World.Set(testRigidbodyMesh, new ModelComponent
                {
                    model = boxModel
                });
                DefaultContext.World.Relate(testRigidbodyMesh, testRigidbody, new ChildOf());
            }
        }
    }
}
