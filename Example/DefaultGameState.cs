using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace Example;

/// <summary>
/// Default game state for example game
/// </summary>
public class DefaultGameState : GameState
{
    private readonly WorldContext _context;
    private Scene? _scene;

    public DefaultGameState(PraxisGame game) : base(game)
    {
        _context = new WorldContext("Example World", Game);
    }

    public override void OnEnter()
    {
        base.OnEnter();

        Game.RegisterContext(_context);

        Game.InstallDefaultSystems(_context);
        new CameraMovementSystem(_context);
        new SimpleCharacterMovementSystem(_context);

        var foxModel = Game.Resources.Load<Model>("content/models/Fox.pmdl");
        var boxModel = Game.Resources.Load<Model>("content/models/Box.pmdl");

        var flamesTemplate = Game.Resources.Load<EntityTemplate>("content/entities/Fire.json");

        var filterStack = new ScreenFilterStack(Game);
        filterStack.filters.Add(new BloomFilter(Game));
        filterStack.filters.Add(new TestFilter(Game));

        _scene = Game.LoadScene("content/levels/TestLevel.owblevel", _context.World);

        Entity camera = _context.World.CreateEntity("camera");
        _context.World.Set(camera, new TransformComponent(new Vector3(0f, 10f, 20f), Quaternion.Identity, Vector3.One));
        _context.World.Set(camera, new CameraComponent()
        {
            isOrthographic = false,
            fieldOfView = 60f,
            near = 0.1f,
            far = 1000.0f,
            clearColor = Color.CornflowerBlue,
            renderTarget = null,
            filterStack = filterStack
        });
        _context.World.Set(camera, new CameraMovementComponent
        {
            moveSpeed = 5f
        });

        Entity testCharacter = _context.World.CreateEntity("testCharacter");
        _context.World.Set(testCharacter, new TransformComponent(new Vector3(-20f, 5f, 0f), Quaternion.Identity, Vector3.One));
        _context.World.Set(testCharacter, new SimpleCharacterComponent
        {
            collisionMask = uint.MaxValue,
            radius = 0.5f,
            height = 2f,
            skinWidth = 0.1f,
            maxSlope = 60f
        });

        Entity testCharacterMesh = _context.World.CreateEntity("mesh");
        _context.World.Set(testCharacterMesh, new TransformComponent(new Vector3(0f, 0f, 0f), Quaternion.Identity, new Vector3(1f, 2f, 1f)));
        _context.World.Set(testCharacterMesh, new ModelComponent
        {
            model = boxModel
        });
        _context.World.Relate(testCharacterMesh, testCharacter, new ChildOf());
        
        Entity fox = _context.World.CreateEntity("fox");
        SimpleAnimationComponent foxAnim = new SimpleAnimationComponent();
        foxAnim.SetAnimation(foxModel.Value.GetAnimationId("Run"));
        _context.World.Set(fox, new TransformComponent(new Vector3(20f, 0f, 0f), Quaternion.Identity, Vector3.One * 0.1f));
        _context.World.Set(fox, new ModelComponent
        {
            model = foxModel
        });
        _context.World.Set(fox, foxAnim);

        Entity floor = _context.World.CreateEntity("floor");
        _context.World.Set(floor, new TransformComponent(new Vector3(0f, -0.5f, 0f),
            Quaternion.CreateFromYawPitchRoll(0f, MathHelper.ToRadians(10f), 0f), Vector3.One));
        _context.World.Set(floor, new ColliderComponent
        {
            collider = new BoxColliderDefinition(new Vector3(100f, 1f, 100f))
        });

        var chainOffset = 13f;

        Entity testKinematic = _context.World.CreateEntity($"test kinematic rigidbody");
        _context.World.Set(testKinematic, new TransformComponent(
            new Vector3(chainOffset, 20f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        _context.World.Set(testKinematic, new ColliderComponent
        {
            collider = new BoxColliderDefinition(Vector3.One * 2f)
        });
        _context.World.Set(testKinematic, new RigidbodyComponent
        {
            isKinematic = true,
            material = PhysicsMaterial.Default
        });
        AttachModel(testKinematic, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity testRigidbody1 = _context.World.CreateEntity($"test rigidbody 1");
        _context.World.Set(testRigidbody1, new TransformComponent(
            new Vector3(chainOffset + 3f, 17f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        _context.World.Set(testRigidbody1, new ColliderComponent
        {
            collider = new BoxColliderDefinition(Vector3.One * 2f)
            {
                Mass = 0.25f
            }
        });
        _context.World.Set(testRigidbody1, new RigidbodyComponent
        {
            isKinematic = false,
            material = PhysicsMaterial.Default
        });
        _context.World.Set(testRigidbody1, new ConstraintComponent
        {
            other = testKinematic,
            constraint = new BallSocketDefinition
            {
                LocalOffsetA = new Vector3(-3f, 0f, 0f),
                LocalOffsetB = new Vector3(0f, -3f, 0f)
            }
        });
        AttachModel(testRigidbody1, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity testRigidbody2 = _context.World.CreateEntity($"test rigidbody 2");
        _context.World.Set(testRigidbody2, new TransformComponent(
            new Vector3(chainOffset + 6f, 17f, 0f),
            Quaternion.Identity,
            Vector3.One
        ));
        _context.World.Set(testRigidbody2, new ColliderComponent
        {
            collider = new BoxColliderDefinition(Vector3.One * 2f)
            {
                Mass = 0.25f
            }
        });
        _context.World.Set(testRigidbody2, new RigidbodyComponent
        {
            isKinematic = false,
            material = PhysicsMaterial.Default
        });
        _context.World.Set(testRigidbody2, new ConstraintComponent
        {
            other = testRigidbody1,
            constraint = new BallSocketDefinition
            {
                LocalOffsetA = new Vector3(-3f, 0f, 0f),
                LocalOffsetB = new Vector3(3f, 0f, 0f)
            }
        });
        AttachModel(testRigidbody2, boxModel, Vector3.Zero, Quaternion.Identity, Vector3.One * 2f);

        Entity flames = flamesTemplate.Value.Unpack(_context.World, null);
        _context.World.Relate(flames, testRigidbody2, new ChildOf());
    }

    public override void OnExit()
    {
        base.OnExit();
        _scene?.Dispose();
        Game.UnregisterContext(_context);
    }

    private void AttachModel(Entity entity, RuntimeResource<Model> model, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        Entity mesh = _context!.World.CreateEntity();
        _context.World.Set(mesh, new TransformComponent(localPosition, localRotation, localScale));
        _context.World.Set(mesh, new ModelComponent
        {
            model = model
        });
        _context.World.Relate(mesh, entity, new ChildOf());
    }
}
