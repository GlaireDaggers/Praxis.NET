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

        GraphicsDevice.PresentationParameters.MultiSampleCount = 4;

        new CameraMovementSystem(DefaultContext);

        Resources.Mount("content", new FolderFS("content/bin"));

        var foxModel = Resources.Load<Model>("content/models/Fox.pmdl");
        var boxModel = Resources.Load<Model>("content/models/Box.pmdl");

        var flamesTemplate = Resources.Load<EntityTemplate>("content/entities/Fire.json");

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
        DefaultContext.World.Set(floor, new TransformComponent(new Vector3(0f, -0.5f, 0f), Quaternion.Identity, Vector3.One));
        DefaultContext.World.Set(floor, new ColliderComponent
        {
            collider = new BoxColliderDefinition(new Vector3(100f, 1f, 100f))
        });

        Entity testKinematic = DefaultContext.World.CreateEntity($"test kinematic rigidbody");
        DefaultContext.World.Set(testKinematic, new TransformComponent(
            new Vector3(5f, 20f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(testKinematic, new ColliderComponent
        {
            collider = new BoxColliderDefinition(Vector3.One * 2f)
        });
        DefaultContext.World.Set(testKinematic, new RigidbodyComponent
        {
            isKinematic = true,
            material = PhysicsMaterial.Default
        });
        AttachModel(testKinematic, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity testRigidbody1 = DefaultContext.World.CreateEntity($"test rigidbody 1");
        DefaultContext.World.Set(testRigidbody1, new TransformComponent(
            new Vector3(8f, 17f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(testRigidbody1, new ColliderComponent
        {
            collider = new BoxColliderDefinition(Vector3.One * 2f)
            {
                Mass = 0.25f
            }
        });
        DefaultContext.World.Set(testRigidbody1, new RigidbodyComponent
        {
            isKinematic = false,
            material = PhysicsMaterial.Default
        });
        DefaultContext.World.Set(testRigidbody1, new ConstraintComponent
        {
            other = testKinematic,
            constraint = new BallSocketDefinition
            {
                LocalOffsetA = new Vector3(-3f, 0f, 0f),
                LocalOffsetB = new Vector3(0f, -3f, 0f)
            }
        });
        AttachModel(testRigidbody1, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity testRigidbody2 = DefaultContext.World.CreateEntity($"test rigidbody 2");
        DefaultContext.World.Set(testRigidbody2, new TransformComponent(
            new Vector3(11f, 17f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(testRigidbody2, new ColliderComponent
        {
            collider = new BoxColliderDefinition(Vector3.One * 2f)
            {
                Mass = 0.25f
            }
        });
        DefaultContext.World.Set(testRigidbody2, new RigidbodyComponent
        {
            isKinematic = false,
            material = PhysicsMaterial.Default
        });
        DefaultContext.World.Set(testRigidbody2, new ConstraintComponent
        {
            other = testRigidbody1,
            constraint = new BallSocketDefinition
            {
                LocalOffsetA = new Vector3(-3f, 0f, 0f),
                LocalOffsetB = new Vector3(3f, 0f, 0f)
            }
        });
        AttachModel(testRigidbody2, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity flames = flamesTemplate.Value.Unpack(DefaultContext.World, null);
        DefaultContext.World.Relate(flames, testRigidbody2, new ChildOf());

        /*Random rng = new Random();
        for (int i = -4; i < 4; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                float yaw = (float)rng.NextDouble() * MathHelper.Pi * 2f;
                float pitch = (float)rng.NextDouble() * MathHelper.Pi * 2f;
                float roll = (float)rng.NextDouble() * MathHelper.Pi * 2f;

                Entity testRigidbody = DefaultContext.World.CreateEntity($"test rigidbody {i} {j}");
                DefaultContext.World.Set(testRigidbody, new TransformComponent(
                    new Vector3(i * 5f, 5f + (j * 5f), 0f),
                    Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll),
                    Vector3.One));
                DefaultContext.World.Set(testRigidbody, new ColliderComponent
                {
                    collider = new BoxColliderDefinition(Vector3.One * 2f)
                    {
                        mass = 1f
                    }
                });
                DefaultContext.World.Set(testRigidbody, new RigidbodyComponent
                {
                    isKinematic = false,
                    material = new PhysicsMaterial
                    {
                        friction = 1f,
                        maxRecoveryVelocity = float.MaxValue,
                        bounceFrequency = 30f,
                        bounceDamping = 1f
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
        }*/
    }

    private void AttachModel(Entity entity, RuntimeResource<Model> model, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        Entity mesh = DefaultContext.World.CreateEntity();
        DefaultContext.World.Set(mesh, new TransformComponent(Vector3.Zero, Quaternion.Identity, Vector3.One * 2f));
        DefaultContext.World.Set(mesh, new ModelComponent
        {
            model = model
        });
        DefaultContext.World.Relate(mesh, entity, new ChildOf());
    }
}
