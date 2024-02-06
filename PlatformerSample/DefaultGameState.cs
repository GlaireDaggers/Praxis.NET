using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OWB;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

/// <summary>
/// Default game state for platformer game
/// </summary>
public class DefaultGameState : GameState, IGenericEntityHandler
{
    private readonly WorldContext _context;

    private Scene? _scene;
    private Filter _spawnFilter;

    public DefaultGameState(PraxisGame game) : base(game)
    {
        _context = new WorldContext("Platformer World", Game);

        _spawnFilter = new FilterBuilder(_context.World)
            .Include<TransformComponent>()
            .Include<PlayerSpawnComponent>()
            .Build();
    }

    public override void OnEnter()
    {
        base.OnEnter();

        Game.RegisterContext(_context);
        Game.InstallDefaultSystems(_context);

        new SpinSystem(_context);
        new PickupSystem(_context);
        new SimpleCharacterMovementSystem(_context);
        new AnimationStateSystem(_context);
        new CameraFollowSystem(_context);
        new HudSystem(_context);

        _scene = Game.LoadScene("content/levels/TestLevel.owblevel", _context.World, this);

        var playerTemplate = Game.Resources.Load<EntityTemplate>("content/entities/Player.json");

        var spawnPos = _context.World.Get<TransformComponent>(_spawnFilter.FirstEntity);

        var cam = _context.World.CreateEntity("camera");
        _context.World.Set(cam, new TransformComponent(Vector3.Zero, Quaternion.Identity, Vector3.One));
        _context.World.Set(cam, new CameraComponent
        {
            fieldOfView = 60f,
            near = 0.1f,
            far = 1000.0f,
            clearColor = Color.CornflowerBlue,
        });

        var player = playerTemplate.Value.Unpack(_context.World, null);
        _context.World.Set(player, spawnPos);

        _context.World.Set(cam, new CameraFollowComponent
        {
            lookatHeightOffset = 1f,
            followHeightOffset = 3f,
            followRadius = 7f,
            damping = 0.05f
        });
        _context.World.Relate(cam, player, new Following());
    }

    public override void OnExit()
    {
        base.OnExit();
        _scene?.Dispose();
        Game.UnregisterContext(_context);
    }

    public override void Draw()
    {
        base.Draw();
        Game.GraphicsDevice.Clear(Color.CornflowerBlue);
    }

    public void Unpack(GenericEntityNode node, World world, Entity target)
    {
        var entityDef = Game.FindEntityDefinition(node.EntityDefinition);

        if (entityDef != null)
        {
            var transform = world.Get<TransformComponent>(target);

            switch (entityDef.Name)
            {
                case "PlayerSpawn": {
                    Console.WriteLine("Found spawn at " + transform.position);
                    world.Set(target, new PlayerSpawnComponent());
                    break;
                }
                case "Coin": {
                    var coinTemplate = Game.Resources.Load<EntityTemplate>("content/entities/Coin.json");
                    var coin = coinTemplate.Value.Unpack(world, null);
                    world.Set(coin, transform);
                    break;
                }
            }
        }
    }
}
