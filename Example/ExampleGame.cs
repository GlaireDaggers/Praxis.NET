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
                mass = 0.25f
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
                localOffsetA = new Vector3(-3f, 0f, 0f),
                localOffsetB = new Vector3(0f, -3f, 0f)
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
                mass = 0.25f
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
                localOffsetA = new Vector3(-3f, 0f, 0f),
                localOffsetB = new Vector3(3f, 0f, 0f)
            }
        });
        AttachModel(testRigidbody2, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity flames = DefaultContext.World.CreateEntity("flames");
        DefaultContext.World.Set(flames, new TransformComponent(
            new Vector3(0f, 0f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(flames, new ParticleEmitterComponent
        {
            worldSpace = true,
            maxParticles = 100,
            particlesPerBurst = 1,
            maxBurstCount = 0,
            burstInterval = 0.1f,
            minParticleLifetime = 1f,
            maxParticleLifetime = 2f,
            minAngle = 0f,
            maxAngle = 360f,
            minAngularVelocity = -90f,
            maxAngularVelocity = 90f,
            linearDamping = 0.1f,
            angularDamping = 0f,
        });
        DefaultContext.World.Set(flames, new ParticleEmitterSphereShapeComponent
        {
            radius = 1f
        });
        DefaultContext.World.Set(flames, new ParticleEmitterInitVelocityInConeComponent
        {
            angle = 10f,
            direction = Vector3.UnitY,
            minForce = 0f,
            maxForce = 1f,
        });
        DefaultContext.World.Set(flames, new ParticleEmitterAddLinearForceComponent
        {
            worldSpace = true,
            force = new Vector3(0.1f, 2f, 0.1f)
        });
        DefaultContext.World.Set(flames, new ParticleEmitterSpriteRenderComponent
        {
            material = Resources.Load<Material>("content/vfx/Flame.json"),
            colorOverLifetime = new ColorAnimationCurve(CurveInterpolationMode.Linear, [
                new () { time = 0f, value = new Color(1f, 1f, 1f, 0f) },
                new () { time = 0.1f, value = Color.Yellow },
                new () { time = 0.5f, value = Color.OrangeRed },
                new () { time = 1f, value = new Color(1f, 0f, 0f, 0f) }
            ]),
            sizeOverLifetime = new Vector2AnimationCurve(CurveInterpolationMode.Linear, [
                new () { time = 0f, value = Vector2.One * 3f },
                new () { time = 1f, value = Vector2.One * 6f }
            ])
        });
        DefaultContext.World.Set(flames, new PointLightComponent
        {
            radius = 15f,
            color = new Vector3(2f, 1f, 0.25f)
        });
        DefaultContext.World.Relate(flames, testRigidbody2, new ChildOf());

        Entity smoke = DefaultContext.World.CreateEntity("smoke");
        DefaultContext.World.Set(smoke, new TransformComponent(
            new Vector3(0f, 0f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(smoke, new ParticleEmitterComponent
        {
            worldSpace = true,
            maxParticles = 100,
            particlesPerBurst = 1,
            maxBurstCount = 0,
            burstInterval = 0.05f,
            minParticleLifetime = 3f,
            maxParticleLifetime = 4f,
            minAngle = 0f,
            maxAngle = 360f,
            minAngularVelocity = -90f,
            maxAngularVelocity = 90f,
            linearDamping = 0.1f,
            angularDamping = 0f,
        });
        DefaultContext.World.Set(smoke, new ParticleEmitterSphereShapeComponent
        {
            radius = 1f
        });
        DefaultContext.World.Set(smoke, new ParticleEmitterInitVelocityInConeComponent
        {
            angle = 10f,
            direction = Vector3.UnitY,
            minForce = 0f,
            maxForce = 1f,
        });
        DefaultContext.World.Set(smoke, new ParticleEmitterAddLinearForceComponent
        {
            worldSpace = true,
            force = new Vector3(0.1f, 2f, 0.1f)
        });
        DefaultContext.World.Set(smoke, new ParticleEmitterSpriteRenderComponent
        {
            sortBias = -0.1f,
            material = Resources.Load<Material>("content/vfx/Smoke.json"),
            colorOverLifetime = new ColorAnimationCurve(CurveInterpolationMode.Linear, [
                new () { time = 0f, value = new Color(1f, 0.5f, 0f, 0f) },
                new () { time = 0.2f, value = new Color(1f, 0.5f, 0.1f, 0.5f) },
                new () { time = 0.5f, value = new Color(0.1f, 0.1f, 0.1f, 1f) },
                new () { time = 1f, value = new Color(0.1f, 0.1f, 0.1f, 0f) }
            ]),
            sizeOverLifetime = new Vector2AnimationCurve(CurveInterpolationMode.Linear, [
                new () { time = 0f, value = Vector2.One * 3f },
                new () { time = 1f, value = Vector2.One * 8f }
            ])
        });
        DefaultContext.World.Relate(smoke, flames, new ChildOf());

        Entity sparks = DefaultContext.World.CreateEntity("sparks");
        DefaultContext.World.Set(sparks, new TransformComponent(
            new Vector3(0f, 0f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        DefaultContext.World.Set(sparks, new ParticleEmitterComponent
        {
            worldSpace = true,
            maxParticles = 100,
            particlesPerBurst = 1,
            maxBurstCount = 0,
            burstInterval = 0.1f,
            minParticleLifetime = 2f,
            maxParticleLifetime = 4f,
            minAngle = 0f,
            maxAngle = 0f,
            linearDamping = 0.1f,
            angularDamping = 0f,
        });
        DefaultContext.World.Set(sparks, new ParticleEmitterSphereShapeComponent
        {
            radius = 2f
        });
        DefaultContext.World.Set(sparks, new ParticleEmitterInitRandomVelocityComponent
        {
            minForce = new Vector3(-10f, -10f, -10f),
            maxForce = new Vector3(10f, 10f, 10f)
        });
        DefaultContext.World.Set(sparks, new ParticleEmitterAddLinearForceComponent
        {
            worldSpace = true,
            force = new Vector3(0.1f, 2f, 0.1f)
        });
        DefaultContext.World.Set(sparks, new ParticleEmitterAddNoiseForceComponent
        {
            worldSpace = true,
            seed = 1337,
            scroll = new Vector3(1f, 1f, 1f),
            frequency = 0.25f,
            magnitude = 5f
        });
        DefaultContext.World.Set(sparks, new ParticleEmitterSpriteRenderComponent
        {
            sortBias = 0.1f,
            material = Resources.Load<Material>("content/vfx/Spark.json"),
            colorOverLifetime = new ColorAnimationCurve(CurveInterpolationMode.Linear, [
                new () { time = 0f, value = new Color(1f, 1f, 1f, 0f) },
                new () { time = 0.1f, value = Color.Yellow },
                new () { time = 0.5f, value = Color.OrangeRed },
                new () { time = 1f, value = new Color(1f, 0f, 0f, 0f) }
            ]),
            sizeOverLifetime = new Vector2AnimationCurve(CurveInterpolationMode.Linear, [
                new () { time = 0f, value = Vector2.One * 0.5f },
                new () { time = 1f, value = Vector2.Zero }
            ])
        });
        DefaultContext.World.Relate(sparks, flames, new ChildOf());

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
